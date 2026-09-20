using System;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Transform = Game.Objects.Transform;
namespace GizmoXYZ {
// Optional integration: use the loaded mod's actual components; never ship substitute types.
internal static class AnarchyInterop {
    private static Type bridge, record;
    private static MethodInfo setRecord;
    private static float retry;
    internal static bool Available {
        get {
            if(bridge!=null)return true;
            if(UnityEngine.Time.unscaledTime<retry)return false;
            retry=UnityEngine.Time.unscaledTime+2;
            var assembly=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="Anarchy");
            var candidate=assembly?.GetType("Anarchy.Bridge.AnarchyBridge");
            var component=assembly?.GetType("Anarchy.Components.TransformRecord");
            if(candidate==null || component?.GetField("m_Position")==null || component.GetField("m_Rotation")==null)return false;
            if(candidate.GetMethod("GetAnarchyComponentType")==null || candidate.GetMethod("GetTransformLockComponentType")==null)return false;
            record=component;
            setRecord=typeof(EntityManager).GetMethods().Single(m=>m.Name=="SetComponentData" && m.IsGenericMethodDefinition && m.GetParameters().Length==2 && m.GetParameters()[0].ParameterType==typeof(Entity)).MakeGenericMethod(record);
            bridge=candidate; Diagnostics.Event("anarchy.bridge.ready",assembly.FullName);return true;
        }
    }
    internal static bool Active(World world){
        if(!Available)return false;
        var type=bridge.Assembly.GetType("Anarchy.Systems.Common.AnarchyUISystem");
        var system=type==null?null:world.GetExistingSystemManaged(type);
        return system!=null&&system.Enabled;
    }
    private static object Call(string name,Type[] signature,params object[] args) {
        if(!Available)throw new InvalidOperationException("Anarchy bridge unavailable");
        var method=bridge.GetMethod(name,BindingFlags.Public|BindingFlags.Static,null,signature,null);
        if(method==null)throw new MissingMethodException(bridge.FullName,name);
        return method.Invoke(null,args);
    }
    internal static bool Has(EntityManager em,Entity entity,bool locked) => Available && entity!=Entity.Null && em.Exists(entity) && em.HasComponent(entity,(ComponentType)Call(locked?"GetTransformLockComponentType":"GetAnarchyComponentType",Type.EmptyTypes));
    internal static void Register(Game.Tools.ToolBaseSystem tool){
        if(Available)Diagnostics.Event("anarchy.tool.register",Call("TryAddToolSystem",new[]{typeof(Game.Tools.ToolBaseSystem)},tool).ToString());
    }
    internal static void ApplyPlacementLock(Unity.Entities.World world,Entity[] entities){
        if(!Available)return;
        var type=bridge.Assembly.GetType("Anarchy.Systems.Common.AnarchyUISystem");
        var system=type==null?null:world.GetExistingSystemManaged(type);
        if(system==null||!(type.GetProperty("LockElevation")?.GetValue(system) is bool enabled)||!enabled)return;
        foreach(var entity in entities)if(world.EntityManager.Exists(entity)&&world.EntityManager.HasComponent<Transform>(entity)&&!Has(world.EntityManager,entity,true))
            Call("TryAddTransformLockComponent",new[]{typeof(Entity),typeof(Transform)},entity,world.EntityManager.GetComponentData<Transform>(entity));
    }
    internal static void Toggle(EntityManager em,Entity entity,bool locked) {
        if(entity==Entity.Null || !em.Exists(entity))return;
        bool old=Has(em,entity,locked);
        if(old)Call(locked?"RemoveTransformLockComponent":"RemoveAnarchyComponent",new[]{typeof(Entity)},entity);
        else if(locked)Call("TryAddTransformLockComponent",new[]{typeof(Entity),typeof(Transform)},entity,em.GetComponentData<Transform>(entity));
        else Call("TryAddAnarchyComponent",new[]{typeof(Entity)},entity);
        Diagnostics.Event("anarchy.toggle",$"entity={entity} lock={locked} before={old} after={Has(em,entity,locked)}");
    }
    internal static void UpdateLock(EntityManager em,Entity entity,Transform transform) {
        if(!Has(em,entity,true))return;
        // Supported selected edits are standalone: their TransformRecord is world-space.
        // Older Anarchy releases have no TryUpdateTransformLockComponent bridge method.
        object value=Activator.CreateInstance(record);
        record.GetField("m_Position").SetValue(value,transform.m_Position);
        record.GetField("m_Rotation").SetValue(value,transform.m_Rotation);
        setRecord.Invoke(em,new[]{(object)entity,value});
    }
}
}
