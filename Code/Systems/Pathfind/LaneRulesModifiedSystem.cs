using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
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
    public unsafe partial class LaneRulesModifiedSystem : GameSystemBase
    {
        [BurstCompile]
        private struct LaneRulesMapGenerateJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<LaneRules> m_LaneRulesType;

            public NativeParallelHashMap<Entity, LaneRules>.ParallelWriter m_LaneRulesMap;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<LaneRules> laneRulesArray = chunk.GetNativeArray(ref m_LaneRulesType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var laneRules = laneRulesArray[i];
                    m_LaneRulesMap.TryAdd(entity, laneRules);
                }
            }
        }

        [BurstCompile]
        private struct ReprocessMarkJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Target> m_TargetType;

            public EntityCommandBuffer.ParallelWriter m_EntityCommandBuffer;

            public uint m_Frame;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> m_RequestCount;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Target> targetArray = chunk.GetNativeArray(ref m_TargetType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var target = targetArray[i];

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

                    m_EntityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathfindReprocessRequest { m_Frame = m_Frame + minIndex });
                }
            }
        }

        public EndFrameBarrier m_EndFrameBarrier;

        public SimulationSystem m_SimulationSystem;

        public EntityQuery m_UpdatedLaneRulesEntityQuery;

        public EntityQuery m_LaneRulesEntityQuery;

        public EntityQuery m_NeedMarkEntityQuery;

        private JobHandle m_LaneRulesMapGenerateDependency;

        private JobHandle m_LaneRulesMapDependency;

        private NativeParallelHashMap<Entity, LaneRules> m_LaneRulesMap;

        private bool m_Loaded;

        public NativeParallelHashMap<Entity, LaneRules>.ReadOnly GetLaneRulesMap()
        {
            m_LaneRulesMapGenerateDependency.Complete();
            CompleteDependency();
            return m_LaneRulesMap.AsReadOnly();
        }

        public void AddDependency(JobHandle jobHandle)
        {
            m_LaneRulesMapDependency = JobHandle.CombineDependencies(m_LaneRulesMapDependency, jobHandle);
        }

        private bool GetLoaded()
        {
            if (m_Loaded)
            {
                m_Loaded = false;
                return true;
            }
            return false;
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            m_Loaded = true;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_UpdatedLaneRulesEntityQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = [ComponentType.ReadOnly<CarLane>()],
                    Any = [ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Deleted>()],
                    None = [ComponentType.ReadOnly<Temp>()],
                }
            );
            m_LaneRulesEntityQuery = GetEntityQuery(
                new EntityQueryDesc { All = [ComponentType.ReadOnly<LaneRules>()], None = [ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>()] }
            );
            m_NeedMarkEntityQuery = GetEntityQuery(
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
                    None = [ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PathfindReprocessRequest>()],
                }
            );

            m_LaneRulesMap = new NativeParallelHashMap<Entity, LaneRules>(0, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            if (GetLoaded() || m_UpdatedLaneRulesEntityQuery.CalculateEntityCount() > 0)
            {
                m_LaneRulesMap.Dispose(m_LaneRulesMapDependency);
                m_LaneRulesMap = new NativeParallelHashMap<Entity, LaneRules>(m_LaneRulesEntityQuery.CalculateEntityCount(), Allocator.Persistent);
                m_LaneRulesMapGenerateDependency = JobChunkExtensions.ScheduleParallel(
                    new LaneRulesMapGenerateJob
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_LaneRulesType = SystemAPI.GetComponentTypeHandle<LaneRules>(true),
                        m_LaneRulesMap = m_LaneRulesMap.AsParallelWriter(),
                    },
                    m_LaneRulesEntityQuery,
                    Dependency
                );
                m_LaneRulesMapDependency = m_LaneRulesMapGenerateDependency;

                var requestCount = new NativeArray<int>(256, Allocator.Persistent);
                Dependency = JobChunkExtensions.ScheduleParallel(
                    new ReprocessMarkJob
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_TargetType = SystemAPI.GetComponentTypeHandle<Target>(true),
                        m_EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_Frame = m_SimulationSystem.frameIndex,
                        m_RequestCount = requestCount,
                    },
                    m_NeedMarkEntityQuery,
                    Dependency
                );
                m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
                requestCount.Dispose(Dependency);

                Dependency = JobHandle.CombineDependencies(Dependency, m_LaneRulesMapGenerateDependency);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_LaneRulesMap.Dispose(m_LaneRulesMapDependency);
        }
    }
}
