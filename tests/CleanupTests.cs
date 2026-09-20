using GizmoXYZ;
internal static class CleanupTests {
    internal static void Run(Action<bool,string> check){
        var queue=new PreviewCleanup<string>();
        var generation=queue.Begin();queue.Track("root",generation);queue.Track("area",generation);
        queue.Request();queue.Request();
        check(queue.Drain(e=>true).Count==2,"UI and stop requests clean each definition once");
        check(queue.Drain(e=>true).Count==0,"cleanup is idempotent");
        generation=queue.Begin();queue.Request();queue.Track("late-output",generation);
        check(queue.Drain(e=>true).SequenceEqual(new[]{"late-output"}),"cancellation before barrier playback cleans late output");
        generation=queue.Begin();queue.Track("old",generation);queue.Request();
        var next=queue.Begin();queue.Track("new",next);
        check(queue.Drain(e=>true).SequenceEqual(new[]{"old"}),"rapid re-entry preserves new preview");
        check(queue.Drain(e=>true).Count==0,"old cancellation never propagates to new session");
        queue.Track("native-deleted",next);queue.Request();
        check(queue.Drain(e=>e!="native-deleted").SequenceEqual(new[]{"new"}),"already deleted definitions skipped");
        for(int i=0;i<100;i++){
            generation=queue.Begin();queue.Track("preview-"+i,generation);
            check(queue.Drain(e=>true).Count==0,"active preview retained until cancellation");
            queue.Request();check(queue.Drain(e=>true).Count==1,"repeated sessions each clean once");
        }
    }
}
