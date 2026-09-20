using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using EditorContainer=Game.Tools.EditorContainer;
using Transform=Game.Objects.Transform;
namespace GizmoXYZ {
// Native definitions own building relocation and regenerate prefab children for copies.
public partial class GizmoSelectionTool:ObjectToolBaseSystem {
    internal Entity Source,Prefab;
    internal bool Duplicate,PreviewExists;
    private RandomSeed seed;
    private PseudoRandomSeed? appearanceSeed;
    private Tree? tree;
    private Game.Rendering.CustomMeshColor[] colors;
    private EditorContainer? container;
    private EntityQuery allTemps;
    private readonly PreviewCleanup<Entity> cleanup=new PreviewCleanup<Entity>();
    private int pendingGeneration;
    private Transform expected;
    public override string toolID=>"Gizmo XYZ Selection";
    public override PrefabBase GetPrefab()=>null;
    public override bool TrySetPrefab(PrefabBase prefab)=>false;
    protected override void OnCreate(){base.OnCreate();allTemps=GetEntityQuery(ComponentType.ReadOnly<Temp>(),ComponentType.Exclude<Deleted>());}
    internal void Capture(Entity source,bool duplicate){
        Source=source;Duplicate=duplicate;Prefab=EntityManager.GetComponentData<PrefabRef>(source).m_Prefab;seed=RandomSeed.Next();
        appearanceSeed=EntityManager.HasComponent<PseudoRandomSeed>(source)?EntityManager.GetComponentData<PseudoRandomSeed>(source):(PseudoRandomSeed?)null;
        tree=EntityManager.HasComponent<Tree>(source)?EntityManager.GetComponentData<Tree>(source):(Tree?)null;
        container=EntityManager.HasComponent<EditorContainer>(source)?EntityManager.GetComponentData<EditorContainer>(source):(EditorContainer?)null;
        colors=null;
        if(EntityManager.HasBuffer<Game.Rendering.CustomMeshColor>(source)&&EntityManager.IsComponentEnabled<Game.Rendering.CustomMeshColor>(source)){
            var buffer=EntityManager.GetBuffer<Game.Rendering.CustomMeshColor>(source,true);colors=new Game.Rendering.CustomMeshColor[buffer.Length];for(int i=0;i<buffer.Length;i++)colors[i]=buffer[i];
        }
    }
    protected override void OnStartRunning(){base.OnStartRunning();AnarchyInterop.Register(this);}
    protected override JobHandle OnUpdate(JobHandle deps){
        deps.Complete();applyMode=ApplyMode.None;
        try{World.GetOrCreateSystemManaged<GizmoSession>().TickNativeSelection(this);}
        catch(System.Exception e){Diagnostics.Failure("selection.tick.failed",e);World.GetOrCreateSystemManaged<GizmoSession>().Release("selection exception");}
        return default;
    }
    internal void Preview(Transform pose){
        DestroyDefinitions(GetDefinitionQuery(),m_ToolOutputBarrier,default).Complete();applyMode=ApplyMode.Clear;
        pendingGeneration=cleanup.Begin();
        expected=pose;PreviewExists=true;
        var terrain=m_TerrainSystem.GetHeightData();var city=World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        using(var points=new NativeList<ControlPoint>(1,Allocator.TempJob)){
            points.Add(new ControlPoint{m_Position=pose.m_Position,m_HitPosition=pose.m_Position,m_Rotation=pose.m_Rotation,m_Elevation=pose.m_Position.y-TerrainUtils.SampleHeight(ref terrain,pose.m_Position)});
            CreateDefinitions(Prefab,container?.m_Prefab??Entity.Null,Entity.Null,Entity.Null,Duplicate?Entity.Null:Source,Entity.Null,city.defaultTheme,
                points,default,m_ToolSystem.actionMode.IsEditor(),city.leftHandTraffic,false,false,0,0,1,0,0,seed,Snap.None,Game.Tools.AgeMask.Mature,false,default(PlacementOverrides),default).Complete();
        }
    }
    internal bool Prepare(ref CreationDefinition c,ref ObjectDefinition d){
        if(!PreviewExists||c.m_Prefab!=Prefab||c.m_Original!=(Duplicate?Entity.Null:Source)||c.m_Owner!=Entity.Null||math.distancesq(d.m_Position,expected.m_Position)>.0001f)return false;
        d.m_ParentMesh=0;d.m_Rotation=expected.m_Rotation;d.m_LocalRotation=expected.m_Rotation;
        if(appearanceSeed.HasValue)c.m_RandomSeed=appearanceSeed.Value.m_Seed;
        if(Duplicate&&tree.HasValue){
            var value=tree.Value;
            switch(value.m_State&(TreeState.Teen|TreeState.Adult|TreeState.Elderly|TreeState.Dead)){
                case TreeState.Teen:d.m_Age=.1f+value.m_Growth/1706.6666f;break;
                case TreeState.Adult:d.m_Age=.25f+value.m_Growth/731.4286f;break;
                case TreeState.Elderly:d.m_Age=.6f+value.m_Growth/731.4286f;break;
                case TreeState.Dead:d.m_Age=.95f+value.m_Growth/5120f;break;
                default:d.m_Age=value.m_Growth/2560f;break;
            }
        }
        if(container.HasValue){c.m_SubPrefab=container.Value.m_Prefab;d.m_Scale=container.Value.m_Scale;d.m_Intensity=container.Value.m_Intensity;}
        return true;
    }
    internal bool Ready(out Entity root){
        root=Entity.Null;allTemps.CompleteDependency();
        using(var entities=allTemps.ToEntityArray(Allocator.Temp))foreach(var e in entities){
            var temp=EntityManager.GetComponentData<Temp>(e);
            // A copy must never silently demolish or move something already in the city.
            if(Duplicate&&temp.m_Original!=Entity.Null&&(temp.m_Flags&(TempFlags.Delete|TempFlags.Modify|TempFlags.Replace))!=0)return false;
            if(temp.m_Original!=(Duplicate?Entity.Null:Source)||!EntityManager.HasComponent<PrefabRef>(e)||EntityManager.GetComponentData<PrefabRef>(e).m_Prefab!=Prefab||!EntityManager.HasComponent<Transform>(e))continue;
            if(Duplicate&&(temp.m_Flags&TempFlags.Create)==0)continue;
            if(EntityManager.HasComponent<Owner>(e)&&EntityManager.GetComponentData<Owner>(e).m_Owner!=Entity.Null)continue;
            if(!Exact(e))continue;
            if(root!=Entity.Null)return false;root=e;
        }
        if(root==Entity.Null)return false;
        if(Duplicate)ApplyAppearance(root);
        m_ErrorQuery.CompleteDependency();return m_ErrorQuery.IsEmptyIgnoreFilter&&GetAllowApply();
    }
    internal bool Exact(Entity e){
        if(!EntityManager.Exists(e)||EntityManager.HasComponent<Deleted>(e)||!EntityManager.HasComponent<Transform>(e)||!EntityManager.HasComponent<PrefabRef>(e)||EntityManager.GetComponentData<PrefabRef>(e).m_Prefab!=Prefab)return false;
        var t=EntityManager.GetComponentData<Transform>(e);return math.distancesq(t.m_Position,expected.m_Position)<.0025f&&math.abs(math.dot(t.m_Rotation.value,expected.m_Rotation.value))>.9999f;
    }
    internal void Commit(){applyMode=ApplyMode.Apply;DestroyDefinitions(GetDefinitionQuery(),m_ToolOutputBarrier,default).Complete();PreviewExists=false;}
    internal void ClearPreview(){
        // Called by UIUpdate and OnStopRunning as well as ToolUpdate: no ECB here.
        applyMode=ApplyMode.Clear;PreviewExists=false;cleanup.Request();
        Diagnostics.Event("selection.cleanup.request","deferred to Modification1");
    }
    internal void ProcessDefinitions(EntityQuery generated){
        // ToolOutputBarrier has now played even if UI requested cancellation after the job.
        if(pendingGeneration!=0){
            generated.CompleteDependency();
            using(var entities=generated.ToEntityArray(Allocator.Temp))
                foreach(var entity in entities)cleanup.Track(entity,pendingGeneration);
            pendingGeneration=0;
        }
        var retired=cleanup.Drain(EntityManager.Exists);
        foreach(var entity in retired)EntityManager.DestroyEntity(entity);
        if(retired.Count>0)Diagnostics.Event("selection.cleanup.completed",$"definitions={retired.Count} phase=Modification1");
    }
    internal void ApplyAppearance(Entity entity){
        if(tree.HasValue&&EntityManager.HasComponent<Tree>(entity))EntityManager.SetComponentData(entity,tree.Value);
        if(appearanceSeed.HasValue&&EntityManager.HasComponent<PseudoRandomSeed>(entity))EntityManager.SetComponentData(entity,appearanceSeed.Value);
        if(colors!=null&&EntityManager.HasBuffer<Game.Rendering.CustomMeshColor>(entity)){
            var buffer=EntityManager.GetBuffer<Game.Rendering.CustomMeshColor>(entity);buffer.Clear();foreach(var c in colors)buffer.Add(c);EntityManager.SetComponentEnabled<Game.Rendering.CustomMeshColor>(entity,true);
        }
    }
    protected override void OnStopRunning(){ClearPreview();World.GetOrCreateSystemManaged<GizmoSession>().Release("selection tool stopped");base.OnStopRunning();}
}
public partial class GizmoSelectionDefinitionSystem:GameSystemBase {
    private EntityQuery definitions,generated;
    protected override void OnCreate(){base.OnCreate();definitions=GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(),ComponentType.ReadWrite<ObjectDefinition>(),ComponentType.ReadOnly<Updated>(),ComponentType.Exclude<Deleted>());generated=GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(),ComponentType.ReadOnly<Updated>());}
    protected override void OnUpdate(){
        // Always drain, including when another tool is active. Include network/area definitions.
        Dependency.Complete();World.GetOrCreateSystemManaged<GizmoSelectionTool>().ProcessDefinitions(generated);
        var tool=World.GetOrCreateSystemManaged<ToolSystem>().activeTool as GizmoSelectionTool;if(tool==null||!tool.PreviewExists)return;
        Dependency.Complete();definitions.CompleteDependency();
        using(var entities=definitions.ToEntityArray(Allocator.Temp))foreach(var e in entities){
            if(EntityManager.HasComponent<OwnerDefinition>(e))continue;
            var c=EntityManager.GetComponentData<CreationDefinition>(e);var d=EntityManager.GetComponentData<ObjectDefinition>(e);
            if(tool.Prepare(ref c,ref d)){EntityManager.SetComponentData(e,c);EntityManager.SetComponentData(e,d);}
        }
    }
}
}


