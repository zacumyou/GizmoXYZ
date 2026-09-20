using System;
namespace GizmoXYZ {
// Pure math shared with the standalone tests. The drag plane contains the axis.
internal static class DragMath {
    internal struct V {
        internal double X, Y, Z;
        internal V(double x, double y, double z) { X=x; Y=y; Z=z; }
        public static V operator +(V a,V b)=>new V(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static V operator -(V a,V b)=>new V(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static V operator *(V a,double b)=>new V(a.X*b,a.Y*b,a.Z*b);
        internal static double Dot(V a,V b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    }
    internal static bool AxisParameter(V rayOrigin,V rayDirection,V anchor,V axis,V viewDirection,out double value) {
        value=0;
        V normal=viewDirection-axis*V.Dot(viewDirection,axis);
        double n=V.Dot(normal,normal), d=V.Dot(rayDirection,normal);
        if(n<1e-5 || Math.Abs(d)<1e-7)return false;
        double t=V.Dot(anchor-rayOrigin,normal)/d;
        if(t<0)return false;
        value=V.Dot(rayOrigin+rayDirection*t-anchor,axis);
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
    internal static double ScreenDelta(double mx,double my,double sx,double sy,double dx,double dy,double pixelsPerUnit) {
        if(pixelsPerUnit<0.01)return 0;
        return ((mx-sx)*dx+(my-sy)*dy)/pixelsPerUnit;
    }
}
}
