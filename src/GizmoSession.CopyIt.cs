using System;
using System.Reflection;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
namespace GizmoXYZ {
public partial class GizmoSession {
    private ToolBaseSystem copyTool,cancellingCopyTool;
    internal bool CopyGroup=>copyTool!=null;
    private static object CopyGet(ToolBaseSystem tool,string name)=>tool.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance)?.GetValue(tool);
    private object CopyCall(string name,params object[] args)=>copyTool.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.Instance).Invoke(copyTool,args);
    internal static bool CopyCompatible(ToolBaseSystem tool)=>tool?.GetType().FullName=="CopyIt.CopyTool"&&(CopyGet(tool,"GizmoBridgeVersion") is int version)&&version==1;
    internal void RequestCopy(){
        var tool=World.GetOrCreateSystemManaged<ToolSystem>().activeTool;
        if(Held||cancellingCopyTool!=null)return;
        if(!CopyCompatible(tool)){
            var selected=World.GetOrCreateSystemManaged<ToolSystem>().selected;
            if(CanDuplicate(selected))BeginNativeSelection(selected,true);
            return;
        }
        AnarchyInterop.Register(tool);
        var accepted=tool.GetType().GetMethod("RequestGizmo").Invoke(tool,null);
        Diagnostics.Event("copyit.request",accepted.ToString());
    }
    internal void TickCopy(){
        var active=World.GetOrCreateSystemManaged<ToolSystem>().activeTool;
        if(cancellingCopyTool!=null){
            if(active==cancellingCopyTool&&CopyGet(active,"GizmoHeld") is bool waiting&&waiting)return;
            cancellingCopyTool=null;Diagnostics.Event("copyit.cancel.ack","native preview released");
        }
        if(!Held&&CopyCompatible(active)&&CopyGet(active,"GizmoHeld") is bool ready&&ready){
            copyTool=active;Held=true;Target=null;Edited=Entity.Null;Position=(float3)CopyGet(active,"GizmoAnchor");Rotation=(quaternion)CopyGet(active,"GizmoRotation");Axis=-1;confirm=cancel=applying=false;dirty=false;Placed=0;
            PickingReference=false;referenceFlashUntil=0;ResetPivot();ReadReference();
            Diagnostics.Event("copyit.hold",$"count={CopyGet(active,"GizmoCount")} pivot={Position}");
        }
        if(!CopyGroup)return;
        if(active!=copyTool||!(bool)CopyGet(copyTool,"GizmoHeld")){Release("Copy It stopped");return;}
        var input=Game.Input.InputManager.instance;
        if(UnityEngine.Application.isFocused&&!input.hasInputFieldFocus&&!input.overlayActive)UpdateDrag();else EndDrag("focus lost");
        CopyCall("SetGizmoTransform",Position,Rotation);
        CanPlace=(bool)CopyGet(copyTool,"GizmoReady");
        var commits=(int)CopyGet(copyTool,"GizmoCommits");
        if(commits!=Placed){AnarchyInterop.ApplyPlacementLock(World,(Entity[])CopyGet(copyTool,"GizmoLastCreated"));Diagnostics.Event("copyit.verified",$"batches={commits}");}
        Placed=commits;Status=(string)CopyGet(copyTool,"GizmoStatus");
    }
}
}

