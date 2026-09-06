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
using Game.Simulation;
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

            [ReadOnly]
            public ComponentTypeHandle<PathfindReprocessRequest> m_PathfindReprocessRequestType;

            public ComponentTypeHandle<PathOwner> m_PathOwnerType;

            [ReadOnly]
            public ComponentTypeHandle<Target> m_TargetType;

            public EntityCommandBuffer.ParallelWriter m_EntityCommandBuffer;

            public uint m_Frame;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<PathfindReprocessRequest> pathfindReprocessRequestArray = chunk.GetNativeArray(ref m_PathfindReprocessRequestType);
                NativeArray<PathOwner> pathOwnerArray = chunk.GetNativeArray(ref m_PathOwnerType);
                NativeArray<Target> targetArray = chunk.GetNativeArray(ref m_TargetType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var pathfindReprocessRequest = pathfindReprocessRequestArray[i];
                    var pathOwner = pathOwnerArray[i];
                    var target = targetArray[i];

                    if (m_Frame < pathfindReprocessRequest.m_Frame)
                    {
                        continue;
                    }

                    pathOwner.m_State |= PathFlags.Obsolete;
                    pathOwnerArray[i] = pathOwner;

                    m_EntityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathfindReprocessed() { m_LastTargetEntity = target.m_Target });
                    m_EntityCommandBuffer.RemoveComponent<PathfindReprocessRequest>(unfilteredChunkIndex, entity);
                }
            }
        }

        public EndFrameBarrier m_EndFrameBarrier;

        public SimulationSystem m_SimulationSystem;

        public EntityQuery m_NeedReprocessEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_NeedReprocessEntityQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = [ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<PathfindReprocessRequest>()],
                    None = [ComponentType.ReadOnly<Deleted>()],
                }
            );
        }

        protected override void OnUpdate()
        {
            Dependency = JobChunkExtensions.ScheduleParallel(
                new ObsoleteMarkerJob
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_PathfindReprocessRequestType = SystemAPI.GetComponentTypeHandle<PathfindReprocessRequest>(true),
                    m_PathOwnerType = SystemAPI.GetComponentTypeHandle<PathOwner>(false),
                    m_TargetType = SystemAPI.GetComponentTypeHandle<Target>(true),
                    m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_Frame = m_SimulationSystem.frameIndex,
                },
                m_NeedReprocessEntityQuery,
                Dependency
            );
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
        }
    }
}
