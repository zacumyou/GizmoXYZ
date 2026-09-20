using System;
using System.Linq;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Input;
using Game.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
namespace GizmoXYZ {
public partial class GizmoUI : Game.UI.UISystemBase {
    private GizmoSession session;
    private ProxyAction confirm,cancel,rotate,edit,editPanel,duplicate,space,ground;
    private ValueBinding<bool> held,local,canPlace,editing,rotating,editable,anarchyAvailable,anarchyOn,lockOn;
    private ValueBinding<int> axis,placed,pivotIndex;
    private ValueBinding<string> shortcuts;
    private float nextShortcutRefresh;
    private ValueBinding<string> projection,status,editReason,rings,markers,ticks;
    private string lastTool,lastSelection;
    private int lastObservedClick=-1;
    private ValueBinding<bool> referenceAvailable,pickingReference;
    private ValueBinding<string> referenceName,referenceOutline;
    public override GameMode gameMode => GameMode.Game | GameMode.Editor;
    protected override void OnCreate() {
        base.OnCreate(); session=World.GetOrCreateSystemManaged<GizmoSession>();
        AddBinding(referenceAvailable=new ValueBinding<bool>("GizmoXYZ","referenceAvailable",false));
        AddBinding(pickingReference=new ValueBinding<bool>("GizmoXYZ","pickingReference",false));
        AddBinding(referenceName=new ValueBinding<string>("GizmoXYZ","referenceName",""));
        AddBinding(referenceOutline=new ValueBinding<string>("GizmoXYZ","referenceOutline",""));
        editPanel=Mod.Options.GetAction("EditPanel");
        duplicate=Mod.Options.GetAction("Duplicate");
        space=Mod.Options.GetAction("Space");ground=Mod.Options.GetAction("Ground");
        rotate=Mod.Options.GetAction("Rotate");edit=Mod.Options.GetAction("Edit");
        AddBinding(pivotIndex=new ValueBinding<int>("GizmoXYZ","pivotIndex",4));
        AddBinding(rotating=new ValueBinding<bool>("GizmoXYZ","rotating",false));
        AddBinding(ticks=new ValueBinding<string>("GizmoXYZ","ticks",""));
        AddBinding(markers=new ValueBinding<string>("GizmoXYZ","markers",""));
        AddBinding(rings=new ValueBinding<string>("GizmoXYZ","rings",""));
        AddBinding(shortcuts=new ValueBinding<string>("GizmoXYZ","shortcuts",""));
        confirm=Mod.Options.GetAction("Confirm"); cancel=Mod.Options.GetAction("Cancel");
        AddBinding(editing=new ValueBinding<bool>("GizmoXYZ","editing",false));
        AddBinding(editReason=new ValueBinding<string>("GizmoXYZ","editReason",L10n.Message("GizmoXYZ.Text.5C2AFA8931")));
        AddBinding(editable=new ValueBinding<bool>("GizmoXYZ","editable",false));
        AddBinding(anarchyAvailable=new ValueBinding<bool>("GizmoXYZ","anarchyAvailable",false));
        AddBinding(anarchyOn=new ValueBinding<bool>("GizmoXYZ","anarchyOn",false));
        AddBinding(lockOn=new ValueBinding<bool>("GizmoXYZ","lockOn",false));
        AddBinding(held=new ValueBinding<bool>("GizmoXYZ","held",false));
        AddBinding(local=new ValueBinding<bool>("GizmoXYZ","local",false));
        AddBinding(canPlace=new ValueBinding<bool>("GizmoXYZ","canPlace",false));
        AddBinding(axis=new ValueBinding<int>("GizmoXYZ","axis",-1));
        AddBinding(placed=new ValueBinding<int>("GizmoXYZ","placed",0));
        AddBinding(projection=new ValueBinding<string>("GizmoXYZ","projection",""));
        AddBinding(status=new ValueBinding<string>("GizmoXYZ","status",""));
        AddBinding(new TriggerBinding<string>("GizmoXYZ","command",command=> {
            try { session.Command(command); } catch(Exception e) { Diagnostics.Failure("ui.command.failed",e); }
        }));
        AddBinding(new TriggerBinding<string>("GizmoXYZ","diagnostic",message=>Diagnostics.Event("ui.event",(message??"").Substring(0,Math.Min(300,(message??"").Length)))));
    }
    protected override void OnUpdate() {
        base.OnUpdate();
        var activeTool=World.GetOrCreateSystemManaged<ToolSystem>().activeTool;
        var toolName=activeTool==null?"none":activeTool.GetType().FullName;
        if(toolName!=lastTool) { Diagnostics.Event("input.activeTool",toolName);lastTool=toolName; }
        if(!(activeTool is ObjectToolSystem) && Mod.HoldAction!=null) Mod.HoldAction.shouldBeEnabled=false;
        if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && Keyboard.current!=null && Keyboard.current.shiftKey.isPressed && lastObservedClick!=UnityEngine.Time.frameCount) {
            lastObservedClick=UnityEngine.Time.frameCount;
            var input=InputManager.instance;
            Diagnostics.Event("input.shiftClick.observed",$"frame={lastObservedClick} tool={toolName} held={session.Held} focused={Application.isFocused} field={input.hasInputFieldFocus} overlay={input.overlayActive} world={input.controlOverWorld}");
        }
        session.TickCopy();
        duplicate.shouldBeEnabled=Mod.Ready&&!session.Held&&Application.isFocused&&!InputManager.instance.hasInputFieldFocus&&!InputManager.instance.overlayActive&&(GizmoSession.CopyCompatible(activeTool)||session.CanDuplicate(World.GetOrCreateSystemManaged<ToolSystem>().selected));
        if(duplicate.shouldBeEnabled&&duplicate.WasPressedThisFrame())session.Command("duplicate");
        if (session.Held && !session.Matches(session.Target)) session.Release("tool switched");
        bool enabled=Mod.Ready && session.Held && Application.isFocused && !InputManager.instance.hasInputFieldFocus && !InputManager.instance.overlayActive;
        confirm.shouldBeEnabled=enabled; cancel.shouldBeEnabled=enabled;rotate.shouldBeEnabled=enabled;space.shouldBeEnabled=enabled;ground.shouldBeEnabled=enabled&&!session.PickingReference;
        edit.shouldBeEnabled=Mod.Ready&&!session.Held&&Application.isFocused&&!InputManager.instance.hasInputFieldFocus&&!InputManager.instance.overlayActive&&session.CanEdit(World.GetOrCreateSystemManaged<ToolSystem>().selected);
        editPanel.shouldBeEnabled=edit.shouldBeEnabled;
        if(edit.shouldBeEnabled&&(edit.WasPressedThisFrame()||editPanel.WasPressedThisFrame()))session.Command("edit");
        if(enabled&&space.WasPressedThisFrame())session.Command("space");
        if(ground.shouldBeEnabled&&ground.WasPressedThisFrame())session.Command("ground");
        if(enabled&&rotate.WasPressedThisFrame())session.Command("rotate");
        pivotIndex.Update(session.PivotIndex);rotating.Update(session.RotateMode);rings.Update(session.Rings());markers.Update(session.RingMarkers());ticks.Update(session.RotationTicks());
        if (enabled && cancel.WasPressedThisFrame()) session.Command("cancel");
        else if (enabled && confirm.WasPressedThisFrame()) session.Command("confirm");
                var selected=World.GetOrCreateSystemManaged<ToolSystem>().selected;
        var rejection=session.EditRejection(selected);
        editing.Update(session.Editing); editable.Update(rejection=="");editReason.Update(rejection);
        var selectionState=selected+" | "+rejection;
        if(selectionState!=lastSelection){lastSelection=selectionState;Diagnostics.Event("selection.eligibility",selectionState);}
        anarchyAvailable.Update(AnarchyInterop.Active(World)); anarchyOn.Update(AnarchyInterop.Has(EntityManager,session.Edited,false)); lockOn.Update(AnarchyInterop.Has(EntityManager,session.Edited,true));
        if(session.Held&&UnityEngine.Time.unscaledTime>=nextShortcutRefresh){shortcuts.Update(string.Join("|",new[]{Shortcut(space),Shortcut(rotate),Shortcut(ground),Shortcut(confirm),Shortcut(cancel)}));nextShortcutRefresh=UnityEngine.Time.unscaledTime+.25f;}
        held.Update(session.Held); local.Update(session.Local); canPlace.Update(session.ConfirmReady);
        referenceAvailable.Update(session.ReferenceAvailable);pickingReference.Update(session.PickingReference);referenceName.Update(session.ReferenceName);referenceOutline.Update(session.ReferenceOutline());
        axis.Update(session.Axis); placed.Update(session.Placed); projection.Update(session.Projection()); status.Update(session.Status);
    }
    private static string Shortcut(ProxyAction action){
        if(action==null)return "";
        foreach(var binding in action.bindings)if(binding.isKeyboard&&binding.isSet)
            return string.Join("+",binding.ToHumanReadablePath()).Replace("Escape","Esc").Replace("Control","Ctrl");
        return "";
    }
    protected override void OnGamePreload(Purpose purpose,GameMode mode) {
        session.Release("world preload"); Diagnostics.Event("world.preload",purpose+" "+mode); base.OnGamePreload(purpose,mode);
    }
    protected override void OnStopRunning() {
        space.shouldBeEnabled=false;ground.shouldBeEnabled=false;
        confirm.shouldBeEnabled=false;cancel.shouldBeEnabled=false;rotate.shouldBeEnabled=false;edit.shouldBeEnabled=false;editPanel.shouldBeEnabled=false;duplicate.shouldBeEnabled=false;session.Release("UI stopped");base.OnStopRunning();
        if(Mod.HoldAction!=null) Mod.HoldAction.shouldBeEnabled=false;
    }
}
}

