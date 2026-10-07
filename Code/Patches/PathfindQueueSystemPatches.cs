using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using HarmonyLib;
using RoadRule.Components;
using RoadRule.Systems.Pathfind;
using RoadRule.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static Game.Buildings.LocalEffectSystem;
using static Game.Pathfind.PathfindQueueSystem;

namespace RoadRule.Patches
{
    public static class PathfindQueueSystemPatches
    {
        [HarmonyPatch]
        public class OnUpdatePatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(PathfindQueueSystem), "OnUpdate", []);
            }

            public static bool Prefix(ref PathfindQueueSystem __instance)
            {
                var instanceT = Traverse.Create(__instance);

                var scheduleModificationJobMethod = AccessTools.Method(typeof(PathfindQueueSystem), "ScheduleModificationJob");

                bool flag = instanceT.Field("m_RequireDebug").GetValue<bool>();
                instanceT.Field("m_RequireDebug").SetValue(false);
                for (int i = 0; i < instanceT.Field("m_AllocatorPool").GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>().Count; i++)
                {
                    instanceT.Field("m_AllocatorPool").GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>()[i].Allocator.Rewind(true);
                }
                int num = 0;
                for (int j = 0; j < instanceT.Field("m_ThreadData").Property("Count").GetValue<int>(); j++)
                {
                    var valueT = Traverse.Create(instanceT.Field("m_ThreadData").Method("get_Item", j).GetValue());
                    if (valueT.Field("m_JobHandle").GetValue<JobHandle>().IsCompleted)
                    {
                        valueT.Field("m_JobHandle").GetValue<JobHandle>().Complete();
                        instanceT
                            .Field("m_AllocatorPool")
                            .GetValue<List<AllocatorHelper<UnsafeLinearAllocator>>>()
                            .Add(valueT.Field("m_Allocator").GetValue<AllocatorHelper<UnsafeLinearAllocator>>());
                        if (num < instanceT.Field("m_DependencyIndex").GetValue<int>())
                        {
                            instanceT.Field("m_DependencyIndex").SetValue(instanceT.Field("m_DependencyIndex").GetValue<int>() - 1);
                        }
                    }
                    else
                    {
                        instanceT.Field("m_ThreadData").Method("set_Item", num++, valueT.GetValue()).GetValue();
                    }
                }
                if (num < instanceT.Field("m_ThreadData").Property("Count").GetValue<int>())
                {
                    instanceT.Field("m_ThreadData").Method("RemoveRange", num, instanceT.Field("m_ThreadData").Property("Count").GetValue<int>() - num).GetValue();
                }
                for (int k = 0; k < instanceT.Field("m_WorkerData").Property("Count").GetValue<int>(); k++)
                {
                    var workerDataT = Traverse.Create(instanceT.Field("m_WorkerData").Method("get_Item", k).GetValue());
                    if (workerDataT.Field("m_WriteHandle").Property("IsCompleted").GetValue<bool>())
                    {
                        workerDataT.Field("m_WriteHandle").Method("Complete").GetValue();
                    }
                    if (workerDataT.Field("m_ReadHandle").Property("IsCompleted").GetValue<bool>())
                    {
                        workerDataT.Field("m_ReadHandle").Method("Complete").GetValue();
                    }
                }
                PatchedPathfindQueueSystem.PatchedWorkerActions result;
                while (PatchedPathfindQueueSystem.m_WorkerActions.TryPeek(out result) && result.m_ReadHandle.IsCompleted)
                {
                    result.Clear();
                    PatchedPathfindQueueSystem.m_WorkerActions.Dequeue();
                    PatchedPathfindQueueSystem.m_WorkerActionPool.Enqueue(result);
                }
                instanceT.Field("m_PathfindSetupSystem").GetValue<PathfindSetupSystem>().CompleteSetup();
                PatchedPathfindQueueSystem.PatchedWorkerActions currentActions = null;

                LaneRulesModifiedSystem laneRulesModifiedSystem = __instance.World.GetOrCreateSystemManaged<LaneRulesModifiedSystem>();
                NativeParallelHashMap<Entity, LaneRules>.ReadOnly laneRulesMap = laneRulesModifiedSystem.GetLaneRulesMap();

                var carLookup = __instance.GetComponentLookup<Car>(true);
                var citizenLookup = __instance.GetComponentLookup<Citizen>(true);
                var carKeeperLookup = __instance.GetComponentLookup<Game.Citizens.CarKeeper>(true);
                var residentLookup = __instance.GetComponentLookup<Game.Creatures.Resident>(true);
                var ambulanceLookup = __instance.GetComponentLookup<Game.Vehicles.Ambulance>(true);
                var deliveryTruckLookup = __instance.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
                var fireEngineLookup = __instance.GetComponentLookup<Game.Vehicles.FireEngine>(true);
                var garbageTruckLookup = __instance.GetComponentLookup<Game.Vehicles.GarbageTruck>(true);
                var hearseLookup = __instance.GetComponentLookup<Game.Vehicles.Hearse>(true);
                var maintenanceVehicleLookup = __instance.GetComponentLookup<Game.Vehicles.MaintenanceVehicle>(true);
                var personalCarLookup = __instance.GetComponentLookup<Game.Vehicles.PersonalCar>(true);
                var policeCarLookup = __instance.GetComponentLookup<Game.Vehicles.PoliceCar>(true);
                var postVanLookup = __instance.GetComponentLookup<Game.Vehicles.PostVan>(true);
                var publicTransportLookup = __instance.GetComponentLookup<Game.Vehicles.PublicTransport>(true);
                var taxiLookup = __instance.GetComponentLookup<Game.Vehicles.Taxi>(true);
                var randomTrafficRequestLookup = __instance.GetComponentLookup<RandomTrafficRequest>(true);
                var routeInfoLookup = __instance.GetComponentLookup<RouteInfo>(true);
                instanceT.Method("CompleteDependency").GetValue();

                try
                {
                    while (true)
                    {
                        ActionType actionType;
                        bool flag2;
                        bool flag3;
                        if (instanceT.Field("m_HighPriorityTypes").GetValue<Queue<ActionType>>().Count != 0)
                        {
                            actionType = instanceT.Field("m_HighPriorityTypes").GetValue<Queue<ActionType>>().Peek();
                            flag2 = true;
                            flag3 = false;
                        }
                        else if (instanceT.Field("m_ModificationTypes").GetValue<Queue<ActionType>>().Count != 0)
                        {
                            actionType = instanceT.Field("m_ModificationTypes").GetValue<Queue<ActionType>>().Peek();
                            flag2 = false;
                            flag3 = true;
                        }
                        else
                        {
                            if (instanceT.Field("m_ActionTypes").GetValue<Queue<ActionType>>().Count == 0)
                            {
                                break;
                            }
                            actionType = instanceT.Field("m_ActionTypes").GetValue<Queue<ActionType>>().Peek();
                            flag2 = false;
                            flag3 = false;
                        }
                        switch (actionType)
                        {
                            case ActionType.Create:
                            {
                                var method = scheduleModificationJobMethod.MakeGenericMethod(typeof(ModificationJobs.CreateEdgesJob));
                                ActionListItem<CreateAction> value5 = instanceT.Field("m_CreateActions").GetValue<ActionList<CreateAction>>().m_Items[
                                    instanceT.Field("m_CreateActions").GetValue<ActionList<CreateAction>>().m_NextIndex
                                ];
                                if (!value5.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value5.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                                value5.m_Dependencies = (JobHandle)method.Invoke(instanceT.GetValue(), [new ModificationJobs.CreateEdgesJob { m_Action = value5.m_Action }]);
                                value5.m_Flags = (value5.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_CreateActions").GetValue<ActionList<CreateAction>>().m_Items[
                                    instanceT.Field("m_CreateActions").GetValue<ActionList<CreateAction>>().m_NextIndex++
                                ] = value5;
                                break;
                            }
                            case ActionType.Update:
                            {
                                var method = scheduleModificationJobMethod.MakeGenericMethod(typeof(ModificationJobs.UpdateEdgesJob));
                                ActionListItem<UpdateAction> value10 = instanceT.Field("m_UpdateActions").GetValue<ActionList<UpdateAction>>().m_Items[
                                    instanceT.Field("m_UpdateActions").GetValue<ActionList<UpdateAction>>().m_NextIndex
                                ];
                                if (!value10.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value10.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                                value10.m_Dependencies = (JobHandle)method.Invoke(instanceT.GetValue(), [new ModificationJobs.UpdateEdgesJob { m_Action = value10.m_Action }]);
                                value10.m_Flags = (value10.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_UpdateActions").GetValue<ActionList<UpdateAction>>().m_Items[
                                    instanceT.Field("m_UpdateActions").GetValue<ActionList<UpdateAction>>().m_NextIndex++
                                ] = value10;
                                break;
                            }
                            case ActionType.Delete:
                            {
                                var method = scheduleModificationJobMethod.MakeGenericMethod(typeof(ModificationJobs.DeleteEdgesJob));
                                ActionListItem<DeleteAction> value7 = instanceT.Field("m_DeleteActions").GetValue<ActionList<DeleteAction>>().m_Items[
                                    instanceT.Field("m_DeleteActions").GetValue<ActionList<DeleteAction>>().m_NextIndex
                                ];
                                if (!value7.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value7.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                                value7.m_Dependencies = (JobHandle)method.Invoke(instanceT.GetValue(), [new ModificationJobs.DeleteEdgesJob { m_Action = value7.m_Action }]);
                                value7.m_Flags = (value7.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_DeleteActions").GetValue<ActionList<DeleteAction>>().m_Items[
                                    instanceT.Field("m_DeleteActions").GetValue<ActionList<DeleteAction>>().m_NextIndex++
                                ] = value7;
                                break;
                            }
                            case ActionType.Pathfind:
                            {
                                ActionListItem<PathfindAction> value9 = instanceT.Field("m_PathfindActions").GetValue<ActionList<PathfindAction>>().m_Items[
                                    instanceT.Field("m_PathfindActions").GetValue<ActionList<PathfindAction>>().m_NextIndex
                                ];
                                if (!value9.m_Dependencies.IsCompleted || (flag && (value9.m_Flags & PathFlags.Debug) == 0))
                                {
                                    return false;
                                }
                                value9.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.RequireWorkerActions(ref currentActions);
                                LaneRulesUtils.GetCarParameters(
                                    value9.m_Action.data.m_Parameters.m_Methods,
                                    value9.m_Action.data.m_OriginType,
                                    value9.m_Action.data.m_DestinationType,
                                    value9.m_Owner,
                                    carLookup,
                                    citizenLookup,
                                    carKeeperLookup,
                                    residentLookup,
                                    ambulanceLookup,
                                    deliveryTruckLookup,
                                    fireEngineLookup,
                                    garbageTruckLookup,
                                    hearseLookup,
                                    maintenanceVehicleLookup,
                                    personalCarLookup,
                                    policeCarLookup,
                                    postVanLookup,
                                    publicTransportLookup,
                                    taxiLookup,
                                    randomTrafficRequestLookup,
                                    routeInfoLookup,
                                    out var carParameters
                                );
                                currentActions.Add(actionType, flag2, ref value9.m_Action.data, carParameters);
                                value9.m_Flags = (value9.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_PathfindActions").GetValue<ActionList<PathfindAction>>().m_Items[
                                    instanceT.Field("m_PathfindActions").GetValue<ActionList<PathfindAction>>().m_NextIndex++
                                ] = value9;
                                if (flag2)
                                {
                                    instanceT.Field("m_PathfindActions").GetValue<ActionList<PathfindAction>>().m_PriorityCount--;
                                }
                                break;
                            }
                            case ActionType.Coverage:
                            {
                                ActionListItem<CoverageAction> value3 = instanceT.Field("m_CoverageActions").GetValue<ActionList<CoverageAction>>().m_Items[
                                    instanceT.Field("m_CoverageActions").GetValue<ActionList<CoverageAction>>().m_NextIndex
                                ];
                                if (!value3.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value3.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.RequireWorkerActions(ref currentActions);
                                currentActions.Add(actionType, flag2, ref value3.m_Action.data, new LaneRulesUtils.CarParameters());
                                value3.m_Flags = (value3.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_CoverageActions").GetValue<ActionList<CoverageAction>>().m_Items[
                                    instanceT.Field("m_CoverageActions").GetValue<ActionList<CoverageAction>>().m_NextIndex++
                                ] = value3;
                                if (flag2)
                                {
                                    instanceT.Field("m_CoverageActions").GetValue<ActionList<CoverageAction>>().m_PriorityCount--;
                                }
                                break;
                            }
                            case ActionType.Availability:
                            {
                                ActionListItem<AvailabilityAction> value8 = instanceT.Field("m_AvailabilityActions").GetValue<ActionList<AvailabilityAction>>().m_Items[
                                    instanceT.Field("m_AvailabilityActions").GetValue<ActionList<AvailabilityAction>>().m_NextIndex
                                ];
                                if (!value8.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value8.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.RequireWorkerActions(ref currentActions);
                                currentActions.Add(actionType, flag2, ref value8.m_Action.data, new LaneRulesUtils.CarParameters());
                                value8.m_Flags = (value8.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_AvailabilityActions").GetValue<ActionList<AvailabilityAction>>().m_Items[
                                    instanceT.Field("m_AvailabilityActions").GetValue<ActionList<AvailabilityAction>>().m_NextIndex++
                                ] = value8;
                                if (flag2)
                                {
                                    instanceT.Field("m_CoverageActions").GetValue<ActionList<CoverageAction>>().m_PriorityCount--;
                                }
                                break;
                            }
                            case ActionType.Density:
                            {
                                var method = scheduleModificationJobMethod.MakeGenericMethod(typeof(ModificationJobs.SetDensityJob));
                                ActionListItem<DensityAction> value6 = instanceT.Field("m_DensityActions").GetValue<ActionList<DensityAction>>().m_Items[
                                    instanceT.Field("m_DensityActions").GetValue<ActionList<DensityAction>>().m_NextIndex
                                ];
                                if (!value6.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value6.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                                value6.m_Dependencies = (JobHandle)method.Invoke(instanceT.GetValue(), [new ModificationJobs.SetDensityJob { m_Action = value6.m_Action }]);
                                value6.m_Flags = (value6.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_DensityActions").GetValue<ActionList<DensityAction>>().m_Items[
                                    instanceT.Field("m_DensityActions").GetValue<ActionList<DensityAction>>().m_NextIndex++
                                ] = value6;
                                break;
                            }
                            case ActionType.Time:
                            {
                                var method = scheduleModificationJobMethod.MakeGenericMethod(typeof(ModificationJobs.SetTimeJob));
                                ActionListItem<TimeAction> value4 = instanceT.Field("m_TimeActions").GetValue<ActionList<TimeAction>>().m_Items[
                                    instanceT.Field("m_TimeActions").GetValue<ActionList<TimeAction>>().m_NextIndex
                                ];
                                if (!value4.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value4.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                                value4.m_Dependencies = (JobHandle)method.Invoke(instanceT.GetValue(), [new ModificationJobs.SetTimeJob { m_Action = value4.m_Action }]);
                                value4.m_Flags = (value4.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_TimeActions").GetValue<ActionList<TimeAction>>().m_Items[
                                    instanceT.Field("m_TimeActions").GetValue<ActionList<TimeAction>>().m_NextIndex++
                                ] = value4;
                                break;
                            }
                            case ActionType.Flow:
                            {
                                var method = scheduleModificationJobMethod.MakeGenericMethod(typeof(ModificationJobs.SetFlowJob));
                                ActionListItem<FlowAction> value2 = instanceT.Field("m_FlowActions").GetValue<ActionList<FlowAction>>().m_Items[
                                    instanceT.Field("m_FlowActions").GetValue<ActionList<FlowAction>>().m_NextIndex
                                ];
                                if (!value2.m_Dependencies.IsCompleted)
                                {
                                    return false;
                                }
                                value2.m_Dependencies.Complete();
                                PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                                value2.m_Dependencies = (JobHandle)method.Invoke(instanceT.GetValue(), [new ModificationJobs.SetFlowJob { m_Action = value2.m_Action }]);
                                value2.m_Flags = (value2.m_Flags & ~PathFlags.Pending) | PathFlags.Scheduled;
                                instanceT.Field("m_FlowActions").GetValue<ActionList<FlowAction>>().m_Items[
                                    instanceT.Field("m_FlowActions").GetValue<ActionList<FlowAction>>().m_NextIndex++
                                ] = value2;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            instanceT.Field("m_HighPriorityTypes").GetValue<Queue<ActionType>>().Dequeue();
                        }
                        else if (flag3)
                        {
                            instanceT.Field("m_ModificationTypes").GetValue<Queue<ActionType>>().Dequeue();
                        }
                        else
                        {
                            instanceT.Field("m_ActionTypes").GetValue<Queue<ActionType>>().Dequeue();
                        }
                    }
                }
                finally
                {
                    PatchedPathfindQueueSystem.PatchedScheduleWorkerJobs(ref currentActions, ref __instance, laneRulesMap);
                }

                return false;
            }
        }
    }

    [HarmonyPatch]
    public class OnDestroyPatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PathfindQueueSystem), "OnDestroy", []);
        }

        public static bool Prefix(ref PathfindQueueSystem __instance)
        {
            PatchedPathfindQueueSystem.PatchedWorkerActions result;
            while (PatchedPathfindQueueSystem.m_WorkerActions.TryDequeue(out result))
            {
                result.Dispose();
            }
            PatchedPathfindQueueSystem.PatchedWorkerActions result2;
            while (PatchedPathfindQueueSystem.m_WorkerActionPool.TryDequeue(out result2))
            {
                result2.Dispose();
            }
            return true;
        }
    }

    [HarmonyPatch]
    public class PreDeserializePatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PathfindQueueSystem), "PreDeserialize", [typeof(Context)]);
        }

        public static bool Prefix(ref PathfindQueueSystem __instance)
        {
            PatchedPathfindQueueSystem.PatchedWorkerActions result;
            while (PatchedPathfindQueueSystem.m_WorkerActions.TryDequeue(out result))
            {
                result.Clear();
                PatchedPathfindQueueSystem.m_WorkerActionPool.Enqueue(result);
            }
            return true;
        }
    }
}
