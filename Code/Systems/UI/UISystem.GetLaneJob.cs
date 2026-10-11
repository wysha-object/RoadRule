using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Newtonsoft.Json;
using RoadRule.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using static System.Security.Cryptography.ECCurve;
using static RoadRule.Components.LaneRules;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        private static readonly int MAX_DISPLAY_DISTANCE = 2000;

        private struct GetLaneResult
        {
            public LaneValue lane;
            public float defaultSpeedLimit;
        }

        [BurstCompile]
        private struct GetLaneJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Curve> m_CurveType;

            [ReadOnly]
            public ComponentTypeHandle<Lane> m_LaneType;

            [ReadOnly]
            public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

            [ReadOnly]
            public ComponentTypeHandle<LaneRules> m_LaneRulesType;

            [ReadOnly]
            public ComponentTypeHandle<Owner> m_OwnerType;

            [ReadOnly]
            public ComponentLookup<Composition> m_CompositionLookup;

            [ReadOnly]
            public ComponentLookup<RoadComposition> m_RoadCompositionLookup;

            public NativeList<GetLaneResult>.ParallelWriter m_ResultList;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var curveArray = chunk.GetNativeArray(ref m_CurveType);
                var laneArray = chunk.GetNativeArray(ref m_LaneType);
                var edgeLaneArray = chunk.GetNativeArray(ref m_EdgeLaneType);
                var carLaneArray = chunk.GetNativeArray(ref m_CarLaneType);
                var laneRulesArray = chunk.GetNativeArray(ref m_LaneRulesType);
                var ownerArray = chunk.GetNativeArray(ref m_OwnerType);

                for (int i = 0; i < curveArray.Length; i++)
                {
                    var curve = curveArray[i];
                    var lane = laneArray[i];
                    var edgeLane = edgeLaneArray[i];
                    var carLane = carLaneArray[i];
                    LaneRules laneRules;
                    if (laneRulesArray.Length > 0)
                    {
                        laneRules = laneRulesArray[i];
                    }
                    else
                    {
                        laneRules = new LaneRules();
                    }

                    var delta = edgeLane.m_EdgeDelta;
                    if (
                        (delta.x == 0 || delta.y == 0)
                        && m_CompositionLookup.TryGetComponent(ownerArray[i].m_Owner, out var composition)
                        && m_RoadCompositionLookup.TryGetComponent(composition.m_Edge, out var roadComposition)
                    )
                    {
                        float defaultSpeedLimit = roadComposition.m_SpeedLimit * 1.8f;

                        if (
                            laneRules.m_VehicleType.m_Ambulance == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_DeliveryTruck == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_FireEngine == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_GarbageTruck == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_Hearse == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_MaintenanceVehicle == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_PersonalCar == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_PoliceCar == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_PostVan == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_PublicTransport == LaneRules.RuleOptions.None
                            && laneRules.m_VehicleType.m_Taxi == LaneRules.RuleOptions.None
                            && defaultSpeedLimit == carLane.m_DefaultSpeedLimit
                        )
                        {
                            continue;
                        }

                        var worldPosition = delta.x == 0 ? (curve.m_Bezier.c + curve.m_Bezier.d) * 0.5f : (curve.m_Bezier.a + curve.m_Bezier.b) * 0.5f;
                        m_ResultList.AddNoResize(
                            new GetLaneResult
                            {
                                lane = new LaneValue()
                                {
                                    laneIndex = lane.m_MiddleNode.GetLaneIndex() & 0xff,
                                    position = new PositionValue()
                                    {
                                        x = worldPosition.x,
                                        y = worldPosition.y,
                                        z = worldPosition.z,
                                    },
                                    laneRules = LaneRulesValue.FromRules(laneRules),
                                    carLane = CarLaneValue.FromCarLane(carLane),
                                },
                                defaultSpeedLimit = defaultSpeedLimit,
                            }
                        );
                    }
                }
            }
        }

        private void ScheduleGetLaneJob(EntityQuery entityQuery, NativeList<GetLaneResult> laneValueList)
        {
            JobChunkExtensions
                .ScheduleParallel(
                    new GetLaneJob()
                    {
                        m_CurveType = SystemAPI.GetComponentTypeHandle<Curve>(true),
                        m_LaneType = SystemAPI.GetComponentTypeHandle<Lane>(true),
                        m_EdgeLaneType = SystemAPI.GetComponentTypeHandle<EdgeLane>(true),
                        m_CarLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.CarLane>(true),
                        m_LaneRulesType = SystemAPI.GetComponentTypeHandle<LaneRules>(true),
                        m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(true),
                        m_CompositionLookup = SystemAPI.GetComponentLookup<Composition>(true),
                        m_RoadCompositionLookup = SystemAPI.GetComponentLookup<RoadComposition>(true),
                        m_ResultList = laneValueList.AsParallelWriter(),
                    },
                    entityQuery,
                    new JobHandle()
                )
                .Complete();
            return;
        }
    }
}
