namespace GizmoXYZ {
internal sealed class ConfirmGate {
    private double readyAt;
    internal void Reset()=>readyAt=0;
    internal void EndDrag(double now)=>readyAt=now+0.1;
    internal bool Ready(double now,bool dragging,bool valid)=>!dragging&&valid&&now>=readyAt;
}
}
