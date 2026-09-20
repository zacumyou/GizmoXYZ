using Unity.Mathematics;
namespace GizmoXYZ {
public partial class GizmoSession {
    private void SnapToGround(){
        if(PickingReference||applying)return;
        EndDrag("ground snap");
        var pivot=(float3)Pivot;
        var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
        var height=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,pivot);
        var next=PivotMath.Ground(Position,pivot,height);
        if(!math.all(math.isfinite(next))){Diagnostics.Event("ground.rejected","invalid terrain height");return;}
        if(math.distancesq(next,Position)<.000001f)return;
        var previous=Position;Position=next;dirty=true;CanPlace=false;confirm=false;nextBuild=0;
        confirmGate.EndDrag(UnityEngine.Time.unscaledTime);
        if(CopyGroup)CopyCall("SetGizmoTransform",Position,Rotation);
        Diagnostics.Event("ground.snap",$"pivot={pivot} terrain={height} from={previous} to={Position} group={CopyGroup}");
    }
}
}
