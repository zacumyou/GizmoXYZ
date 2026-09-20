using Game;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace GizmoXYZ {
// Preserve requested absolute height on independent roots. Attached editor props keep native parent-mesh/local transforms.
public partial class GizmoDefinitionSystem : GameSystemBase {
    private EntityQuery definitions;
    protected override void OnCreate() {
        base.OnCreate();
        definitions=GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(),ComponentType.ReadWrite<ObjectDefinition>(),ComponentType.ReadOnly<Updated>(),ComponentType.Exclude<Deleted>());
    }
    protected override void OnUpdate() {
        var session=World.GetExistingSystemManaged<GizmoSession>();
        if (session==null || !session.Held || !session.Matches(session.Target)) return;
        Dependency.Complete(); definitions.CompleteDependency();
        using(var entities=definitions.ToEntityArray(Allocator.Temp)) foreach(var entity in entities) {
            var creation=EntityManager.GetComponentData<CreationDefinition>(entity);
            if (creation.m_Original!=Entity.Null || creation.m_Owner!=Entity.Null || creation.m_Prefab!=session.PreviewPrefab || EntityManager.HasComponent<OwnerDefinition>(entity)) continue;
            var definition=EntityManager.GetComponentData<ObjectDefinition>(entity);
            if (math.distancesq(definition.m_Position,session.Position)>0.0025f) continue;
            definition.m_ParentMesh=0;
            EntityManager.SetComponentData(entity,definition);
        }
    }
}
}
