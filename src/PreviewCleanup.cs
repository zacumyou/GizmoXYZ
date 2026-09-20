using System;
using System.Collections.Generic;
namespace GizmoXYZ {
// Requests may originate in UIUpdate. Only the modification-stage consumer drains them.
internal sealed class PreviewCleanup<T> where T:notnull {
    private readonly Dictionary<T,int> tracked=new Dictionary<T,int>();
    private int generation,retired;
    internal int Begin()=>++generation;
    internal void Track(T item,int ownerGeneration)=>tracked[item]=ownerGeneration;
    internal void Request()=>retired=generation;
    internal List<T> Drain(Func<T,bool> exists){
        var remove=new List<T>();var destroy=new List<T>();
        foreach(var pair in tracked){
            bool live=exists(pair.Key);
            if(!live||pair.Value<=retired){remove.Add(pair.Key);if(live)destroy.Add(pair.Key);}
        }
        foreach(var item in remove)tracked.Remove(item);
        return destroy;
    }
}
}

