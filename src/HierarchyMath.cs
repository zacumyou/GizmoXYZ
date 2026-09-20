using Unity.Mathematics;
namespace GizmoXYZ {
internal static class HierarchyMath {
    internal static float3 Move(float3 child,float3 before,float3 after,quaternion delta)=>after+math.mul(delta,child-before);
    internal static float3 Local(float3 child,float3 parent,quaternion parentRotation)=>math.mul(math.inverse(parentRotation),child-parent);
}
}
