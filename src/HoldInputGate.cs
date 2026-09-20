namespace GizmoXYZ {
// Runs before the native placement action. A modifier must not require native Apply to fire.
internal sealed class HoldInputGate {
    private int consumedFrame = -1;
    internal bool TryConsume(int frame, bool requested, bool ready, bool activeObjectTool, bool createMode,
        bool focused, bool inputField, bool overlay, bool overWorld, bool alreadyHeld) {
        if (!requested || !ready || !activeObjectTool || !createMode || !focused || inputField || overlay || !overWorld || alreadyHeld || consumedFrame == frame)
            return false;
        consumedFrame = frame;
        return true;
    }
}
}
