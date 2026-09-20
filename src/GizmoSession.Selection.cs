using Game.Common;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using Transform=Game.Objects.Transform;
namespace GizmoXYZ {
public partial class GizmoSession {
    internal bool NativeSelection {get;private set;}
    private void BeginNativeSelection(Entity source,bool duplicate){
        Release("begin selected native operation");
        var tools=World.GetOrCreateSystemManaged<ToolSystem>();var tool=World.GetOrCreateSystemManaged<GizmoSelectionTool>();
        tool.Capture(source,duplicate);tools.activeTool=tool;
        var t=EntityManager.GetComponentData<Transform>(source);editBaseline=t;
        Position=builtPosition=t.m_Position;Rotation=t.m_Rotation;PreviewPrefab=tool.Prefab;
        Edited=duplicate?Entity.Null:source;NativeSelection=true;Held=true;Target=null;
        Axis=-1;Placed=0;confirm=cancel=applying=false;dirty=true;nextBuild=0;CanPlace=false;pending=Entity.Null;
        ResetPivot();tools.selected=Entity.Null;
        Diagnostics.Event("selection.begin",$"source={source} duplicate={duplicate} building={EntityManager.HasComponent<Game.Buildings.Building>(source)} position={Position}");
    }
    internal void TickNativeSelection(GizmoSelectionTool tool){
        if(!Held||!NativeSelection)return;
        // Once Apply was sent, resolve its result before allowing cancellation or another Enter.
        if(applying){
            verifyFrames++;
            var result=tool.Duplicate?pending:Edited;
            if(verifyFrames>=2&&tool.Exact(result)&&!EntityManager.HasComponent<Temp>(result)){
                if(tool.Duplicate){tool.ApplyAppearance(result);AnarchyInterop.ApplyPlacementLock(World,new[]{result});}
                else{editBaseline=EntityManager.GetComponentData<Transform>(result);AnarchyInterop.UpdateLock(EntityManager,result,editBaseline);}
                Placed++;PlayPlacementSound();applying=false;pending=Entity.Null;confirm=false;dirty=true;nextBuild=0;
                Diagnostics.Event("selection.verified",$"entity={result} duplicate={tool.Duplicate} count={Placed}");
            }else if(verifyFrames>=12){Diagnostics.Event("selection.unconfirmed",$"entity={result}");Release("native operation not verified; no retry");}
            return;
        }
        if(cancel){Release("selected operation cancelled");return;}
        if(!tool.Duplicate&&(!CanEdit(Edited)||!Unchanged(EntityManager.GetComponentData<Transform>(Edited)))){Release("selected building externally changed");return;}
        var input=Game.Input.InputManager.instance;
        if(!UnityEngine.Application.isFocused||input.hasInputFieldFocus||input.overlayActive){EndDrag("focus lost");confirm=false;return;}
        UpdateDrag();
        if(dirty&&(UnityEngine.Time.unscaledTime>=nextBuild||Axis<0||confirm)){
            tool.Preview(new Transform(Position,Rotation));dirty=false;CanPlace=false;builtPosition=Position;previewFrames=0;nextBuild=UnityEngine.Time.unscaledTime+1f/30f;return;
        }
        previewFrames++;Entity root=Entity.Null;CanPlace=previewFrames>2&&tool.Ready(out root);
        var validation=CanPlace?L10n.Message("GizmoXYZ.Text.197F92D58A"):L10n.Message("GizmoXYZ.Text.63304C6FAF");
        if(validation!=lastValidation){lastValidation=Status=validation;Diagnostics.Event("selection.validation",validation);}
        if(!confirm)return;confirm=false;
        if(!CanPlace){Diagnostics.Event("selection.rejected",validation);return;}
        EndDrag("native selection commit");pending=root;verifyFrames=0;applying=true;CanPlace=false;
        if(!tool.Duplicate)AnarchyInterop.UpdateLock(EntityManager,Edited,new Transform(Position,Rotation));
        tool.Commit();
        Diagnostics.Event("selection.commit",$"root={root} duplicate={tool.Duplicate} position={Position}");
    }
}
}

