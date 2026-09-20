using System.Collections.Generic;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;
using Transform=Game.Objects.Transform;
namespace GizmoXYZ {
public partial class GizmoSession {
    private readonly Dictionary<Entity,Transform> editChildren=new Dictionary<Entity,Transform>();
    private readonly Dictionary<Entity,Entity> editOwners=new Dictionary<Entity,Entity>();
    private bool ReadEditChildren(Entity root,Dictionary<Entity,Transform> result){
        var queue=new List<Entity>{root};var seen=new HashSet<Entity>();int cursor=0;
        while(cursor<queue.Count){
            var e=queue[cursor++];if(!seen.Add(e)||seen.Count>2048)return false;
            if(!EntityManager.Exists(e)||!EntityManager.HasComponent<Transform>(e)||EntityManager.HasComponent<Deleted>(e)||EntityManager.HasComponent<Game.Buildings.Building>(e)||EntityManager.HasComponent<Game.Objects.NetObject>(e)||EntityManager.HasComponent<Game.Objects.UtilityObject>(e))return false;
            if(EntityManager.HasBuffer<Game.Net.SubNet>(e)&&EntityManager.GetBuffer<Game.Net.SubNet>(e,true).Length>0)return false;
            if(EntityManager.HasBuffer<Game.Areas.SubArea>(e)&&EntityManager.GetBuffer<Game.Areas.SubArea>(e,true).Length>0)return false;
            if(e!=root)result?.Add(e,EntityManager.GetComponentData<Transform>(e));
            if(EntityManager.HasBuffer<Game.Objects.SubObject>(e))foreach(var child in EntityManager.GetBuffer<Game.Objects.SubObject>(e,true)){
                var c=child.m_SubObject;
                if(!EntityManager.Exists(c)||!EntityManager.HasComponent<Owner>(c)||EntityManager.GetComponentData<Owner>(c).m_Owner!=e)return false;
                queue.Add(c);
            }
        }
        return true;
    }
    private void CaptureEditChildren(){
        editChildren.Clear();editOwners.Clear();
        if(!ReadEditChildren(Edited,editChildren))throw new System.InvalidOperationException("Unsupported edit hierarchy");
        foreach(var e in editChildren.Keys)editOwners.Add(e,EntityManager.GetComponentData<Owner>(e).m_Owner);
    }
    private bool ChildrenUnchanged(){
        var current=new Dictionary<Entity,Transform>();if(!ReadEditChildren(Edited,current)||current.Count!=editChildren.Count)return false;
        foreach(var pair in editChildren){
            if(!current.TryGetValue(pair.Key,out var t)||EntityManager.GetComponentData<Owner>(pair.Key).m_Owner!=editOwners[pair.Key]||math.distancesq(t.m_Position,pair.Value.m_Position)>.000001f||math.abs(math.dot(t.m_Rotation.value,pair.Value.m_Rotation.value))<.99999f)return false;
        }
        return true;
    }
    internal struct ChildPreview {internal Entity Original,Owner;internal Transform World,Local;}
    private List<ChildPreview> BuildEditChildPreviews(){
        var result=new List<ChildPreview>(editChildren.Count);
        var q=math.mul(Rotation,math.inverse(editBaseline.m_Rotation));
        foreach(var pair in editChildren){
            var t=pair.Value;var owner=editOwners[pair.Key];
            var parent=owner==Edited?editBaseline:editChildren[owner];
            var local=new Transform(HierarchyMath.Local(t.m_Position,parent.m_Position,parent.m_Rotation),math.mul(math.inverse(parent.m_Rotation),t.m_Rotation));
            t.m_Position=HierarchyMath.Move(t.m_Position,editBaseline.m_Position,Position,q);t.m_Rotation=math.mul(q,t.m_Rotation);
            result.Add(new ChildPreview{Original=pair.Key,Owner=owner,World=t,Local=local});
        }
        return result;
    }
    private void MoveEditChildren(Transform target){
        var q=math.mul(target.m_Rotation,math.inverse(editBaseline.m_Rotation));
        foreach(var pair in editChildren){
            var t=pair.Value;t.m_Position=HierarchyMath.Move(t.m_Position,editBaseline.m_Position,target.m_Position,q);t.m_Rotation=math.mul(q,t.m_Rotation);
            // Owned TransformRecord and LocalTransformCache are parent-local and remain unchanged.
            EntityManager.SetComponentData(pair.Key,t);EntityManager.AddComponent<Updated>(pair.Key);
        }
    }
}
}
