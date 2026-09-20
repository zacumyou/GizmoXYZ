using System;
using System.Diagnostics;
using System.Globalization;
using Game;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using ObjectTransform = Game.Objects.Transform;

namespace GizmoXYZ {
public partial class GizmoSession : GameSystemBase {
    internal bool Held { get; private set; }
    internal bool Local => Mod.Options?.LastLocal ?? true;
    internal bool CanPlace { get; private set; }
    internal int Axis { get; private set; } = -1;
    internal int Placed { get; private set; }
    internal string Status { get; private set; } = L10n.Message("GizmoXYZ.Text.FAC5E80828");
    internal ObjectToolSystem Target { get; private set; }
    internal float3 Position { get; private set; }
    internal quaternion Rotation { get; private set; }
    internal Entity PreviewPrefab { get; private set; }
    private PrefabBase selectedPrefab;
    private EntityQuery temps, sound;
    private Entity pending, expectedOwner;
    private ControlPoint control;
    private float3 builtPosition, lastPlaced;
    private quaternion lastPlacedRotation;
    private bool dirty, confirm, cancel, applying, hasLastPlaced;
    private int previewFrames, verifyFrames;
    private float nextBuild, nextValidation, nextDragLog;
    private Vector3 dragAxis, dragAnchor, dragView;
    private Vector2 dragScreen, screenDirection;
    private float3 dragPosition;
    private double dragStartParameter;
    private float pixelsPerUnit;
    private bool planeDrag;
    private readonly ConfirmGate confirmGate=new ConfirmGate();
    internal bool ConfirmReady=>Held&&!applying&&!PickingReference&&confirmGate.Ready(UnityEngine.Time.unscaledTime,Axis>=0,CanPlace);
    private string lastValidation;

    protected override void OnCreate() {
        base.OnCreate();
        temps = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<ObjectTransform>(), ComponentType.Exclude<Deleted>());
        sound = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
    }
    protected override void OnUpdate() { }
    internal bool Matches(ObjectToolSystem tool) => CopyGroup ? Held && World.GetOrCreateSystemManaged<ToolSystem>().activeTool==copyTool : NativeSelection ? Held && World.GetOrCreateSystemManaged<ToolSystem>().activeTool is GizmoSelectionTool : Editing ? Held && World.GetOrCreateSystemManaged<ToolSystem>().activeTool is GizmoEditTool : Held && Target == tool && tool.GetPrefab() == selectedPrefab && tool.actualMode == ObjectToolSystem.Mode.Create && World.GetOrCreateSystemManaged<ToolSystem>().activeTool == tool;
    internal void Hold(ObjectToolSystem tool) {
        if (Held) return;
        var points = tool.GetControlPoints(out var deps); deps.Complete();
        if (points.Length != 1 || tool.GetPrefab() == null) {
            Diagnostics.Event("hold.rejected", "single-object preview required; points=" + points.Length); return;
        }
        control = points[0];
        if (!math.all(math.isfinite(control.m_Position))) return;
        Position = builtPosition = control.m_Position; Rotation = control.m_Rotation;
        var root = FindRoot(Entity.Null, Position);
        if (root == Entity.Null) { Diagnostics.Event("hold.rejected", "no unique created object at control point"); return; }
        PreviewPrefab = EntityManager.GetComponentData<PrefabRef>(root).m_Prefab;
        expectedOwner = OwnerOf(root);
        Target = tool; selectedPrefab = tool.GetPrefab(); Held = true;
        dirty = true; confirm = cancel = applying = hasLastPlaced = false;
        Axis = -1; previewFrames = 0; Placed = 0; nextBuild = 0; CanPlace = false;
        ResetPivot();
        Status = L10n.Message("GizmoXYZ.Text.48782C8DED");
        Diagnostics.Event("hold.begin", $"prefab={selectedPrefab.name} actual={PreviewPrefab} position={Position} rotation={Rotation.value} owner={expectedOwner} mode={GameManager.instance.gameMode}");
    }
    private Entity OwnerOf(Entity entity) {
        var owner=EntityManager.HasComponent<Owner>(entity) ? EntityManager.GetComponentData<Owner>(entity).m_Owner : Entity.Null;
        if (owner!=Entity.Null && EntityManager.HasComponent<Temp>(owner)) {
            var original=EntityManager.GetComponentData<Temp>(owner).m_Original;
            if (original!=Entity.Null) return original;
        }
        return owner;
    }
    private Entity FindRoot(Entity prefab, float3 position) {
        temps.CompleteDependency();
        Entity found = Entity.Null;
        using (var entities = temps.ToEntityArray(Allocator.Temp)) foreach (var e in entities) {
            var temp = EntityManager.GetComponentData<Temp>(e);
            if (temp.m_Original != Entity.Null || (temp.m_Flags & TempFlags.Create) == 0) continue;
            if (prefab != Entity.Null && EntityManager.GetComponentData<PrefabRef>(e).m_Prefab != prefab) continue;
            var transform = EntityManager.GetComponentData<ObjectTransform>(e);
            if (math.distancesq(transform.m_Position, position) > 0.0025f || math.abs(math.dot(transform.m_Rotation.value, Rotation.value)) < 0.9999f) continue;
            // Skip generated children when capturing; explicitly attached editor props remain eligible.
            var owner = OwnerOf(e);
            if (owner != Entity.Null && EntityManager.HasComponent<Temp>(owner)) continue;
            if (found != Entity.Null) return Entity.Null;
            found = e;
        }
        return found;
    }
    internal JobHandle Tick(ObjectToolSystem tool, JobHandle deps) {
        Native.SetApply(tool, ApplyMode.None);
        if (applying) {
            Verify();
            return deps;
        }
        if (cancel) { Release("cancel key or button"); return deps; }
        if (expectedOwner != Entity.Null && (!EntityManager.Exists(expectedOwner) || EntityManager.HasComponent<Deleted>(expectedOwner))) {
            Release("editor parent removed"); return deps;
        }
        bool focus = Application.isFocused && !InputManager.instance.hasInputFieldFocus && !InputManager.instance.overlayActive;
        if (!focus) { if (Axis >= 0) EndDrag("focus lost"); confirm = false; return deps; }
        UpdateDrag();
        if (dirty && (UnityEngine.Time.unscaledTime >= nextBuild || Axis < 0 || confirm)) {
            var watch = Stopwatch.StartNew();
            var points = tool.GetControlPoints(out var pointDeps); pointDeps.Complete();
            control.m_Position = Position;
            control.m_HitPosition = Position;
            control.m_Rotation = Rotation;
            var terrain = World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
            control.m_Elevation = Position.y - Game.Simulation.TerrainUtils.SampleHeight(ref terrain, Position);
            points.Clear(); points.Add(control);
            Native.SetApply(tool, ApplyMode.Clear);
            deps = Native.Rebuild(tool, deps);
            builtPosition = Position; dirty = false; previewFrames = 0; CanPlace = false;
            nextBuild = UnityEngine.Time.unscaledTime + 1f / 30f;
            if (Mod.Options.DiagnosticDetail && Axis < 0) Diagnostics.Event("preview.rebuild", $"position={Position} schedulingMs={watch.Elapsed.TotalMilliseconds:0.00}");
            return deps;
        }
        previewFrames++;
        if (previewFrames < 2 || dirty) return deps;
        if (confirm || UnityEngine.Time.unscaledTime >= nextValidation) {
            CanPlace = Validate(out var root, out var reason);
            var validation=CanPlace?"ready":reason;
            if (validation!=lastValidation) { Diagnostics.Event("validation.changed",validation);lastValidation=validation; }
            nextValidation = UnityEngine.Time.unscaledTime + 0.15f;
            if (!CanPlace) Status = reason;
            else Status = L10n.Message("GizmoXYZ.Text.197F92D58A");
            if (confirm) {
                confirm = false;
                if (!CanPlace) { Diagnostics.Event("placement.rejected", reason); return deps; }
                EndDrag("commit"); pending = root; verifyFrames = 0;
                Native.SetApply(tool, ApplyMode.Apply);
                deps = Native.ClearDefinitions(tool, deps);
                applying = true; CanPlace = false;
                Status = L10n.Message("GizmoXYZ.Text.B8CBEE86E4");
                Diagnostics.Event("placement.commit", $"entity={pending} position={Position} owner={expectedOwner}");
            }
        }
        return deps;
    }
    private bool Validate(out Entity root, out string reason) {
        root = Entity.Null;
        reason = L10n.Message("GizmoXYZ.Text.193199B951");
        if (hasLastPlaced && math.distancesq(Position, lastPlaced) < 0.0001f && math.abs(math.dot(Rotation.value,lastPlacedRotation.value))>.99999f) {
            reason = L10n.Message("GizmoXYZ.Text.1A33858B36"); return false;
        }
        root = FindRoot(PreviewPrefab, builtPosition);
        if (root == Entity.Null) { reason = L10n.Message("GizmoXYZ.Text.5A61404135"); return false; }
        if (OwnerOf(root) != expectedOwner) { reason = L10n.Message("GizmoXYZ.Text.BA7EC56234"); return false; }
        if (!Native.CanApply(Target)) { reason = L10n.Message("GizmoXYZ.Text.63304C6FAF"); return false; }
        return true;
    }
    private void Verify() {
        verifyFrames++;
        if (EntityManager.Exists(pending) && !EntityManager.HasComponent<Temp>(pending) && !EntityManager.HasComponent<Deleted>(pending) && EntityManager.HasComponent<ObjectTransform>(pending)) {
            var t = EntityManager.GetComponentData<ObjectTransform>(pending);
            bool exact = EntityManager.HasComponent<PrefabRef>(pending) && EntityManager.GetComponentData<PrefabRef>(pending).m_Prefab == PreviewPrefab &&
                math.distancesq(t.m_Position, builtPosition) <= 0.0025f && math.abs(math.dot(t.m_Rotation.value, Rotation.value)) >= 0.9999f && OwnerOf(pending) == expectedOwner;
            Diagnostics.Event(exact ? "placement.verified" : "placement.mismatch", $"entity={pending} expected={builtPosition} actual={t.m_Position} owner={OwnerOf(pending)}");
            if (!exact) { Release("placement mismatch; no automatic retry"); return; }
            Placed++; lastPlaced = builtPosition; lastPlacedRotation=Rotation; hasLastPlaced = true;
            PlayPlacementSound(); applying = false; dirty = true; pending = Entity.Null;
            confirm = false; nextBuild = 0; Status = L10n.Message("GizmoXYZ.Text.1A33858B36");
            Diagnostics.Event("repeat.ready", $"count={Placed} held=true position={Position}");
        } else if (verifyFrames >= 12) {
            Diagnostics.Event("placement.unconfirmed", $"entity={pending} frames={verifyFrames}; no automatic retry");
            Release("placement could not be verified");
        }
    }
    private void PlayPlacementSound() {
        try {
            if (sound.IsEmptyIgnoreFilter) { Diagnostics.Event("sound.unavailable", "ToolUXSoundSettingsData"); return; }
            var data = sound.GetSingleton<ToolUXSoundSettingsData>();
            var clip = EntityManager.HasComponent<BuildingData>(PreviewPrefab) ? data.m_PlaceBuildingSound : data.m_PlacePropSound;
            if (clip == Entity.Null) clip = data.m_PlacePropSound;
            if (clip == Entity.Null) { Diagnostics.Event("sound.unavailable", "empty native clip"); return; }
            World.GetOrCreateSystemManaged<Game.Audio.AudioManager>().PlayUISound(clip);
            Diagnostics.Event("sound.played", $"clip={clip} count=1 placement={Placed}");
        } catch (Exception e) { Diagnostics.Failure("sound.failed", e); }
    }
    internal void Command(string command) {
        if (command == "duplicate") { RequestCopy(); return; }
        if (command == "edit") { EditSelected(); return; }
        if (!Held || !Matches(Target)) return;
        if (Editing && (command == "anarchy" || command == "lock")) { AnarchyInterop.Toggle(EntityManager,Edited,command == "lock"); return; }
        if (command == "cancel") { if(CopyGroup){Release("Copy It cancelled");return;} cancel = true; return; }
        if(ReferenceCommand(command))return;
        if(command=="confirm"&&!ConfirmReady){Diagnostics.Event("confirm.deferred","dragging, settling or preview invalid");return;}
        if(command.StartsWith("pivot:")&&int.TryParse(command.Substring(6),out var index)){SelectPivot(index);return;}
        if(CopyGroup&&command=="confirm"){EndDrag("group commit");CopyCall("SetGizmoTransform",Position,Rotation);CopyCall("ConfirmGizmo");return;}
        if (applying) return;
        switch (command) {
            case "confirm": EndDrag("confirm requested"); confirm = true; Diagnostics.Event("placement.request", $"position={Position}"); break;
            case "rotate": EndDrag("mode changed"); RotateMode=!RotateMode; Diagnostics.Event("gizmo.mode",RotateMode?"Rotate":"Move"); break;
            case "space": if (Axis >= 0) EndDrag("space changed"); PickingReference=false;Mod.Options.LastLocal = !Local;try{Mod.Options.ApplyAndSave();}catch(Exception e){Diagnostics.Failure("space.save.failed",e);} Diagnostics.Event("space.changed", Local ? "Local" : "Global"); break;
            case "drag:0": BeginDrag(0); break;
            case "drag:1": BeginDrag(1); break;
            case "drag:2": BeginDrag(2); break;
            case "drag:3": BeginFreeDrag(); break;
            case "ground": SnapToGround(); break;
        }
    }
    // Display follows the requested Blender-style convention: Z up. Unity internally uses Y up.
    internal Vector3 Direction(int axis) {
        var d = axis == 0 ? new float3(1,0,0) : axis == 1 ? new float3(0,0,1) : new float3(0,1,0);
        return Local ? (Vector3)math.mul(ObjectFrame,d) : (Vector3)d;
    }
    internal Vector3 Pivot => (Vector3)(Position + (RotateMode?math.mul(Rotation,RotationPivotLocal):new float3(0)));
    private static DragMath.V V(Vector3 v) => new DragMath.V(v.x,v.y,v.z);
    private void BeginDrag(int axis) {
        var camera = Camera.main; var mouse = Mouse.current;
        if (PickingReference || camera == null || mouse == null || !mouse.leftButton.isPressed) return;
        confirm=false;
        Axis = axis; dragPosition = Position; dragAxis = Direction(axis); dragAnchor = Pivot; dragView = camera.transform.forward;
        dragScreen = mouse.position.ReadValue();
        if(RotateMode){BeginRotation(camera);return;}
        var ray = camera.ScreenPointToRay(dragScreen);
        planeDrag = DragMath.AxisParameter(V(ray.origin),V(ray.direction),V(dragAnchor),V(dragAxis),V(dragView),out dragStartParameter);
        var a = camera.WorldToScreenPoint(dragAnchor); var b = camera.WorldToScreenPoint(dragAnchor+dragAxis);
        var projected = new Vector2(b.x-a.x,b.y-a.y);
        pixelsPerUnit = projected.magnitude;
        screenDirection = pixelsPerUnit > 0.01f ? projected.normalized : Vector2.up;
        if (pixelsPerUnit < 0.01f) {
            float worldSpan = camera.orthographic ? camera.orthographicSize*2 : 2*Mathf.Max(1,a.z)*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2);
            pixelsPerUnit = Screen.height / worldSpan; planeDrag = false;
        }
        nextDragLog = 0;
        Diagnostics.Event("drag.begin", $"axis={axis} space={(Local?"Local":"Global")} origin={Position} plane={planeDrag}");
    }
    private void UpdateDrag() {
        if (Axis < 0) return;
        var mouse = Mouse.current; var camera = Camera.main;
        if (mouse == null || camera == null || !mouse.leftButton.isPressed) { EndDrag("mouse released"); return; }
        var screen = mouse.position.ReadValue();
        if(FreeDragging){UpdateFreeDrag(camera,screen);return;}
        if(RotateMode){UpdateRotation(camera,screen);return;}
        double delta;
        var ray = camera.ScreenPointToRay(screen);
        if (planeDrag) {
            if (!DragMath.AxisParameter(V(ray.origin),V(ray.direction),V(dragAnchor),V(dragAxis),V(dragView),out var value)) return;
            delta = value-dragStartParameter;
        } else delta = DragMath.ScreenDelta(screen.x,screen.y,dragScreen.x,dragScreen.y,screenDirection.x,screenDirection.y,pixelsPerUnit);
        if (double.IsNaN(delta) || double.IsInfinity(delta) || Math.Abs(delta) > 50000) return;
        var next = dragPosition+(float3)dragAxis*(float)delta;
        if (!math.all(math.isfinite(next)) || math.distancesq(next,Position) < 0.000001f) return;
        Position = next; dirty = true; CanPlace = false; confirm = false;
        Status = L10n.Message("GizmoXYZ.Text.4B3280C6D2", ((Axis==0?"X":Axis==1?"Y":"Z")), $"{delta:0.00}");
        if (Mod.Options.DiagnosticDetail && UnityEngine.Time.unscaledTime >= nextDragLog) {
            Diagnostics.Event("drag.sample", $"axis={Axis} delta={delta:0.000} position={Position}"); nextDragLog = UnityEngine.Time.unscaledTime+0.5f;
        }
    }
    private void EndDrag(string reason) {
        if (Axis < 0) return;
        Diagnostics.Event("drag.end", $"axis={Axis} position={Position} reason={reason}");
        Axis = -1; nextBuild = 0;confirmGate.EndDrag(UnityEngine.Time.unscaledTime);
    }
    internal void Release(string reason) {
        PickingReference=false;referenceRotation=quaternion.identity;
        if (!Held) return;
        EndDrag(reason); Diagnostics.Event("hold.end", $"reason={reason} placed={Placed}");
        if(CopyGroup){CopyCall("CancelGizmo");cancellingCopyTool=copyTool;copyTool=null;}
        if(NativeSelection&&Editing&&EntityManager.Exists(Edited)&&EntityManager.HasComponent<ObjectTransform>(Edited))AnarchyInterop.UpdateLock(EntityManager,Edited,EntityManager.GetComponentData<ObjectTransform>(Edited));
        bool wasNative=NativeSelection; NativeSelection=false; bool wasEditing=Editing; Edited=Entity.Null;
        Held = false; confirm = cancel = applying = dirty = false; CanPlace = false; pending = Entity.Null;
        if(wasNative){
            var tools=World.GetOrCreateSystemManaged<ToolSystem>(); var selection=World.GetExistingSystemManaged<GizmoSelectionTool>(); selection?.ClearPreview();
            if(tools.activeTool==selection)tools.activeTool=World.GetOrCreateSystemManaged<DefaultToolSystem>();
        }
        if (wasEditing && !wasNative) {
            var tools=World.GetOrCreateSystemManaged<ToolSystem>();
            var edit=World.GetExistingSystemManaged<GizmoEditTool>();edit?.ClearPreview();
            if(tools.activeTool==edit)tools.activeTool=World.GetOrCreateSystemManaged<DefaultToolSystem>();
        }
        if (Target != null) Native.Resume(Target);
        Target = null; Status = L10n.Message("GizmoXYZ.Text.FAC5E80828");
    }
    internal string Projection() {
        var camera = Camera.main;
        if (!Held || camera == null || Screen.width <= 0 || Screen.height <= 0) return "";
        var center = camera.WorldToScreenPoint(Pivot);
        if (center.z <= camera.nearClipPlane) return "";
        float rem = Screen.height/1080f;
        float span = camera.orthographic ? camera.orthographicSize*2 : 2*center.z*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2);
        float length = span * 0.13f;
        var values = new float[8]; values[0]=center.x/Screen.width*100; values[1]=(Screen.height-center.y)/Screen.height*100;
        for (int i=0;i<3;i++) {
            var end=camera.WorldToScreenPoint(Pivot+Direction(i)*length);
            var delta=new Vector2(end.x-center.x,center.y-end.y)/rem;
            if (delta.magnitude>155) delta=delta.normalized*155;
            if (delta.magnitude<22 || end.z <= camera.nearClipPlane) delta=new Vector2((i-1)*24,-32);
            values[2+i*2]=delta.x; values[3+i*2]=delta.y;
        }
        return string.Join(",", Array.ConvertAll(values, v=>v.ToString("0.##",CultureInfo.InvariantCulture)));
    }
}
}


