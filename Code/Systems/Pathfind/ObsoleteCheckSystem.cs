using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Rendering;
using Game.Simulation;
using Game.Vehicles;
using RoadRule.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace RoadRule.Systems.Pathfind
{
    public unsafe partial class ObsoleteCheckSystem : GameSystemBase
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

            public uint m_Frame;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> m_RequestCount;

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

                    uint minIndex = 0;
                    var minValue = int.MaxValue;
                    for (int j = 0; j < m_RequestCount.Length; j++)
                    {
                        if (m_RequestCount[j] < minValue)
                        {
                            minIndex = (uint)j;
                            minValue = m_RequestCount[j];
                        }
                    }
                    Interlocked.Increment(ref ((int*)NativeArrayUnsafeUtility.GetUnsafePtr(m_RequestCount))[(int)minIndex]);

                    m_EntityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathfindReprocessRequest { m_Frame = m_Frame + 8 + minIndex });
                }
            }
        }

        [BurstCompile]
        private struct ShiftRightJob : IJob
        {
            public NativeArray<int> m_RequestCount;

            public void Execute()
            {
                for (int i = 0; i < m_RequestCount.Length - 1; i++)
                {
                    m_RequestCount[i] = m_RequestCount[i + 1];
                }
                m_RequestCount[m_RequestCount.Length - 1] = 0;
            }
        }

        public EndFrameBarrier m_EndFrameBarrier;

        public SimulationSystem m_SimulationSystem;

        public EntityQuery m_StartedPathfindEntityQuery;

        private bool m_ForceMarkNext;

        private NativeArray<int> m_RequestCount;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
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
                    None = [ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<PathfindReprocessRequest>()],
                }
            );

            m_RequestCount = new NativeArray<int>(256, Allocator.Persistent);
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
                    m_Frame = m_SimulationSystem.frameIndex,
                    m_RequestCount = m_RequestCount,
                },
                m_StartedPathfindEntityQuery,
                Dependency
            );
            Dependency = new ShiftRightJob { m_RequestCount = m_RequestCount }.Schedule(Dependency);
            m_ForceMarkNext = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_RequestCount.Dispose();
        }

        public void UpdateAll()
        {
            m_ForceMarkNext = true;
        }
    }
}
