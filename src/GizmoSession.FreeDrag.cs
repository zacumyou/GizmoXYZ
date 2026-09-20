using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace GizmoXYZ {
public partial class GizmoSession {
    internal bool FreeDragging=>Held&&Axis==3;
    private float freeElevation;
    private float3 freeGrab;
    private bool FreePoint(Camera camera,Vector2 screen,out float3 point){
        var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
        var ray=camera.ScreenPointToRay(screen);
        return FreeDragMath.Surface(ray.origin,ray.direction,freeElevation,p=>Game.Simulation.TerrainUtils.SampleHeight(ref terrain,p),out point);
    }
    private void BeginFreeDrag(){
        var camera=Camera.main;var mouse=Mouse.current;
        if(RotateMode||PickingReference||applying||camera==null||mouse==null||!mouse.leftButton.isPressed)return;
        var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
        freeElevation=Pivot.y-Game.Simulation.TerrainUtils.SampleHeight(ref terrain,Pivot);
        if(!FreePoint(camera,mouse.position.ReadValue(),out freeGrab)){Diagnostics.Event("freeDrag.rejected","no surface intersection");return;}
        EndDrag("free drag started");Axis=3;dragPosition=Position;confirm=false;nextDragLog=0;
        Diagnostics.Event("freeDrag.begin",$"position={Position} elevation={freeElevation}");
    }
    private void UpdateFreeDrag(Camera camera,Vector2 screen){
        if(!FreePoint(camera,screen,out var cursor))return;
        var next=FreeDragMath.Move(dragPosition,freeGrab,cursor);
        if(!math.all(math.isfinite(next))||math.distancesq(next,Position)<.000001f)return;
        Position=next;dirty=true;CanPlace=false;confirm=false;
        if(Mod.Options.DiagnosticDetail&&UnityEngine.Time.unscaledTime>=nextDragLog){Diagnostics.Event("freeDrag.sample",$"position={Position}");nextDragLog=UnityEngine.Time.unscaledTime+.5f;}
    }
}
}
