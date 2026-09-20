using Game.Prefabs;
using Unity.Mathematics;
namespace GizmoXYZ {
public partial class GizmoSession {
    internal int PivotIndex {get;private set;}=4;
    private float3 pivotMin,pivotMax;
    // The nine points lie on the object's local X/Z footprint, at origin height.
    private float3 RotationPivotLocal => CopyGroup?math.mul(referenceRotation,PivotMath.RotationPoint(pivotMin,pivotMax,PivotIndex)):PivotMath.RotationPoint(pivotMin,pivotMax,PivotIndex);
    private void ResetPivot(){
        PivotIndex=4;confirmGate.Reset();pivotMin=pivotMax=0;
        if(CopyGroup){
            if(CopyGet(copyTool,"GizmoBoundsMin") is float3 lo)pivotMin=lo;
            if(CopyGet(copyTool,"GizmoBoundsMax") is float3 hi)pivotMax=hi;
        }else if(EntityManager.HasComponent<ObjectGeometryData>(PreviewPrefab)){
            var bounds=EntityManager.GetComponentData<ObjectGeometryData>(PreviewPrefab).m_Bounds;
            pivotMin=bounds.min;pivotMax=bounds.max;
        }
    }
    private void SelectPivot(int index){
        if(!RotateMode||index<0||index>8)return;
        EndDrag("pivot changed");PivotIndex=index;
        Diagnostics.Event("rotation.pivot",$"index={index} local={RotationPivotLocal} world={Pivot}");
    }
}
}
