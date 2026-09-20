using System;
namespace GizmoXYZ {
internal static class RotationMath {
    internal static double Delta(double previous,double current){var d=current-previous;while(d>180)d-=360;while(d< -180)d+=360;return d;}
    internal static double Snap(double degrees,bool shift)=>shift?Math.Round(degrees/15,MidpointRounding.AwayFromZero)*15:degrees;
}
}
