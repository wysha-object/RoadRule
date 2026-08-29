using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Common;
using Game.Pathfind;
using Game.Vehicles;
using RoadRule.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace RoadRule.Systems.Pathfind
{
    public partial class ObsoleteMarkerSystem : GameSystemBase
    {
        [BurstCompile]
        private struct ObsoleteMarkerJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            public ComponentTypeHandle<PathOwner> m_PathOwnerType;

            [ReadOnly]
            public ComponentTypeHandle<Target> m_TargetType;

            [ReadOnly]
            public ComponentTypeHandle<PathfindReprocessed> m_PathfindReprocessedType;

            public EntityCommandBuffer.ParallelWriter m_EntityCommandBuffer;

            public bool m_ForceMarker;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<PathOwner> pathOwnerArray = chunk.GetNativeArray(ref m_PathOwnerType);
                NativeArray<Target> targetArray = chunk.GetNativeArray(ref m_TargetType);
                NativeArray<PathfindReprocessed> pathfindReprocessedArray = chunk.GetNativeArray(ref m_PathfindReprocessedType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var pathOwner = pathOwnerArray[i];
                    var target = targetArray[i];

                    if (!m_ForceMarker && pathfindReprocessedArray.Length > 0)
                    {
                        var pathfindReprocessed = pathfindReprocessedArray[i];
                        if (pathfindReprocessed.m_LastTargetEntity == target.m_Target)
                        {
                            continue;
                        }
                    }

                    pathOwner.m_State |= PathFlags.Obsolete;
                    pathOwnerArray[i] = pathOwner;

                    m_EntityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathfindReprocessed { m_LastTargetEntity = target.m_Target });
                }
            }
        }

        [BurstCompile]
        private struct ReprocessedCleanupJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer.ParallelWriter m_EntityCommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    m_EntityCommandBuffer.RemoveComponent<PathfindReprocessed>(unfilteredChunkIndex, entity);
                }
            }
        }

        public EndFrameBarrier m_EndFrameBarrier;

        public EntityQuery m_StartedPathfindEntityQuery;

        public EntityQuery m_FinishedPathfindEntityQuery;

        private bool m_ForceNextUpdate;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_StartedPathfindEntityQuery = SystemAPI.QueryBuilder().WithAll<PathOwner, Target, Car>().WithNone<Deleted>().Build();
            m_FinishedPathfindEntityQuery = SystemAPI.QueryBuilder().WithAll<PathfindReprocessed, Car>().WithNone<Deleted, PathOwner>().Build();
        }

        protected override void OnUpdate()
        {
            Dependency = JobChunkExtensions.ScheduleParallel(
                new ObsoleteMarkerJob
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_PathOwnerType = SystemAPI.GetComponentTypeHandle<PathOwner>(false),
                    m_TargetType = SystemAPI.GetComponentTypeHandle<Target>(true),
                    m_PathfindReprocessedType = SystemAPI.GetComponentTypeHandle<PathfindReprocessed>(true),
                    m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_ForceMarker = m_ForceNextUpdate,
                },
                m_StartedPathfindEntityQuery,
                Dependency
            );
            Dependency = JobChunkExtensions.ScheduleParallel(
                new ReprocessedCleanupJob { m_EntityType = SystemAPI.GetEntityTypeHandle(), m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter() },
                m_FinishedPathfindEntityQuery,
                Dependency
            );
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
        }

        public void UpdateAll()
        {
            m_ForceNextUpdate = true;
        }
    }
}
