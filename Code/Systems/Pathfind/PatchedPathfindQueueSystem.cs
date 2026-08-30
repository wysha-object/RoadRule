using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using HarmonyLib;
using RoadRule.Components;
using RoadRule.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static Game.Buildings.LocalEffectSystem;
using static Game.Pathfind.PathfindQueueSystem;

namespace RoadRule.Systems.Pathfind
{
    public static class PatchedPathfindQueueSystem
    {
        public static Queue<PatchedWorkerActions> m_WorkerActions = new Queue<PatchedWorkerActions>();
        public static Queue<PatchedWorkerActions> m_WorkerActionPool = new Queue<PatchedWorkerActions>();

        public static void PatchedScheduleWorkerJobs(ref PatchedPathfindQueueSystem.PatchedWorkerActions currentActions, ref PathfindQueueSystem __instance)
        {
            var instanceT = Traverse.Create(__instance);
            if (currentActions == null)
            {
                return;
            }
            var workerDataT = Traverse.Create(instanceT.Field("m_WorkerData").Method("get_Item", instanceT.Field("m_NextWorkerIndex").GetValue<int>()).GetValue());
            int num = math.min(currentActions.m_Actions.Length, math.max(instanceT.Field("m_MaxThreadCount").GetValue<int>(), currentActions.m_HighPriorityCount));
            int num2 = instanceT.Field("m_MaxThreadCount").GetValue<int>() + currentActions.m_HighPriorityCount;
            int count = instanceT.Field("m_ThreadData").Property("Count").GetValue<int>();
            PatchedPathfindQueueSystem.PatchedPathfindWorkerJob jobData = new PatchedPathfindQueueSystem.PatchedPathfindWorkerJob
            {
                m_RandomSeed = RandomSeed.Next(),
                m_PathfindData = workerDataT.Field("m_PathfindData").GetValue<NativePathfindData>(),
                m_PathfindHeuristicData = instanceT.Field("m_NetInitializeSystem").GetValue<NetInitializeSystem>().GetHeuristicData(),
                m_Actions = currentActions.m_Actions.AsArray(),
                m_ActionIndex = currentActions.m_ActionIndex,
                m_LaneRulesLookup = __instance.GetComponentLookup<LaneRules>(true),
                m_PrefabRefLookup = __instance.GetComponentLookup<PrefabRef>(true),
                m_CarLookup = __instance.GetComponentLookup<Car>(true),
                m_CarDataLookup = __instance.GetComponentLookup<CarData>(true),
                m_AmbulanceLookup = __instance.GetComponentLookup<Game.Vehicles.Ambulance>(true),
                m_DeliveryTruckLookup = __instance.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true),
                m_FireEngineLookup = __instance.GetComponentLookup<Game.Vehicles.FireEngine>(true),
                m_GarbageTruckLookup = __instance.GetComponentLookup<Game.Vehicles.GarbageTruck>(true),
                m_HearseLookup = __instance.GetComponentLookup<Game.Vehicles.Hearse>(true),
                m_MaintenanceVehicleLookup = __instance.GetComponentLookup<Game.Vehicles.MaintenanceVehicle>(true),
                m_PersonalCarLookup = __instance.GetComponentLookup<Game.Vehicles.PersonalCar>(true),
                m_PoliceCarLookup = __instance.GetComponentLookup<Game.Vehicles.PoliceCar>(true),
                m_PostVanLookup = __instance.GetComponentLookup<Game.Vehicles.PostVan>(true),
                m_PublicTransportLookup = __instance.GetComponentLookup<Game.Vehicles.PublicTransport>(true),
            };
            instanceT
                .Field("m_TransportLineSystem")
                .GetValue<TransportLineSystem>()
                .GetMaxTransportSpeed(out jobData.m_MaxPassengerTransportSpeed, out jobData.m_MaxCargoTransportSpeed);
            for (int i = 0; i < num; i++)
            {
                JobHandle jobHandle = workerDataT.Field("m_WriteHandle").GetValue<JobHandle>();
                var threadDataT = Traverse.Create(RuntimeHelpers.GetUninitializedObject(AccessTools.Inner(typeof(PathfindQueueSystem), "ThreadData")));
                if (instanceT.Field("m_ThreadData").Property("Count").GetValue<int>() >= num2)
                {
                    if (instanceT.Field("m_DependencyIndex").GetValue<int>() >= count)
                    {
                        instanceT.Field("m_DependencyIndex").SetValue(0);
                    }
                    threadDataT = Traverse.Create(instanceT.Field("m_ThreadData").Method("get_Item", instanceT.Field("m_DependencyIndex").GetValue<int>()).GetValue());
                    jobHandle = JobHandle.CombineDependencies(jobHandle, threadDataT.Field("m_JobHandle").GetValue<JobHandle>());
                }
                else if (instanceT.Field("m_AllocatorPool").Property("Count").GetValue<int>() != 0)
                {
                    threadDataT
                        .Field("m_Allocator")
                        .SetValue(
                            instanceT.Field("m_AllocatorPool").GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>()[
                                instanceT.Field("m_AllocatorPool").GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>().Count - 1
                            ]
                        );
                    instanceT
                        .Field("m_AllocatorPool")
                        .GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>()
                        .RemoveAt(instanceT.Field("m_AllocatorPool").GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>().Count - 1);
                }
                else
                {
                    threadDataT.Field("m_Allocator").SetValue(new AllocatorHelper<UnsafeLinearAllocator>(Allocator.Persistent));
                    threadDataT.Field("m_Allocator").GetValue<AllocatorHelper<UnsafeLinearAllocator>>().Allocator.Initialize(1048576u);
                }
                jobData.m_Allocator = threadDataT.Field("m_Allocator").GetValue<AllocatorHelper<UnsafeLinearAllocator>>();
                threadDataT.Field("m_JobHandle").SetValue(IJobExtensions.Schedule(jobData, jobHandle));
                currentActions.m_ReadHandle = JobHandle.CombineDependencies(currentActions.m_ReadHandle, threadDataT.Field("m_JobHandle").GetValue<JobHandle>());
                if (instanceT.Field("m_ThreadData").Property("Count").GetValue<int>() >= num2)
                {
                    instanceT.Field("m_ThreadData").Method("set_Item", instanceT.Field("m_DependencyIndex").GetValue<int>(), threadDataT.GetValue()).GetValue();
                    instanceT.Field("m_DependencyIndex").SetValue(instanceT.Field("m_DependencyIndex").GetValue<int>() + 1);
                }
                else
                {
                    instanceT.Field("m_ThreadData").Method("Add", threadDataT.GetValue()).GetValue();
                }
            }
            workerDataT.Field("m_ReadHandle").SetValue(JobHandle.CombineDependencies(workerDataT.Field("m_ReadHandle").GetValue<JobHandle>(), currentActions.m_ReadHandle));
            currentActions = null;
            instanceT.Field("m_LastWorkerIndex").SetValue(instanceT.Field("m_NextWorkerIndex").GetValue<int>());
        }

        public static void RequireWorkerActions(ref PatchedPathfindQueueSystem.PatchedWorkerActions currentActions)
        {
            if (currentActions == null)
            {
                if (!m_WorkerActionPool.TryDequeue(out currentActions))
                {
                    currentActions = new PatchedWorkerActions(Allocator.Persistent);
                }
                m_WorkerActions.Enqueue(currentActions);
            }
        }

        public class PatchedWorkerActions : IDisposable
        {
            public NativeList<PatchedWorkerAction> m_Actions;

            public NativeReference<int> m_ActionIndex;

            public JobHandle m_ReadHandle;

            public int m_HighPriorityCount;

            public PatchedWorkerActions(Allocator allocator)
            {
                m_Actions = new NativeList<PatchedWorkerAction>(100, allocator);
                m_ActionIndex = new NativeReference<int>(0, allocator);
                m_ReadHandle = default(JobHandle);
                m_HighPriorityCount = 0;
            }

            public unsafe void Add<T>(PathfindQueueSystem.ActionType type, bool isHighPriority, ref T data, Entity owner)
                where T : struct
            {
                ref NativeList<PatchedWorkerAction> reference = ref m_Actions;
                PatchedWorkerAction value = new PatchedWorkerAction
                {
                    m_Type = type,
                    m_ActionData = UnsafeUtility.AddressOf(ref data),
                    m_Owner = owner,
                };
                reference.Add(in value);
                if (isHighPriority)
                {
                    m_HighPriorityCount++;
                }
            }

            public void Clear()
            {
                m_ReadHandle.Complete();
                m_Actions.Clear();
                m_ActionIndex.Value = 0;
                m_HighPriorityCount = 0;
            }

            public void Dispose()
            {
                m_ReadHandle.Complete();
                m_Actions.Dispose();
                m_ActionIndex.Dispose();
            }
        }

        public struct PatchedWorkerAction
        {
            public PathfindQueueSystem.ActionType m_Type;

            public unsafe void* m_ActionData;

            public Entity m_Owner;
        }

        [BurstCompile]
        public struct PatchedPathfindWorkerJob : IJob
        {
            [ReadOnly]
            public RandomSeed m_RandomSeed;

            [ReadOnly]
            public NativePathfindData m_PathfindData;

            [ReadOnly]
            public PathfindHeuristicData m_PathfindHeuristicData;

            [ReadOnly]
            public float m_MaxPassengerTransportSpeed;

            [ReadOnly]
            public float m_MaxCargoTransportSpeed;

            [ReadOnly]
            public NativeArray<PatchedWorkerAction> m_Actions;

            [NativeDisableContainerSafetyRestriction]
            public NativeReference<int> m_ActionIndex;

            [NativeDisableUnsafePtrRestriction]
            public AllocatorHelper<UnsafeLinearAllocator> m_Allocator;

            [ReadOnly]
            public ComponentLookup<LaneRules> m_LaneRulesLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;

            [ReadOnly]
            public ComponentLookup<Car> m_CarLookup;

            [ReadOnly]
            public ComponentLookup<CarData> m_CarDataLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.Ambulance> m_AmbulanceLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.FireEngine> m_FireEngineLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.GarbageTruck> m_GarbageTruckLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.Hearse> m_HearseLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.MaintenanceVehicle> m_MaintenanceVehicleLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.PostVan> m_PostVanLookup;

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportLookup;

            public unsafe void Execute()
            {
                ref int location = ref m_ActionIndex.ValueAsRef();
                ref UnsafeLinearAllocator allocator = ref m_Allocator.Allocator;
                Allocator toAllocator = allocator.Handle.ToAllocator;
                while (true)
                {
                    int num = Interlocked.Increment(ref location) - 1;
                    if (num >= m_Actions.Length)
                    {
                        break;
                    }
                    PatchedWorkerAction workerAction = m_Actions[num];
                    switch (workerAction.m_Type)
                    {
                        case PathfindQueueSystem.ActionType.Pathfind:
                            Execute(ref UnsafeUtility.AsRef<PathfindActionData>(workerAction.m_ActionData), num, toAllocator, workerAction.m_Owner);
                            break;
                        case PathfindQueueSystem.ActionType.Coverage:
                            Execute(ref UnsafeUtility.AsRef<CoverageActionData>(workerAction.m_ActionData), toAllocator);
                            break;
                        case PathfindQueueSystem.ActionType.Availability:
                            Execute(ref UnsafeUtility.AsRef<AvailabilityActionData>(workerAction.m_ActionData), toAllocator);
                            break;
                    }
                    allocator.Rewind();
                }
                allocator.Rewind(updateSize: true);
            }

            private void Execute(ref PathfindActionData actionData, int index, Allocator allocator, Entity owner)
            {
                PatchedPathfindJobs.PatchedPathfindJob.Execute(
                    m_PathfindData,
                    allocator,
                    m_RandomSeed.GetRandom(index),
                    m_PathfindHeuristicData,
                    m_MaxPassengerTransportSpeed,
                    m_MaxCargoTransportSpeed,
                    ref actionData,
                    m_LaneRulesLookup,
                    m_PrefabRefLookup,
                    m_CarLookup,
                    m_CarDataLookup,
                    m_AmbulanceLookup,
                    m_DeliveryTruckLookup,
                    m_FireEngineLookup,
                    m_GarbageTruckLookup,
                    m_HearseLookup,
                    m_MaintenanceVehicleLookup,
                    m_PersonalCarLookup,
                    m_PoliceCarLookup,
                    m_PostVanLookup,
                    m_PublicTransportLookup,
                    owner
                );
                Interlocked.MemoryBarrier();
                actionData.m_State = PathfindActionState.Completed;
            }

            private void Execute(ref CoverageActionData actionData, Allocator allocator)
            {
                CoverageJobs.CoverageJob.Execute(m_PathfindData, allocator, ref actionData);
                Interlocked.MemoryBarrier();
                actionData.m_State = PathfindActionState.Completed;
            }

            private void Execute(ref AvailabilityActionData actionData, Allocator allocator)
            {
                AvailabilityJobs.AvailabilityJob.Execute(m_PathfindData, allocator, ref actionData);
                Interlocked.MemoryBarrier();
                actionData.m_State = PathfindActionState.Completed;
            }
        }
    }
}
