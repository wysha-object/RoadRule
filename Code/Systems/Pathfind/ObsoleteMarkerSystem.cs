using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs.Effects;
using Game.Rendering;
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

            public EntityCommandBuffer.ParallelWriter m_EntityCommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<PathOwner> pathOwnerArray = chunk.GetNativeArray(ref m_PathOwnerType);
                NativeArray<Target> targetArray = chunk.GetNativeArray(ref m_TargetType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var pathOwner = pathOwnerArray[i];
                    var target = targetArray[i];

                    pathOwner.m_State |= PathFlags.Obsolete;
                    pathOwnerArray[i] = pathOwner;

                    m_EntityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathfindReprocessed() { m_LastTargetEntity = target.m_Target });
                    m_EntityCommandBuffer.RemoveComponent<PathfindNeedObsoleteFlag>(unfilteredChunkIndex, entity);
                }
            }
        }

        public EndFrameBarrier m_EndFrameBarrier;

        public EntityQuery m_NeedReprocessEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_NeedReprocessEntityQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = [ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<PathfindNeedObsoleteFlag>()],
                    None = [ComponentType.ReadOnly<Deleted>()],
                }
            );
        }

        private int m_Frame;

        protected override void OnUpdate()
        {
            if (m_Frame++ % 256 != 0)
            {
                return;
            }
            Dependency = JobChunkExtensions.ScheduleParallel(
                new ObsoleteMarkerJob
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_PathOwnerType = SystemAPI.GetComponentTypeHandle<PathOwner>(false),
                    m_TargetType = SystemAPI.GetComponentTypeHandle<Target>(true),
                    m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                },
                m_NeedReprocessEntityQuery,
                Dependency
            );
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
        }
    }
}
