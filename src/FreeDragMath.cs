using System;
using Unity.Mathematics;
namespace GizmoXYZ {
internal static class FreeDragMath {
    // Follow terrain at the captured height above ground. A bounded search also
    // handles oblique cameras; a sky/invalid ray leaves the last valid pose intact.
    internal static bool Surface(float3 origin,float3 direction,float elevation,Func<float3,float> height,out float3 point){
        point=0;
        if(!math.all(math.isfinite(origin))||!math.all(math.isfinite(direction))||!math.isfinite(elevation)||math.lengthsq(direction)<.000001f)return false;
        direction=math.normalize(direction);
        float previous=0,previousGap=origin.y-height(origin)-elevation;
        if(!math.isfinite(previousGap))return false;
        for(float distance=4;distance<=50000;distance+=math.max(4,distance*.06f)){
            var candidate=origin+direction*distance;
            float gap=candidate.y-height(candidate)-elevation;
            if(!math.isfinite(gap))return false;
            if(previousGap>=0&&gap<=0||previousGap<=0&&gap>=0){
                float low=previous,high=distance;
                for(int i=0;i<20;i++){
                    float middle=(low+high)*.5f;var p=origin+direction*middle;float g=p.y-height(p)-elevation;
                    if(!math.isfinite(g))return false;
                    if((g>=0)==(previousGap>=0))low=middle;else high=middle;
                }
                point=origin+direction*((low+high)*.5f);return math.all(math.isfinite(point));
            }
            previous=distance;previousGap=gap;
        }
        return false;
    }
    internal static float3 Move(float3 start,float3 grab,float3 cursor)=>start+cursor-grab;
}
}
