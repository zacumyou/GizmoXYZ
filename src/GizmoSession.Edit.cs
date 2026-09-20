using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Transform=Game.Objects.Transform;
namespace GizmoXYZ {
public partial class GizmoSession {
    internal Entity Edited {get;private set;}
    internal bool Editing=>Edited!=Entity.Null;
    private Transform editBaseline;
    private int editVerify;
    private System.Collections.Generic.List<ChildPreview> editPreviewChildren=new System.Collections.Generic.List<ChildPreview>();
    internal bool CanEdit(Entity entity)=>EditRejection(entity)=="";
    internal bool CanDuplicate(Entity entity)=>EditRejection(entity,true)=="";
    internal string EditRejection(Entity entity,bool duplicate=false){
        if(!Mod.Ready)return L10n.Message("GizmoXYZ.Text.F42BAE8DA9");
        if(entity==Entity.Null || !EntityManager.Exists(entity))return L10n.Message("GizmoXYZ.Text.EB00DF5CFD");
        if(!EntityManager.HasComponent<Transform>(entity) || !EntityManager.HasComponent<PrefabRef>(entity))return L10n.Message("GizmoXYZ.Text.19ADCCB3F2");
        if(EntityManager.HasComponent<Deleted>(entity)||EntityManager.HasComponent<Temp>(entity))return L10n.Message("GizmoXYZ.Text.B2FF18AF06");
        var prefab=EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
        var prefabSystem=World.GetOrCreateSystemManaged<PrefabSystem>();
        if(!prefabSystem.TryGetPrefab<PrefabBase>(prefab,out var asset) || !(asset is StaticObjectPrefab))return L10n.Message("GizmoXYZ.Text.205B6F8CC3");
        // A present but null Owner/Attached is not an attached object.
        if(EntityManager.HasComponent<Owner>(entity)&&EntityManager.GetComponentData<Owner>(entity).m_Owner!=Entity.Null)return L10n.Message("GizmoXYZ.Text.C4B68BDFFC");
        if(EntityManager.HasComponent<Game.Objects.Attached>(entity)&&EntityManager.GetComponentData<Game.Objects.Attached>(entity).m_Parent!=Entity.Null)return L10n.Message("GizmoXYZ.Text.BEE4765B3D");
        if(EntityManager.HasComponent<Game.Objects.NetObject>(entity)||EntityManager.HasComponent<Game.Objects.UtilityObject>(entity))return L10n.Message("GizmoXYZ.Text.77A0826FF7");
        // Buildings use native relocation, including their areas, networks and upgrades.
        if(duplicate||EntityManager.HasComponent<Game.Buildings.Building>(entity))return "";
        if(!ReadEditChildren(entity,null))return L10n.Message("GizmoXYZ.Text.7A73387369");
        if(EntityManager.HasBuffer<Game.Net.SubNet>(entity)&&EntityManager.GetBuffer<Game.Net.SubNet>(entity,true).Length>0)return L10n.Message("GizmoXYZ.Text.7F2C568D61");
        if(EntityManager.HasBuffer<Game.Areas.SubArea>(entity)&&EntityManager.GetBuffer<Game.Areas.SubArea>(entity,true).Length>0)return L10n.Message("GizmoXYZ.Text.8D147D0375");
        return "";
    }
    internal void EditSelected(){
        var tools=World.GetOrCreateSystemManaged<ToolSystem>();var entity=tools.selected;
        if(!CanEdit(entity)){Diagnostics.Event("edit.rejected",$"entity={entity}; standalone static prop/tree required");return;}
        if(EntityManager.HasComponent<Game.Buildings.Building>(entity)){BeginNativeSelection(entity,false);return;}
        Release("begin selected edit");
        editBaseline=EntityManager.GetComponentData<Transform>(entity);
        Position=builtPosition=editBaseline.m_Position;Rotation=editBaseline.m_Rotation;
        PreviewPrefab=EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
        var tool=World.GetOrCreateSystemManaged<GizmoEditTool>();tools.activeTool=tool;
        Edited=entity;Held=true;Target=null;Axis=-1;Placed=0;confirm=cancel=applying=false;dirty=true;nextBuild=0;editVerify=0;CanPlace=false;
        ResetPivot();
        CaptureEditChildren();
        tools.selected=Entity.Null; // Close native selected-info panel after capturing the edit target.
        Diagnostics.Event("edit.panel.closed",$"entity={Edited} children={editChildren.Count}");
        Status=L10n.Message("GizmoXYZ.Text.C1B7F37D47");
        Diagnostics.Event("edit.begin",$"entity={entity} position={Position} anarchy={AnarchyInterop.Has(EntityManager,entity,false)} lock={AnarchyInterop.Has(EntityManager,entity,true)}");
    }
    private bool Unchanged(Transform t)=>math.distancesq(t.m_Position,editBaseline.m_Position)<0.000001f&&math.abs(math.dot(t.m_Rotation.value,editBaseline.m_Rotation.value))>.99999f;
    private bool EditPreviewReady(){
        temps.CompleteDependency();
        var expected=new System.Collections.Generic.Dictionary<Entity,Transform>{{Edited,new Transform(Position,Rotation)}};
        foreach(var child in editPreviewChildren)expected.Add(child.Original,child.World);
        using(var entities=temps.ToEntityArray(Allocator.Temp))foreach(var entity in entities){
            var original=EntityManager.GetComponentData<Temp>(entity).m_Original;
            if(!expected.TryGetValue(original,out var desired))continue;
            var t=EntityManager.GetComponentData<Transform>(entity);
            if(math.distancesq(t.m_Position,desired.m_Position)<.0025f&&math.abs(math.dot(t.m_Rotation.value,desired.m_Rotation.value))>.9999f)expected.Remove(original);
        }
        return expected.Count==0;
    }
    internal void TickEdit(GizmoEditTool tool){
        tool.Idle();if(!Held||!Editing)return;
        if(!CanEdit(Edited)){Release("edited object removed or changed");return;}
        if(cancel){Release("edit cancelled; original retained");return;}
        var actual=EntityManager.GetComponentData<Transform>(Edited);
        if(!Unchanged(actual)||!ChildrenUnchanged()){Release("external transform change; do not overwrite");return;}
        if(editVerify>0){
            if(++editVerify>=4){Diagnostics.Event("edit.verified",$"entity={Edited} position={actual.m_Position}");editVerify=0;PlayPlacementSound();dirty=true;}
            return;
        }
        var input=Game.Input.InputManager.instance;
        if(!UnityEngine.Application.isFocused||input.hasInputFieldFocus||input.overlayActive){EndDrag("focus lost");confirm=false;return;}
        UpdateDrag();
        if(dirty&&(UnityEngine.Time.unscaledTime>=nextBuild||Axis<0||confirm)){
            editPreviewChildren=BuildEditChildPreviews();
            tool.Preview(Edited,new Transform(Position,Rotation),editPreviewChildren);CanPlace=false;builtPosition=Position;dirty=false;previewFrames=0;nextBuild=UnityEngine.Time.unscaledTime+1f/30f;return;
        }
        previewFrames++;
        bool previewReady=previewFrames>2&&!dirty&&EditPreviewReady();
        CanPlace=previewReady&&tool.CanCommit();
        var validation=!previewReady?L10n.Message("GizmoXYZ.Text.32743D96D8"):!CanPlace?L10n.Message("GizmoXYZ.Text.7F7686ABDD"):L10n.Message("GizmoXYZ.Text.6C9B6DCF95");
        if(validation!=lastValidation){lastValidation=validation;Status=validation;Diagnostics.Event("edit.validation",validation);}
        if(confirm&&previewFrames>2&&!dirty&&!CanPlace){confirm=false;Diagnostics.Event("edit.rejected",validation);}
        if(confirm&&CanPlace){
            confirm=false;EndDrag("edit commit");
            if(math.distancesq(Position,actual.m_Position)<.000001f&&math.abs(math.dot(Rotation.value,actual.m_Rotation.value))>.99999f)return;
            var moved=new Transform(Position,Rotation);
            // Preserve an existing lock, updating its target before marking the original Updated.
            AnarchyInterop.UpdateLock(EntityManager,Edited,moved);
            MoveEditChildren(moved);
            EntityManager.SetComponentData(Edited,moved);EntityManager.AddComponent<Updated>(Edited);
            editBaseline=moved;CaptureEditChildren();Placed++;editVerify=1;CanPlace=false;
            tool.ClearPreview();Diagnostics.Event("edit.commit",$"entity={Edited} from={actual.m_Position} to={Position} count={Placed}");
        }
    }
}
}

