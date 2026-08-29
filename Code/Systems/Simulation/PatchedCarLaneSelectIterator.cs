// Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Game.Simulation.CarLaneSelectIterator
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using RoadRule.Components;
using RoadRule.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Systems.Simulation;

public struct PatchedCarLaneSelectIterator
{
    public ComponentLookup<Owner> m_OwnerData;

    public ComponentLookup<Lane> m_LaneData;

    public ComponentLookup<CarLane> m_CarLaneData;

    public ComponentLookup<SlaveLane> m_SlaveLaneData;

    public ComponentLookup<LaneReservation> m_LaneReservationData;

    public ComponentLookup<Moving> m_MovingData;

    public ComponentLookup<Car> m_CarData;

    public ComponentLookup<Controller> m_ControllerData;

    public ComponentLookup<LaneRules> m_CarLaneRulesData;

    public BufferLookup<SubLane> m_Lanes;

    public BufferLookup<LaneObject> m_LaneObjects;

    public Entity m_Entity;

    public Entity m_Blocker;

    public int m_Priority;

    public bool m_LeftHandTraffic;

    public bool m_TurnAhead;

    public Game.Net.CarLaneFlags m_ForbidLaneFlags;

    public Game.Net.CarLaneFlags m_PreferLaneFlags;

    public CarFlags m_CarFlags;

    public SizeClass m_SizeClass;

    public EnergyTypes m_EnergyTypes;

    public PathMethod m_PathMethods;

    private const float TURN_AHEAD_LANE_SWITCH_COST_FACTOR = 7f;

    private NativeArray<float> m_Buffer;

    private int m_BufferPos;

    private float m_LaneSwitchCost;

    private float m_LaneSwitchBaseCost;

    private Entity m_PrevLane;

    public void SetBuffer(ref CarLaneSelectBuffer buffer)
    {
        m_Buffer = buffer.Ensure();
    }

    public void CalculateLaneCosts(CarNavigationLane navLaneData, int index)
    {
        if ((navLaneData.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.FixedLane)) != 0 || !m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
        {
            return;
        }
        SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
        Owner owner = m_OwnerData[navLaneData.m_Lane];
        DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
        int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
        float laneObjectCost = math.abs(navLaneData.m_CurvePosition.y - navLaneData.m_CurvePosition.x) * 0.49f;
        for (int i = slaveLane.m_MinIndex; i <= num; i++)
        {
            SubLane subLane = dynamicBuffer[i];
            float num2 = CalculateLaneObjectCost(laneObjectCost, index, subLane.m_SubLane, navLaneData.m_Flags);
            if (m_LaneReservationData.TryGetComponent(subLane.m_SubLane, out var componentData))
            {
                num2 += GetLanePriorityCost(componentData.GetPriority());
            }
            if (m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData2))
            {
                if (!m_CarLaneRulesData.TryGetComponent(subLane.m_SubLane, out var carLaneRules))
                {
                    carLaneRules = new LaneRules();
                }
                num2 += GetLaneDriveCost(componentData2.m_Flags, subLane.m_PathMethods, i, slaveLane.m_MinIndex, num, carLaneRules);
            }
            m_Buffer[m_BufferPos++] = num2;
        }
    }

    private float CalculateLaneObjectCost(float laneObjectCost, int index, Entity lane, Game.Vehicles.CarLaneFlags laneFlags)
    {
        float num = 0f;
        if (m_LaneObjects.HasBuffer(lane))
        {
            DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjects[lane];
            if (index < 2 && m_Blocker != Entity.Null)
            {
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    LaneObject laneObject = dynamicBuffer[i];
                    num = ((!(laneObject.m_LaneObject == m_Blocker)) ? (num + laneObjectCost) : (num + CalculateLaneObjectCost(laneObject, laneObjectCost, laneFlags)));
                }
            }
            else
            {
                num += laneObjectCost * (float)dynamicBuffer.Length;
            }
        }
        return num;
    }

    private float CalculateLaneObjectCost(float laneObjectCost, Entity lane, float minCurvePosition, Game.Vehicles.CarLaneFlags laneFlags)
    {
        float num = 0f;
        if (m_LaneObjects.HasBuffer(lane))
        {
            DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjects[lane];
            for (int i = 0; i < dynamicBuffer.Length; i++)
            {
                LaneObject laneObject = dynamicBuffer[i];
                if (
                    laneObject.m_CurvePosition.y > minCurvePosition
                    && !(laneObject.m_LaneObject == m_Entity)
                    && (!m_ControllerData.HasComponent(laneObject.m_LaneObject) || !(m_ControllerData[laneObject.m_LaneObject].m_Controller == m_Entity))
                )
                {
                    num += CalculateLaneObjectCost(laneObject, laneObjectCost, laneFlags);
                }
            }
        }
        return num;
    }

    private float CalculateLaneObjectCost(LaneObject laneObject, float laneObjectCost, Game.Vehicles.CarLaneFlags laneFlags)
    {
        if (!m_MovingData.HasComponent(laneObject.m_LaneObject))
        {
            if (
                m_CarData.HasComponent(laneObject.m_LaneObject)
                && (m_CarData[laneObject.m_LaneObject].m_Flags & CarFlags.Queueing) != 0
                && (laneFlags & Game.Vehicles.CarLaneFlags.Queue) != 0
            )
            {
                return laneObjectCost;
            }
            return math.lerp(10000000f, 9000000f, laneObject.m_CurvePosition.y);
        }
        return laneObjectCost;
    }

    public void CalculateLaneCosts(CarNavigationLane navLaneData, CarNavigationLane nextNavLaneData, int index)
    {
        if (
            (navLaneData.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.FixedLane)) == 0
            && m_SlaveLaneData.TryGetComponent(navLaneData.m_Lane, out var componentData)
        )
        {
            Owner owner = m_OwnerData[navLaneData.m_Lane];
            DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
            int num = math.min(componentData.m_MaxIndex, dynamicBuffer.Length - 1);
            m_LaneSwitchCost = m_LaneSwitchBaseCost + math.select(1f, 5f, (componentData.m_Flags & SlaveLaneFlags.AllowChange) == 0);
            if (m_TurnAhead)
            {
                m_LaneSwitchCost *= 7f;
            }
            float laneObjectCost = math.abs(navLaneData.m_CurvePosition.y - navLaneData.m_CurvePosition.x) * 0.49f * (2f / (float)(2 + index));
            if (
                (nextNavLaneData.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.FixedLane)) == 0
                && m_SlaveLaneData.TryGetComponent(nextNavLaneData.m_Lane, out var componentData2)
            )
            {
                Owner owner2 = m_OwnerData[nextNavLaneData.m_Lane];
                DynamicBuffer<SubLane> dynamicBuffer2 = m_Lanes[owner2.m_Owner];
                int num2 = math.min(componentData2.m_MaxIndex, dynamicBuffer2.Length - 1);
                int num3 = m_BufferPos - (num2 - componentData2.m_MinIndex + 1);
                float falseValue = 1000000f;
                int num4 = 100000;
                float trueValue = 1000000f;
                int num5 = -100000;
                for (int i = componentData.m_MinIndex; i <= num; i++)
                {
                    SubLane subLane = dynamicBuffer[i];
                    Lane lane = m_LaneData[subLane.m_SubLane];
                    float num6 = 1000000f;
                    int num7;
                    int num8;
                    if ((nextNavLaneData.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) != 0)
                    {
                        num7 = componentData2.m_MinIndex;
                        num8 = num2;
                    }
                    else
                    {
                        num7 = 100000;
                        num8 = -100000;
                        if ((componentData.m_Flags & SlaveLaneFlags.MiddleEnd) != 0)
                        {
                            for (int j = componentData2.m_MinIndex; j <= num2; j++)
                            {
                                Lane lane2 = m_LaneData[dynamicBuffer2[j].m_SubLane];
                                if (lane.m_EndNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode))
                                {
                                    num7 = math.min(num7, j);
                                    num8 = j;
                                }
                            }
                        }
                        else if ((componentData2.m_Flags & SlaveLaneFlags.MiddleStart) != 0)
                        {
                            for (int k = componentData2.m_MinIndex; k <= num2; k++)
                            {
                                Lane lane3 = m_LaneData[dynamicBuffer2[k].m_SubLane];
                                if (lane.m_MiddleNode.EqualsIgnoreCurvePos(lane3.m_StartNode))
                                {
                                    num7 = math.min(num7, k);
                                    num8 = k;
                                }
                            }
                        }
                        else
                        {
                            for (int l = componentData2.m_MinIndex; l <= num2; l++)
                            {
                                Lane lane4 = m_LaneData[dynamicBuffer2[l].m_SubLane];
                                if (lane.m_EndNode.Equals(lane4.m_StartNode))
                                {
                                    num7 = math.min(num7, l);
                                    num8 = l;
                                }
                            }
                        }
                    }
                    if (num7 <= num8)
                    {
                        int num9 = num3;
                        for (int m = componentData2.m_MinIndex; m < num7; m++)
                        {
                            num6 = math.min(num6, m_Buffer[num9++] + GetLaneSwitchCost(num7 - m));
                        }
                        for (int n = num7; n <= num8; n++)
                        {
                            num6 = math.min(num6, m_Buffer[num9++]);
                        }
                        for (int num10 = num8 + 1; num10 <= num2; num10++)
                        {
                            num6 = math.min(num6, m_Buffer[num9++] + GetLaneSwitchCost(num10 - num8));
                        }
                        num6 += CalculateLaneObjectCost(laneObjectCost, index, subLane.m_SubLane, navLaneData.m_Flags);
                        bool test = num4 == 100000;
                        falseValue = math.select(falseValue, num6, test);
                        num4 = math.select(num4, i, test);
                        trueValue = num6;
                        num5 = i;
                        if (m_LaneReservationData.TryGetComponent(subLane.m_SubLane, out var componentData3))
                        {
                            num6 += GetLanePriorityCost(componentData3.GetPriority());
                        }
                        if (m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData4))
                        {
                            if (!m_CarLaneRulesData.TryGetComponent(subLane.m_SubLane, out var carLaneRules))
                            {
                                carLaneRules = new LaneRules();
                            }
                            num6 += GetLaneDriveCost(componentData4.m_Flags, subLane.m_PathMethods, i, componentData.m_MinIndex, num, carLaneRules);
                        }
                    }
                    m_Buffer[m_BufferPos++] = num6;
                }
                if (num4 <= num5)
                {
                    for (int num11 = componentData.m_MinIndex; num11 <= num; num11++)
                    {
                        if (num11 < num4 || num11 > num5)
                        {
                            SubLane subLane2 = dynamicBuffer[num11];
                            float num12 = math.select(falseValue, trueValue, num11 > num5);
                            num12 += GetLaneSwitchCost(math.max(num4 - num11, num11 - num5));
                            if (m_LaneReservationData.TryGetComponent(subLane2.m_SubLane, out var componentData5))
                            {
                                num12 += GetLanePriorityCost(componentData5.GetPriority());
                            }
                            if (m_CarLaneData.TryGetComponent(subLane2.m_SubLane, out var componentData6))
                            {
                                if (!m_CarLaneRulesData.TryGetComponent(subLane2.m_SubLane, out var carLaneRules))
                                {
                                    carLaneRules = new LaneRules();
                                }
                                num12 += GetLaneDriveCost(componentData6.m_Flags, subLane2.m_PathMethods, num11, componentData.m_MinIndex, num, carLaneRules);
                            }
                            m_Buffer[m_BufferPos - num + num11 - 1] = num12;
                        }
                    }
                }
            }
            else if ((nextNavLaneData.m_Flags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
            {
                for (int num13 = componentData.m_MinIndex; num13 <= num; num13++)
                {
                    SubLane subLane3 = dynamicBuffer[num13];
                    float num14 = CalculateLaneObjectCost(laneObjectCost, index, subLane3.m_SubLane, navLaneData.m_Flags);
                    if (m_LaneReservationData.TryGetComponent(subLane3.m_SubLane, out var componentData7))
                    {
                        num14 += GetLanePriorityCost(componentData7.GetPriority());
                    }
                    if (m_CarLaneData.TryGetComponent(subLane3.m_SubLane, out var componentData8))
                    {
                        if (!m_CarLaneRulesData.TryGetComponent(subLane3.m_SubLane, out var carLaneRules))
                        {
                            carLaneRules = new LaneRules();
                        }
                        num14 += GetLaneDriveCost(componentData8.m_Flags, subLane3.m_PathMethods, num13, componentData.m_MinIndex, num, carLaneRules);
                    }
                    m_Buffer[m_BufferPos++] = num14;
                }
            }
            else
            {
                int num15 = 100000;
                int num16 = -100000;
                if ((nextNavLaneData.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) != 0)
                {
                    for (int num17 = componentData.m_MinIndex; num17 <= num; num17++)
                    {
                        if (dynamicBuffer[num17].m_SubLane == nextNavLaneData.m_Lane)
                        {
                            num15 = num17;
                            num16 = num17;
                            break;
                        }
                    }
                }
                else
                {
                    Lane lane5 = m_LaneData[nextNavLaneData.m_Lane];
                    for (int num18 = componentData.m_MinIndex; num18 <= num; num18++)
                    {
                        Lane lane6 = m_LaneData[dynamicBuffer[num18].m_SubLane];
                        if ((componentData.m_Flags & SlaveLaneFlags.MiddleEnd) != 0)
                        {
                            if (lane6.m_EndNode.EqualsIgnoreCurvePos(lane5.m_MiddleNode))
                            {
                                num15 = math.min(num15, num18);
                                num16 = num18;
                            }
                        }
                        else if (lane6.m_EndNode.Equals(lane5.m_StartNode))
                        {
                            num15 = math.min(num15, num18);
                            num16 = num18;
                        }
                    }
                }
                for (int num19 = componentData.m_MinIndex; num19 <= num; num19++)
                {
                    SubLane subLane4 = dynamicBuffer[num19];
                    float num20 = 0f;
                    if (num15 <= num16)
                    {
                        num20 += GetLaneSwitchCost(math.max(0, math.max(num15 - num19, num19 - num16)));
                    }
                    num20 += CalculateLaneObjectCost(laneObjectCost, index, subLane4.m_SubLane, navLaneData.m_Flags);
                    if (m_LaneReservationData.TryGetComponent(subLane4.m_SubLane, out var componentData9))
                    {
                        num20 += GetLanePriorityCost(componentData9.GetPriority());
                    }
                    if (m_CarLaneData.TryGetComponent(subLane4.m_SubLane, out var componentData10))
                    {
                        if (!m_CarLaneRulesData.TryGetComponent(subLane4.m_SubLane, out var carLaneRules))
                        {
                            carLaneRules = new LaneRules();
                        }
                        num20 += GetLaneDriveCost(componentData10.m_Flags, subLane4.m_PathMethods, num19, componentData.m_MinIndex, num, carLaneRules);
                    }
                    m_Buffer[m_BufferPos++] = num20;
                }
            }
        }
        m_LaneSwitchBaseCost += 0.01f;
    }

    private float GetLaneSwitchCost(int numLanes)
    {
        return (float)(numLanes * numLanes * numLanes) * m_LaneSwitchCost;
    }

    private float GetLanePriorityCost(int lanePriority)
    {
        return (float)math.max(0, lanePriority - m_Priority) * 1f;
    }

    private float GetLaneDriveCost(Game.Net.CarLaneFlags flags, PathMethod pathMethods, int index, int minIndex, int maxIndex, LaneRules carLaneRules)
    {
        float falseValue = math.select(0.4f, 0f, ((flags & m_PreferLaneFlags) != 0) || LaneRulesUtils.IsPrefer(carLaneRules, m_CarFlags, m_SizeClass, m_EnergyTypes));
        float trueValue = math.select(0.9f, 4.9f, m_Priority < 108);
        float num = math.select(falseValue, trueValue, ((flags & m_ForbidLaneFlags) != 0));
        num = math.select(num, num + 10f, LaneRulesUtils.IsForbidden(carLaneRules, m_CarFlags, m_SizeClass, m_EnergyTypes));
        int num2 = math.select(index - minIndex, maxIndex - index, (flags & Game.Net.CarLaneFlags.Invert) != 0 == m_LeftHandTraffic);
        return math.select(
            num + math.select(0f, 1.4f + (float)num2 * 0.4f, (m_PathMethods == PathMethod.Bicycle) & (pathMethods != PathMethod.Bicycle)),
            float.MaxValue,
            (pathMethods & m_PathMethods) == 0
        );
    }

    private float GetBicycleDriveCost(Game.Net.CarLaneFlags flags, PathMethod pathMethods, int index, int minIndex, int maxIndex)
    {
        int num = math.select(index - minIndex, maxIndex - index, (flags & Game.Net.CarLaneFlags.Invert) != 0 == m_LeftHandTraffic);
        return math.select(0f, 1.4f + (float)num * 0.4f, pathMethods != PathMethod.Bicycle);
    }

    public void UpdateOptimalLane(ref CarCurrentLane currentLane, CarNavigationLane nextNavLaneData)
    {
        Entity entity = ((currentLane.m_ChangeLane != Entity.Null) ? currentLane.m_ChangeLane : currentLane.m_Lane);
        int2 int5 = 0;
        int changeIndex = 0;
        if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.FixedLane) == 0 && m_SlaveLaneData.TryGetComponent(entity, out var componentData))
        {
            Owner owner = m_OwnerData[entity];
            DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
            int num = math.min(componentData.m_MaxIndex, dynamicBuffer.Length - 1);
            m_LaneSwitchCost = m_LaneSwitchBaseCost + math.select(1f, 5f, (componentData.m_Flags & SlaveLaneFlags.AllowChange) == 0);
            if (m_TurnAhead)
            {
                m_LaneSwitchCost *= 7f;
            }
            float laneObjectCost = 0.49f;
            for (int i = componentData.m_MinIndex; i <= num; i++)
            {
                if (dynamicBuffer[i].m_SubLane == entity)
                {
                    int5 = i;
                    break;
                }
            }
            if (currentLane.m_ChangeLane != Entity.Null)
            {
                for (int j = componentData.m_MinIndex; j <= num; j++)
                {
                    if (dynamicBuffer[j].m_SubLane == currentLane.m_Lane)
                    {
                        int5.y = j;
                        break;
                    }
                }
            }
            float num2 = float.MaxValue;
            if (
                (nextNavLaneData.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.FixedLane)) == 0
                && m_SlaveLaneData.TryGetComponent(nextNavLaneData.m_Lane, out var componentData2)
            )
            {
                Owner owner2 = m_OwnerData[nextNavLaneData.m_Lane];
                DynamicBuffer<SubLane> dynamicBuffer2 = m_Lanes[owner2.m_Owner];
                int num3 = math.min(componentData2.m_MaxIndex, dynamicBuffer2.Length - 1);
                int num4 = m_BufferPos - (num3 - componentData2.m_MinIndex + 1);
                for (int k = componentData.m_MinIndex; k <= num; k++)
                {
                    SubLane subLane = dynamicBuffer[k];
                    Lane lane = m_LaneData[subLane.m_SubLane];
                    float num5 = 1000000f;
                    int num6;
                    int num7;
                    if ((nextNavLaneData.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) != 0)
                    {
                        num6 = componentData2.m_MinIndex;
                        num7 = num3;
                    }
                    else
                    {
                        num6 = 100000;
                        num7 = -100000;
                        if ((componentData.m_Flags & SlaveLaneFlags.MiddleEnd) != 0)
                        {
                            for (int l = componentData2.m_MinIndex; l <= num3; l++)
                            {
                                Lane lane2 = m_LaneData[dynamicBuffer2[l].m_SubLane];
                                if (lane.m_EndNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode))
                                {
                                    num6 = math.min(num6, l);
                                    num7 = l;
                                }
                            }
                        }
                        else if ((componentData2.m_Flags & SlaveLaneFlags.MiddleStart) != 0)
                        {
                            for (int m = componentData2.m_MinIndex; m <= num3; m++)
                            {
                                Lane lane3 = m_LaneData[dynamicBuffer2[m].m_SubLane];
                                if (lane.m_MiddleNode.EqualsIgnoreCurvePos(lane3.m_StartNode))
                                {
                                    num6 = math.min(num6, m);
                                    num7 = m;
                                }
                            }
                        }
                        else
                        {
                            for (int n = componentData2.m_MinIndex; n <= num3; n++)
                            {
                                Lane lane4 = m_LaneData[dynamicBuffer2[n].m_SubLane];
                                if (lane.m_EndNode.Equals(lane4.m_StartNode))
                                {
                                    num6 = math.min(num6, n);
                                    num7 = n;
                                }
                            }
                        }
                    }
                    if (num6 <= num7)
                    {
                        int num8 = num4 + (num6 - componentData2.m_MinIndex);
                        for (int num9 = num6; num9 <= num7; num9++)
                        {
                            num5 = math.min(num5, m_Buffer[num8++]);
                        }
                        num5 += CalculateLaneObjectCost(laneObjectCost, subLane.m_SubLane, currentLane.m_CurvePosition.x, currentLane.m_LaneFlags);
                        if (m_LaneReservationData.TryGetComponent(subLane.m_SubLane, out var componentData3))
                        {
                            num5 += GetLanePriorityCost(componentData3.GetPriority());
                        }
                    }
                    int2 int6 = math.abs(k - int5);
                    num5 += GetLaneSwitchCost(math.select(int6.x, int6.y, int6.x != 0 && int6.y > int6.x));
                    if (m_PathMethods == PathMethod.Bicycle && m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData4))
                    {
                        num5 += GetBicycleDriveCost(componentData4.m_Flags, subLane.m_PathMethods, k, componentData.m_MinIndex, num);
                    }
                    num5 = math.select(num5, float.MaxValue, (subLane.m_PathMethods & m_PathMethods) == 0);
                    if (num5 < num2)
                    {
                        num2 = num5;
                        entity = subLane.m_SubLane;
                        changeIndex = k;
                    }
                }
            }
            else if ((nextNavLaneData.m_Flags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0 || nextNavLaneData.m_Lane == Entity.Null)
            {
                for (int num10 = componentData.m_MinIndex; num10 <= num; num10++)
                {
                    SubLane subLane2 = dynamicBuffer[num10];
                    float num11 = CalculateLaneObjectCost(laneObjectCost, subLane2.m_SubLane, currentLane.m_CurvePosition.x, currentLane.m_LaneFlags);
                    if (m_LaneReservationData.TryGetComponent(subLane2.m_SubLane, out var componentData5))
                    {
                        num11 += GetLanePriorityCost(componentData5.GetPriority());
                    }
                    int2 int7 = math.abs(num10 - int5);
                    num11 += GetLaneSwitchCost(math.select(int7.x, int7.y, int7.x != 0 && int7.y > int7.x));
                    if (m_PathMethods == PathMethod.Bicycle && m_CarLaneData.TryGetComponent(subLane2.m_SubLane, out var componentData6))
                    {
                        num11 += GetBicycleDriveCost(componentData6.m_Flags, subLane2.m_PathMethods, num10, componentData.m_MinIndex, num);
                    }
                    num11 = math.select(num11, float.MaxValue, (subLane2.m_PathMethods & m_PathMethods) == 0);
                    if (num11 < num2)
                    {
                        num2 = num11;
                        entity = subLane2.m_SubLane;
                        changeIndex = num10;
                    }
                }
            }
            else
            {
                int num12 = 100000;
                int num13 = -100000;
                if ((nextNavLaneData.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) != 0)
                {
                    for (int num14 = componentData.m_MinIndex; num14 <= num; num14++)
                    {
                        if (dynamicBuffer[num14].m_SubLane == nextNavLaneData.m_Lane)
                        {
                            num12 = num14;
                            num13 = num14;
                            break;
                        }
                    }
                }
                else
                {
                    Lane lane5 = m_LaneData[nextNavLaneData.m_Lane];
                    for (int num15 = componentData.m_MinIndex; num15 <= num; num15++)
                    {
                        Lane lane6 = m_LaneData[dynamicBuffer[num15].m_SubLane];
                        if ((componentData.m_Flags & SlaveLaneFlags.MiddleEnd) != 0)
                        {
                            if (lane6.m_EndNode.EqualsIgnoreCurvePos(lane5.m_MiddleNode))
                            {
                                num12 = math.min(num12, num15);
                                num13 = num15;
                            }
                        }
                        else if (lane6.m_EndNode.Equals(lane5.m_StartNode))
                        {
                            num12 = math.min(num12, num15);
                            num13 = num15;
                        }
                    }
                }
                for (int num16 = componentData.m_MinIndex; num16 <= num; num16++)
                {
                    SubLane subLane3 = dynamicBuffer[num16];
                    float num17;
                    if ((num16 >= num12 && num16 <= num13) || num12 > num13)
                    {
                        num17 = CalculateLaneObjectCost(laneObjectCost, subLane3.m_SubLane, currentLane.m_CurvePosition.x, currentLane.m_LaneFlags);
                        if (m_LaneReservationData.TryGetComponent(subLane3.m_SubLane, out var componentData7))
                        {
                            num17 += GetLanePriorityCost(componentData7.GetPriority());
                        }
                    }
                    else
                    {
                        num17 = 1000000f;
                    }
                    int2 int8 = math.abs(num16 - int5);
                    num17 += GetLaneSwitchCost(math.select(int8.x, int8.y, int8.x != 0 && int8.y > int8.x));
                    if (m_PathMethods == PathMethod.Bicycle && m_CarLaneData.TryGetComponent(subLane3.m_SubLane, out var componentData8))
                    {
                        num17 += GetBicycleDriveCost(componentData8.m_Flags, subLane3.m_PathMethods, num16, componentData.m_MinIndex, num);
                    }
                    num17 = math.select(num17, float.MaxValue, (subLane3.m_PathMethods & m_PathMethods) == 0);
                    if (num17 < num2)
                    {
                        num2 = num17;
                        entity = subLane3.m_SubLane;
                        changeIndex = num16;
                    }
                }
            }
        }
        if (entity != currentLane.m_Lane)
        {
            if (entity != currentLane.m_ChangeLane)
            {
                currentLane.m_ChangeLane = entity;
                currentLane.m_ChangeProgress = 0f;
                currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
                currentLane.m_LaneFlags |= GetTurnFlags(currentLane.m_Lane, int5.y, changeIndex);
            }
        }
        else if (currentLane.m_ChangeLane != Entity.Null)
        {
            currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
            if (currentLane.m_ChangeProgress == 0f)
            {
                currentLane.m_ChangeLane = Entity.Null;
            }
            else
            {
                currentLane.m_Lane = currentLane.m_ChangeLane;
                currentLane.m_ChangeLane = entity;
                currentLane.m_ChangeProgress = math.saturate(1f - currentLane.m_ChangeProgress);
                currentLane.m_LaneFlags |= GetTurnFlags(currentLane.m_Lane, int5.y, changeIndex);
            }
        }
        if (currentLane.m_ChangeLane == Entity.Null)
        {
            m_PrevLane = currentLane.m_Lane;
        }
        else
        {
            m_PrevLane = currentLane.m_ChangeLane;
        }
        m_LaneSwitchCost = 10000000f;
    }

    private Game.Vehicles.CarLaneFlags GetTurnFlags(Entity currentLane, int currentIndex, int changeIndex)
    {
        if (changeIndex != currentIndex)
        {
            bool flag = false;
            if (m_CarLaneData.TryGetComponent(currentLane, out var componentData))
            {
                flag = (componentData.m_Flags & Game.Net.CarLaneFlags.Invert) != 0;
            }
            if (changeIndex < currentIndex != flag)
            {
                return Game.Vehicles.CarLaneFlags.TurnLeft;
            }
            return Game.Vehicles.CarLaneFlags.TurnRight;
        }
        return (Game.Vehicles.CarLaneFlags)0u;
    }

    public void UpdateOptimalLane(ref CarNavigationLane navLaneData)
    {
        if (m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
        {
            SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
            if (
                (navLaneData.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.FixedLane | Game.Vehicles.CarLaneFlags.FixedStart)) == 0
                && m_LaneData.HasComponent(m_PrevLane)
            )
            {
                Owner owner = m_OwnerData[navLaneData.m_Lane];
                DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
                int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
                m_BufferPos -= num - slaveLane.m_MinIndex + 1;
                int num2 = 100000;
                int num3 = -100000;
                if ((navLaneData.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) == 0)
                {
                    Lane lane = m_LaneData[m_PrevLane];
                    SlaveLane slaveLane2 = default(SlaveLane);
                    if (m_SlaveLaneData.HasComponent(m_PrevLane))
                    {
                        slaveLane2 = m_SlaveLaneData[m_PrevLane];
                    }
                    if ((slaveLane2.m_Flags & SlaveLaneFlags.MiddleEnd) != 0)
                    {
                        for (int i = slaveLane.m_MinIndex; i <= num; i++)
                        {
                            Lane lane2 = m_LaneData[dynamicBuffer[i].m_SubLane];
                            if (lane.m_EndNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode))
                            {
                                num2 = math.min(num2, i);
                                num3 = i;
                            }
                        }
                    }
                    else if ((slaveLane.m_Flags & SlaveLaneFlags.MiddleStart) != 0)
                    {
                        for (int j = slaveLane.m_MinIndex; j <= num; j++)
                        {
                            Lane lane3 = m_LaneData[dynamicBuffer[j].m_SubLane];
                            if (lane.m_MiddleNode.EqualsIgnoreCurvePos(lane3.m_StartNode))
                            {
                                num2 = math.min(num2, j);
                                num3 = j;
                            }
                        }
                    }
                    else
                    {
                        for (int k = slaveLane.m_MinIndex; k <= num; k++)
                        {
                            Lane lane4 = m_LaneData[dynamicBuffer[k].m_SubLane];
                            if (lane.m_EndNode.Equals(lane4.m_StartNode))
                            {
                                num2 = math.min(num2, k);
                                num3 = k;
                            }
                        }
                    }
                }
                if (num2 > num3)
                {
                    num2 = slaveLane.m_MinIndex;
                    num3 = num;
                }
                int bufferPos = m_BufferPos;
                float num4 = float.MaxValue;
                int index = slaveLane.m_MinIndex;
                for (int l = slaveLane.m_MinIndex; l < num2; l++)
                {
                    float num5 = m_Buffer[bufferPos++] + GetLaneSwitchCost(num2 - l);
                    if (num5 < num4)
                    {
                        num4 = num5;
                        index = l;
                    }
                }
                for (int m = num2; m <= num3; m++)
                {
                    float num6 = m_Buffer[bufferPos++];
                    if (num6 < num4)
                    {
                        num4 = num6;
                        index = m;
                    }
                }
                for (int n = num3 + 1; n <= num; n++)
                {
                    float num7 = m_Buffer[bufferPos++] + GetLaneSwitchCost(n - num3);
                    if (num7 < num4)
                    {
                        num4 = num7;
                        index = n;
                    }
                }
                navLaneData.m_Lane = dynamicBuffer[index].m_SubLane;
            }
            m_LaneSwitchCost = m_LaneSwitchBaseCost + math.select(1f, 5f, (slaveLane.m_Flags & SlaveLaneFlags.AllowChange) == 0);
            if (m_TurnAhead)
            {
                m_LaneSwitchCost *= 7f;
            }
        }
        else
        {
            m_LaneSwitchCost = 10000000f;
        }
        m_PrevLane = navLaneData.m_Lane;
        m_LaneSwitchBaseCost -= 0.01f;
    }

    public void DrawLaneCosts(CarCurrentLane currentLaneData, CarNavigationLane nextNavLaneData, ComponentLookup<Curve> curveData, GizmoBatcher gizmoBatcher)
    {
        if (currentLaneData.m_ChangeProgress == 0f || currentLaneData.m_ChangeLane == Entity.Null)
        {
            m_PrevLane = currentLaneData.m_Lane;
        }
        else
        {
            m_PrevLane = currentLaneData.m_ChangeLane;
        }
    }

    public void DrawLaneCosts(CarNavigationLane navLaneData, ComponentLookup<Curve> curveData, GizmoBatcher gizmoBatcher)
    {
        if (m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
        {
            SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
            Owner owner = m_OwnerData[navLaneData.m_Lane];
            DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
            int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
            if ((navLaneData.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.FixedLane)) == 0)
            {
                m_BufferPos -= num - slaveLane.m_MinIndex + 1;
                int bufferPos = m_BufferPos;
                for (int i = slaveLane.m_MinIndex; i <= num; i++)
                {
                    float cost = m_Buffer[bufferPos++];
                    DrawLane(dynamicBuffer[i].m_SubLane, navLaneData.m_CurvePosition, curveData, gizmoBatcher, cost);
                }
            }
            else
            {
                for (int j = slaveLane.m_MinIndex; j <= num; j++)
                {
                    Entity subLane = dynamicBuffer[j].m_SubLane;
                    float cost2 = math.select(1000000f, 0f, subLane == navLaneData.m_Lane);
                    DrawLane(subLane, navLaneData.m_CurvePosition, curveData, gizmoBatcher, cost2);
                }
            }
        }
        m_PrevLane = navLaneData.m_Lane;
    }

    private void DrawLane(Entity lane, float2 curvePos, ComponentLookup<Curve> curveData, GizmoBatcher gizmoBatcher, float cost)
    {
        Curve curve = curveData[lane];
        UnityEngine.Color color;
        if (cost >= 100000f)
        {
            color = UnityEngine.Color.black;
        }
        else
        {
            cost = math.sqrt(cost);
            color = (
                (!(cost < 2f))
                    ? UnityEngine.Color.Lerp(UnityEngine.Color.yellow, UnityEngine.Color.red, (cost - 2f) * 0.5f)
                    : UnityEngine.Color.Lerp(UnityEngine.Color.cyan, UnityEngine.Color.yellow, cost * 0.5f)
            );
        }
        Bezier4x3 bezier = MathUtils.Cut(curve.m_Bezier, curvePos);
        float length = curve.m_Length * math.abs(curvePos.y - curvePos.x);
        gizmoBatcher.DrawCurve(bezier, length, color);
    }
}
