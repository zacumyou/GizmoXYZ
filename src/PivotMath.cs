using Unity.Mathematics;
namespace GizmoXYZ {
internal static class PivotMath {
    internal static float3 RotationPoint(float3 min,float3 max,int index)=>index==4?new float3(0):Point(min,max,index);
    internal static float3 Ground(float3 position,float3 pivot,float height)=>position+new float3(0,height-pivot.y,0);
    internal static quaternion ObjectFrame(quaternion groupDelta,quaternion reference)=>math.mul(groupDelta,reference);
    internal static float3 Point(float3 min,float3 max,int index)=>new float3(
        math.lerp(min.x,max.x,(index%3)*.5f),0,
        math.lerp(max.z,min.z,(index/3)*.5f));
    internal static float3 RotatePosition(float3 position,float3 pivot,quaternion delta)=>pivot+math.mul(delta,position-pivot);
}
}
