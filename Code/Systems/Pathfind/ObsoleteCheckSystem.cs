using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Rendering;
using Game.Vehicles;
using RoadRule.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace RoadRule.Systems.Pathfind
{
    public partial class ObsoleteCheckSystem : GameSystemBase
    {
        [BurstCompile]
        private struct ObsoleteCheckJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Target> m_TargetType;

            [ReadOnly]
            public ComponentTypeHandle<PathfindReprocessed> m_PathfindReprocessedType;

            public EntityCommandBuffer.ParallelWriter m_EntityCommandBuffer;

            public bool m_ForceMark;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Target> targetArray = chunk.GetNativeArray(ref m_TargetType);
                NativeArray<PathfindReprocessed> pathfindReprocessedArray = chunk.GetNativeArray(ref m_PathfindReprocessedType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var target = targetArray[i];

                    if (!m_ForceMark)
                    { // 检查是否需要更新
                        if (pathfindReprocessedArray.Length > 0)
                        {
                            var pathfindReprocessed = pathfindReprocessedArray[i];
                            if (pathfindReprocessed.m_LastTargetEntity == target.m_Target)
                            {
                                continue;
                            }
                        }
                    }

                    m_EntityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathfindNeedObsoleteFlag());
                }
            }
        }

        public EndFrameBarrier m_EndFrameBarrier;

        public EntityQuery m_StartedPathfindEntityQuery;

        private bool m_ForceMarkNext;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_StartedPathfindEntityQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All =
                    [
                        ComponentType.ReadWrite<PathOwner>(),
                        ComponentType.ReadOnly<Target>(),
                        ComponentType.ReadOnly<Blocker>(),
                        ComponentType.ReadOnly<CarCurrentLane>(),
                        ComponentType.ReadOnly<Owner>(),
                        ComponentType.ReadOnly<Car>(),
                        ComponentType.ReadOnly<CarNavigation>(),
                        ComponentType.ReadOnly<Swaying>(),
                        ComponentType.ReadOnly<Moving>(),
                        ComponentType.ReadOnly<Transform>(),
                        ComponentType.ReadOnly<Vehicle>(),
                    ],
                    None = [ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<PathfindNeedObsoleteFlag>()],
                }
            );
        }

        protected override void OnUpdate()
        {
            Dependency = JobChunkExtensions.ScheduleParallel(
                new ObsoleteCheckJob
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_TargetType = SystemAPI.GetComponentTypeHandle<Target>(true),
                    m_PathfindReprocessedType = SystemAPI.GetComponentTypeHandle<PathfindReprocessed>(true),
                    m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_ForceMark = m_ForceMarkNext,
                },
                m_StartedPathfindEntityQuery,
                Dependency
            );
            m_ForceMarkNext = false;
        }

        public void UpdateAll()
        {
            m_ForceMarkNext = true;
        }
    }
}
