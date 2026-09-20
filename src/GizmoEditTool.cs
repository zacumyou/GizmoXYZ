using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
namespace GizmoXYZ {
public partial class GizmoEditTool : ToolBaseSystem {
    private ToolOutputBarrier barrier;
    private EntityQuery definitions;
    private bool hasPreview;
    public override string toolID => "Gizmo XYZ Edit";
    public override PrefabBase GetPrefab()=>null;
    public override bool TrySetPrefab(PrefabBase prefab)=>false;
    protected override void OnCreate(){base.OnCreate();barrier=World.GetOrCreateSystemManaged<ToolOutputBarrier>();definitions=GetEntityQuery(ComponentType.ReadOnly<GizmoEditDefinition>());}
    protected override JobHandle OnUpdate(JobHandle deps){
        deps.Complete();
        try { World.GetOrCreateSystemManaged<GizmoSession>().TickEdit(this); }
        catch(System.Exception e){Diagnostics.Failure("edit.tick.failed",e);World.GetOrCreateSystemManaged<GizmoSession>().Release("edit exception");}
        return default;
    }
    internal void Preview(Entity original,Transform transform,List<GizmoSession.ChildPreview> children){
        applyMode=ApplyMode.Clear;
        if(hasPreview)DestroyDefinitions(definitions,barrier,default).Complete();
        hasPreview=true;
        var buffer=barrier.CreateCommandBuffer();
        AddPreview(buffer,original,Entity.Null,transform,transform);
        foreach(var child in children)AddPreview(buffer,child.Original,child.Owner,child.World,child.Local);
    }
    private void AddPreview(EntityCommandBuffer buffer,Entity original,Entity owner,Transform transform,Transform local){
        var entity=buffer.CreateEntity();
        buffer.AddComponent(entity,new GizmoEditDefinition());
        buffer.AddComponent(entity,new Updated());
        buffer.AddComponent(entity,new CreationDefinition{m_Original=original,m_Owner=owner,m_Flags=CreationFlags.Select|CreationFlags.Dragging});
        var definition=new ObjectDefinition{m_Position=transform.m_Position,m_Rotation=transform.m_Rotation,m_LocalPosition=local.m_Position,m_LocalRotation=local.m_Rotation,m_ParentMesh=-1,m_Probability=100,m_PrefabSubIndex=-1};
        var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
        definition.m_Elevation=transform.m_Position.y-Game.Simulation.TerrainUtils.SampleHeight(ref terrain,transform.m_Position);
        definition.m_ParentMesh=0; // Explicit absolute height for these standalone roots.
        buffer.AddComponent(entity,definition);
    }
    internal void Idle(){applyMode=ApplyMode.None;}
    internal bool CanCommit()=>GetAllowApply();
    protected override void OnStartRunning(){base.OnStartRunning();AnarchyInterop.Register(this);}
    internal void ClearPreview(){applyMode=ApplyMode.Clear;if(hasPreview){definitions.CompleteDependency();EntityManager.DestroyEntity(definitions);hasPreview=false;}}
    protected override void OnStopRunning(){
        ClearPreview();World.GetOrCreateSystemManaged<GizmoSession>().Release("edit tool stopped");base.OnStopRunning();
    }
}
internal struct GizmoEditDefinition:IComponentData {}
}
