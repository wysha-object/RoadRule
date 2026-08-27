using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using RoadRule.Components;
using RoadRule.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace RoadRule.Systems.Pathfind
{
    public static class PatchedPathfindJobs
    {
        private struct FullNode : IEquatable<FullNode>
        {
            public NodeID m_NodeID;

            public float m_CurvePos;

            public FullNode(NodeID nodeID, float curvePos)
            {
                m_NodeID = nodeID;
                m_CurvePos = curvePos;
            }

            public FullNode(EdgeID edgeID, float curvePos)
            {
                m_NodeID = new NodeID { m_Index = -4 - (edgeID.m_Index << 2) };
                m_CurvePos = curvePos;
            }

            public bool Equals(FullNode other)
            {
                if (m_NodeID.Equals(other.m_NodeID))
                {
                    return m_CurvePos == other.m_CurvePos;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return (m_NodeID.m_Index >> 2) ^ math.asint(m_CurvePos);
            }
        }

        [Flags]
        private enum PathfindItemFlags : ushort
        {
            End = 1,
            SingleOnly = 2,
            NextEdge = 4,
            ReducedCost = 8,
            ForbidExit = 0x10,
            ReducedAccess = 0x20,
            Forward = 0x40,
            Backward = 0x80,
        }

        private struct NodeData(
            int sourceIndex,
            float totalCost,
            float baseCost,
            int accessRequirement,
            EdgeID edgeID,
            EdgeID nextID,
            float2 edgeDelta,
            FullNode pathNode,
            PathfindItemFlags flags,
            PathMethod method
        )
        {
            public FullNode m_PathNode = pathNode;

            public int m_NextNodeIndex = -1;

            public int m_SourceIndex = sourceIndex;

            public float m_TotalCost = totalCost;

            public float m_BaseCost = baseCost;

            public int m_AccessRequirement = accessRequirement;

            public EdgeID m_EdgeID = edgeID;

            public EdgeID m_NextID = nextID;

            public float2 m_EdgeDelta = edgeDelta;

            public PathfindItemFlags m_Flags = flags;

            public PathMethod m_Method = method;
        }

        private struct HeapData(float totalCost, int nodeIndex) : ILessThan<HeapData>, IComparable<HeapData>
        {
            public float m_TotalCost = totalCost;

            public int m_NodeIndex = nodeIndex;

            public bool LessThan(HeapData other)
            {
                return m_TotalCost < other.m_TotalCost;
            }

            public int CompareTo(HeapData other)
            {
                return m_NodeIndex - other.m_NodeIndex;
            }
        }

        private struct TargetData(Entity entity, float cost)
        {
            public Entity m_Entity = entity;

            public float m_Cost = cost;
        }

        public struct PatchedPathfindExecutor
        {
            private UnsafePathfindData m_PathfindData;

            private Allocator m_Allocator;

            private Unity.Mathematics.Random m_Random;

            private PathfindParameters m_Parameters;

            private Bounds3 m_StartBounds;

            private Bounds3 m_EndBounds;

            private int3 m_AccessMask;

            private int2 m_AuthorizationMask;

            private Entity m_ParkingOwner;

            private float m_HeuristicCostFactor;

            private float m_MaxTotalCost;

            private float m_CostOffset;

            private float m_ReducedCostFactor;

            private float2 m_ParkingSize;

            private int m_MaxResultCount;

            private bool m_InvertPath;

            private bool m_ParkingReset;

            private EdgeFlags m_Forward;

            private EdgeFlags m_Backward;

            private EdgeFlags m_ForwardMiddle;

            private EdgeFlags m_BackwardMiddle;

            private EdgeFlags m_FreeForward;

            private EdgeFlags m_FreeBackward;

            private EdgeFlags m_ParkingEdgeMask;

            private PathMethod m_ParkingMethodMask;

            private const int NODE_INDEX_SHIFT = 4;

            private UnsafeHashMap<FullNode, TargetData> m_StartTargets;

            private UnsafeHashMap<FullNode, TargetData> m_EndTargets;

            private UnsafeList<int> m_NodeIndex;

            private UnsafeList<int> m_NodeIndexBits;

            private UnsafeMinHeap<HeapData> m_Heap;

            private UnsafeList<NodeData> m_NodeData;

            [ReadOnly]
            public ComponentLookup<LaneRules> m_LaneRulesLookup;

            [ReadOnly]
            public ComponentLookup<Car> m_CarLookup;

            [ReadOnly]
            public ComponentLookup<CarData> m_CarDataLookup;

            public Entity m_Owner;

            public void Initialize(
                NativePathfindData pathfindData,
                Allocator allocator,
                Unity.Mathematics.Random random,
                PathfindParameters parameters,
                PathfindHeuristicData pathfindHeuristicData,
                float maxPassengerTransportSpeed,
                float maxCargoTransportSpeed,
                ComponentLookup<LaneRules> laneRulesData,
                ComponentLookup<Car> carLookup,
                ComponentLookup<CarData> carDataLookup,
                Entity owner
            )
            {
                m_PathfindData = pathfindData.GetReadOnlyData();
                m_Allocator = allocator;
                m_Random = random;
                m_Parameters = parameters;
                m_ReducedCostFactor = 1f;
                if ((parameters.m_PathfindFlags & PathfindFlags.ParkingReset) != 0)
                {
                    m_ReducedCostFactor = 0.5f;
                    m_ParkingReset = true;
                }
                if ((parameters.m_PathfindFlags & PathfindFlags.NoHeuristics) != 0)
                {
                    m_HeuristicCostFactor = 0f;
                }
                else
                {
                    m_HeuristicCostFactor = 1000000f;
                    if ((parameters.m_Methods & PathMethod.Pedestrian) != 0)
                    {
                        PathfindCosts pedestrianCosts = pathfindHeuristicData.m_PedestrianCosts;
                        pedestrianCosts.m_Value.x += 1f / math.max(0.01f, parameters.m_WalkSpeed.x);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(pedestrianCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & (PathMethod.Road | PathMethod.MediumRoad)) != 0)
                    {
                        PathfindCosts carCosts = pathfindHeuristicData.m_CarCosts;
                        carCosts.m_Value.x += 1f / math.max(0.01f, parameters.m_MaxSpeed.x);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(carCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & PathMethod.Track) != 0)
                    {
                        PathfindCosts trackCosts = pathfindHeuristicData.m_TrackCosts;
                        trackCosts.m_Value.x += 1f / math.max(0.01f, parameters.m_MaxSpeed.x);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(trackCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & PathMethod.Flying) != 0)
                    {
                        PathfindCosts flyingCosts = pathfindHeuristicData.m_FlyingCosts;
                        flyingCosts.m_Value.x += 1f / math.max(0.01f, parameters.m_MaxSpeed.x);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(flyingCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & PathMethod.Offroad) != 0)
                    {
                        PathfindCosts offRoadCosts = pathfindHeuristicData.m_OffRoadCosts;
                        offRoadCosts.m_Value.x += 1f / math.max(0.01f, parameters.m_MaxSpeed.x);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(offRoadCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & PathMethod.Taxi) != 0)
                    {
                        PathfindCosts taxiCosts = pathfindHeuristicData.m_TaxiCosts;
                        taxiCosts.m_Value.x += 1f / math.max(0.01f, 111.111115f);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(taxiCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & (PathMethod.PublicTransportDay | PathMethod.PublicTransportNight)) != 0)
                    {
                        PathfindCosts pathfindCosts = default(PathfindCosts);
                        pathfindCosts.m_Value.x += 1f / math.max(0.01f, maxPassengerTransportSpeed);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(pathfindCosts.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_Methods & PathMethod.CargoTransport) != 0)
                    {
                        PathfindCosts pathfindCosts2 = default(PathfindCosts);
                        pathfindCosts2.m_Value.x += 1f / math.max(0.01f, maxCargoTransportSpeed);
                        m_HeuristicCostFactor = math.min(m_HeuristicCostFactor, math.dot(pathfindCosts2.m_Value, parameters.m_Weights.m_Value));
                    }
                    if ((parameters.m_PathfindFlags & PathfindFlags.Stable) == 0)
                    {
                        m_HeuristicCostFactor *= 2f;
                    }
                    m_HeuristicCostFactor *= m_ReducedCostFactor;
                }
                if (parameters.m_ParkingTarget != Entity.Null && parameters.m_ParkingDelta >= 0f)
                {
                    m_ParkingOwner = parameters.m_ParkingTarget;
                }
                m_ParkingSize = (((m_Parameters.m_Methods & PathMethod.Boarding) == 0) ? m_Parameters.m_ParkingSize : ((float2)float.MinValue));
                m_ParkingEdgeMask = ((m_Parameters.m_ParkingTarget == Entity.Null) ? EdgeFlags.OutsideConnection : (~(EdgeFlags.DefaultMask | EdgeFlags.Secondary)));
                int num = (math.max(m_PathfindData.GetNodeIDSize(), m_PathfindData.m_Edges.Length << 2) >> 4) + 1;
                int num2 = (num >> 5) + 1;
                m_NodeIndex = new UnsafeList<int>(num, allocator);
                m_NodeIndexBits = new UnsafeList<int>(num2, allocator);
                m_Heap = new UnsafeMinHeap<HeapData>(1000, allocator);
                m_NodeData = new UnsafeList<NodeData>(10000, allocator);
                m_NodeIndex.Resize(num);
                m_NodeIndexBits.Resize(num2, NativeArrayOptions.ClearMemory);
                m_LaneRulesLookup = laneRulesData;
                m_CarLookup = carLookup;
                m_CarDataLookup = carDataLookup;
                m_Owner = owner;
            }

            public void Release()
            {
                if (m_NodeData.IsCreated)
                {
                    m_NodeIndex.Dispose();
                    m_NodeIndexBits.Dispose();
                    m_Heap.Dispose();
                    m_NodeData.Dispose();
                }
                if (m_EndTargets.IsCreated)
                {
                    m_EndTargets.Dispose();
                }
                if (m_StartTargets.IsCreated)
                {
                    m_StartTargets.Dispose();
                }
            }

            public void AddTargets(UnsafeList<PathTarget> startTargets, UnsafeList<PathTarget> endTargets, ref ErrorCode errorCode)
            {
                m_StartBounds = GetTargetBounds(startTargets, out var minCost, out var accessRequirement, out var multipleRequirements);
                m_EndBounds = GetTargetBounds(endTargets, out var minCost2, out var accessRequirement2, out var multipleRequirements2);
                if (multipleRequirements & multipleRequirements2)
                {
                    multipleRequirements =
                        (m_Parameters.m_PathfindFlags & (PathfindFlags.IgnoreExtraStartAccessRequirements | PathfindFlags.IgnoreExtraEndAccessRequirements))
                        != PathfindFlags.IgnoreExtraStartAccessRequirements;
                    multipleRequirements2 = (m_Parameters.m_PathfindFlags & PathfindFlags.IgnoreExtraEndAccessRequirements) == 0;
                }
                if ((m_Parameters.m_PathfindFlags & PathfindFlags.ForceForward) != 0)
                {
                    m_InvertPath = false;
                }
                else if ((m_Parameters.m_PathfindFlags & PathfindFlags.ForceBackward) != 0)
                {
                    m_InvertPath = true;
                }
                else if ((m_Parameters.m_PathfindFlags & PathfindFlags.MultipleDestinations) != 0)
                {
                    m_InvertPath = false;
                }
                else if ((m_Parameters.m_PathfindFlags & PathfindFlags.MultipleOrigins) != 0)
                {
                    m_InvertPath = true;
                }
                else if (multipleRequirements)
                {
                    m_InvertPath = false;
                }
                else if (multipleRequirements2)
                {
                    m_InvertPath = true;
                }
                else
                {
                    m_InvertPath = math.lengthsq(MathUtils.Size(m_StartBounds)) < math.lengthsq(MathUtils.Size(m_EndBounds));
                }
                if (m_InvertPath)
                {
                    CommonUtils.Swap(ref startTargets, ref endTargets);
                    CommonUtils.Swap(ref m_StartBounds, ref m_EndBounds);
                    CommonUtils.Swap(ref minCost, ref minCost2);
                    CommonUtils.Swap(ref accessRequirement, ref accessRequirement2);
                    CommonUtils.Swap(ref multipleRequirements, ref multipleRequirements2);
                    m_PathfindData.SwapConnections();
                    m_Forward = EdgeFlags.Backward;
                    m_Backward = EdgeFlags.Forward;
                    m_FreeForward = EdgeFlags.FreeBackward;
                    m_FreeBackward = EdgeFlags.FreeForward;
                    m_ParkingMethodMask = PathMethod.Road | PathMethod.Track | PathMethod.Flying | PathMethod.Offroad | PathMethod.MediumRoad | PathMethod.Bicycle;
                }
                else
                {
                    m_Forward = EdgeFlags.Forward;
                    m_Backward = EdgeFlags.Backward;
                    m_FreeForward = EdgeFlags.FreeForward;
                    m_FreeBackward = EdgeFlags.FreeBackward;
                    m_ParkingMethodMask = PathMethod.Pedestrian | PathMethod.PublicTransportDay | PathMethod.Taxi | PathMethod.PublicTransportNight;
                }
                if (multipleRequirements2)
                {
                    errorCode = ErrorCode.TooManyEndAccessRequirements;
                }
                if (
                    ((m_Parameters.m_PathfindFlags & PathfindFlags.MultipleDestinations) != 0 && m_InvertPath)
                    || ((m_Parameters.m_PathfindFlags & PathfindFlags.MultipleOrigins) != 0 && !m_InvertPath)
                )
                {
                    errorCode = ErrorCode.MultipleStartResults;
                }
                m_AccessMask = new int3(-1, -1, accessRequirement2);
                if (
                    m_PathfindData.m_PathEdges.TryGetValue(m_Parameters.m_ParkingTarget, out var item)
                    || m_PathfindData.m_SecondaryEdges.TryGetValue(m_Parameters.m_ParkingTarget, out item)
                )
                {
                    ref Edge edge = ref m_PathfindData.GetEdge(item);
                    m_AccessMask.y = edge.m_Specification.m_AccessRequirement;
                }
                m_AuthorizationMask = math.select(
                    -2,
                    new int2(m_Parameters.m_Authorization1.Index, m_Parameters.m_Authorization2.Index),
                    new bool2(m_Parameters.m_Authorization1 != Entity.Null, m_Parameters.m_Authorization2 != Entity.Null)
                );
                m_ForwardMiddle = m_Forward | EdgeFlags.AllowMiddle;
                m_BackwardMiddle = m_Backward | EdgeFlags.AllowMiddle;
                AddEndTargets(endTargets, minCost2);
                AddStartTargets(startTargets, minCost);
                m_CostOffset = minCost + minCost2;
                m_MaxTotalCost = math.select(m_Parameters.m_MaxCost - m_CostOffset, float.MaxValue, m_Parameters.m_MaxCost == 0f);
                m_MaxResultCount = math.select(
                    1,
                    m_Parameters.m_MaxResultCount,
                    m_Parameters.m_MaxResultCount > 1 && (m_Parameters.m_PathfindFlags & (PathfindFlags.MultipleOrigins | PathfindFlags.MultipleDestinations)) != 0
                );
            }

            public Bounds3 GetTargetBounds(UnsafeList<PathTarget> pathTargets, out float minCost, out int accessRequirement, out bool multipleRequirements)
            {
                Bounds3 result = new Bounds3(float.MaxValue, float.MinValue);
                int length = pathTargets.Length;
                minCost = float.MaxValue;
                accessRequirement = -1;
                multipleRequirements = false;
                for (int i = 0; i < length; i++)
                {
                    PathTarget pathTarget = pathTargets[i];
                    EdgeID item;
                    if ((pathTarget.m_Flags & EdgeFlags.Secondary) != 0)
                    {
                        if (!m_PathfindData.m_SecondaryEdges.TryGetValue(pathTarget.m_Entity, out item))
                        {
                            continue;
                        }
                    }
                    else if (!m_PathfindData.m_PathEdges.TryGetValue(pathTarget.m_Entity, out item))
                    {
                        continue;
                    }
                    ref Edge edge = ref m_PathfindData.GetEdge(item);
                    result |= MathUtils.Position(edge.m_Location.m_Line, pathTarget.m_Delta);
                    minCost = math.min(minCost, pathTarget.m_Cost);
                    if (
                        (edge.m_Specification.m_AccessRequirement != -1)
                        & (edge.m_Specification.m_AccessRequirement != accessRequirement)
                        & ((edge.m_Specification.m_Flags & (EdgeFlags.AllowEnter | EdgeFlags.AllowExit)) != EdgeFlags.AllowEnter)
                    )
                    {
                        multipleRequirements = accessRequirement != -1;
                        accessRequirement = math.select(edge.m_Specification.m_AccessRequirement, accessRequirement, multipleRequirements);
                    }
                }
                return result;
            }

            private void AddEndTargets(UnsafeList<PathTarget> pathTargets, float minCost)
            {
                int length = pathTargets.Length;
                m_EndTargets = new UnsafeHashMap<FullNode, TargetData>(length, m_Allocator);
                for (int i = 0; i < length; i++)
                {
                    PathTarget pathTarget = pathTargets[i];
                    pathTarget.m_Cost -= minCost;
                    EdgeID item;
                    if ((pathTarget.m_Flags & EdgeFlags.Secondary) != 0)
                    {
                        if (!m_PathfindData.m_SecondaryEdges.TryGetValue(pathTarget.m_Entity, out item))
                        {
                            continue;
                        }
                    }
                    else if (!m_PathfindData.m_PathEdges.TryGetValue(pathTarget.m_Entity, out item))
                    {
                        continue;
                    }
                    FullNode fullNode = new FullNode(item, pathTarget.m_Delta);
                    TargetData targetData = new TargetData(pathTarget.m_Target, pathTarget.m_Cost);
                    if (!m_EndTargets.TryAdd(fullNode, targetData))
                    {
                        TargetData targetData2 = m_EndTargets[fullNode];
                        if (targetData.m_Cost < targetData2.m_Cost)
                        {
                            m_EndTargets[fullNode] = targetData;
                        }
                    }
                    if (!GetOrAddNodeIndex(fullNode, out var _))
                    {
                        PathfindItemFlags pathfindItemFlags = PathfindItemFlags.End;
                        if ((pathTarget.m_Flags & m_Forward) != 0)
                        {
                            pathfindItemFlags |= PathfindItemFlags.Forward;
                        }
                        if ((pathTarget.m_Flags & m_Backward) != 0)
                        {
                            pathfindItemFlags |= PathfindItemFlags.Backward;
                        }
                        m_NodeData.Add(
                            new NodeData(
                                -1,
                                float.MaxValue,
                                pathTarget.m_Cost,
                                -1,
                                item,
                                default(EdgeID),
                                pathTarget.m_Delta,
                                fullNode,
                                pathfindItemFlags,
                                ~(
                                    PathMethod.Pedestrian
                                    | PathMethod.Road
                                    | PathMethod.Parking
                                    | PathMethod.PublicTransportDay
                                    | PathMethod.Track
                                    | PathMethod.Taxi
                                    | PathMethod.CargoTransport
                                    | PathMethod.CargoLoading
                                    | PathMethod.Flying
                                    | PathMethod.PublicTransportNight
                                    | PathMethod.Boarding
                                    | PathMethod.Offroad
                                    | PathMethod.SpecialParking
                                    | PathMethod.MediumRoad
                                    | PathMethod.Bicycle
                                    | PathMethod.BicycleParking
                                )
                            )
                        );
                    }
                }
            }

            private void AddStartTargets(UnsafeList<PathTarget> pathTargets, float minCost)
            {
                int length = pathTargets.Length;
                m_StartTargets = new UnsafeHashMap<FullNode, TargetData>(length, m_Allocator);
                for (int i = 0; i < length; i++)
                {
                    PathTarget pathTarget = pathTargets[i];
                    pathTarget.m_Cost -= minCost;
                    EdgeID item;
                    if ((pathTarget.m_Flags & EdgeFlags.Secondary) != 0)
                    {
                        if (!m_PathfindData.m_SecondaryEdges.TryGetValue(pathTarget.m_Entity, out item))
                        {
                            continue;
                        }
                    }
                    else if (!m_PathfindData.m_PathEdges.TryGetValue(pathTarget.m_Entity, out item))
                    {
                        continue;
                    }
                    ref Edge edge = ref m_PathfindData.GetEdge(item);
                    FullNode key = new FullNode(item, pathTarget.m_Delta);
                    TargetData targetData = new TargetData(pathTarget.m_Target, pathTarget.m_Cost);
                    if (!m_StartTargets.TryAdd(key, targetData))
                    {
                        TargetData targetData2 = m_StartTargets[key];
                        if (targetData.m_Cost < targetData2.m_Cost)
                        {
                            m_StartTargets[key] = targetData;
                        }
                    }
                    EdgeFlags flags = edge.m_Specification.m_Flags;
                    RuleFlags rules = edge.m_Specification.m_Rules;
                    flags &= pathTarget.m_Flags;
                    rules = (RuleFlags)(
                        (uint)rules
                        & (uint)(byte)(~(int)(((edge.m_Specification.m_Methods & PathMethod.Taxi) != 0) ? m_Parameters.m_TaxiIgnoredRules : m_Parameters.m_IgnoredRules))
                    );
                    bool3 directions = new bool3(
                        (flags & m_Forward) != 0 || (pathTarget.m_Delta == 1f && edge.m_EndID.m_Index >= 0),
                        (flags & EdgeFlags.AllowMiddle) != 0,
                        (flags & m_Backward) != 0 || (pathTarget.m_Delta == 0f && edge.m_StartID.m_Index >= 0)
                    );
                    bool flag = (edge.m_Specification.m_Methods & (PathMethod.Parking | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0;
                    bool reducedCost = m_ParkingReset & flag;
                    bool reducedAccess =
                        edge.m_Specification.m_AccessRequirement != -1 && (edge.m_Specification.m_Flags & (EdgeFlags.AllowEnter | EdgeFlags.AllowExit)) == EdgeFlags.AllowEnter;
                    if (flag && m_Forward == EdgeFlags.Forward && m_Parameters.m_ParkingTarget == pathTarget.m_Entity)
                    {
                        directions.x &= !math.any(m_ParkingSize > new float2(edge.m_Specification.m_Density, edge.m_Specification.m_MaxSpeed));
                    }
                    AddConnections(int.MaxValue, item, in edge, flags, rules, pathTarget.m_Cost, pathTarget.m_Delta, directions, reducedCost, forbidExit: false, reducedAccess);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HeapExtract(out HeapData heapData)
            {
                if (m_Heap.Length != 0)
                {
                    heapData = m_Heap.Extract();
                    return true;
                }
                heapData = default(HeapData);
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void HeapInsert(HeapData heapData)
            {
                m_Heap.Insert(heapData);
            }

            public bool FindEndNode(out int endNode, out float travelCost, out int graphTraversal)
            {
                endNode = 0;
                travelCost = -1f;
                graphTraversal = m_NodeData.Length;
                if (m_MaxResultCount == 0)
                {
                    return false;
                }
                HeapData heapData;
                while (HeapExtract(out heapData))
                {
                    ref NodeData reference = ref m_NodeData.ElementAt(heapData.m_NodeIndex);
                    if (reference.m_SourceIndex >= 0)
                    {
                        continue;
                    }
                    reference.m_SourceIndex = -1 - reference.m_SourceIndex;
                    if (heapData.m_TotalCost > m_MaxTotalCost)
                    {
                        endNode = 0;
                        travelCost = float.MaxValue;
                        graphTraversal = m_NodeData.Length;
                        return false;
                    }
                    if ((reference.m_Flags & PathfindItemFlags.End) != 0)
                    {
                        endNode = heapData.m_NodeIndex;
                        travelCost = heapData.m_TotalCost + m_CostOffset;
                        graphTraversal = m_NodeData.Length;
                        m_MaxResultCount--;
                        return true;
                    }
                    if ((reference.m_Flags & PathfindItemFlags.NextEdge) != 0)
                    {
                        ref Edge edge = ref m_PathfindData.GetEdge(reference.m_NextID);
                        CheckNextEdge(heapData.m_NodeIndex, reference.m_NextID, reference.m_PathNode, reference.m_BaseCost, reference.m_Flags, in edge);
                        continue;
                    }
                    int connectionCount = m_PathfindData.GetConnectionCount(reference.m_PathNode.m_NodeID);
                    PathfindItemFlags flags = reference.m_Flags;
                    bool flag = (flags & PathfindItemFlags.ForbidExit) != 0;
                    bool test = (flags & PathfindItemFlags.ReducedAccess) != 0;
                    int4 int5 = new int4(m_AccessMask, math.select(reference.m_AccessRequirement, -1, test));
                    FullNode pathNode = reference.m_PathNode;
                    float baseCost = reference.m_BaseCost;
                    PathMethod method = reference.m_Method;
                    EdgeID edgeID = reference.m_EdgeID;
                    for (int i = 0; i < connectionCount; i++)
                    {
                        EdgeID edgeID2 = new EdgeID { m_Index = m_PathfindData.GetConnection(pathNode.m_NodeID, i) };
                        int accessRequirement = m_PathfindData.GetAccessRequirement(pathNode.m_NodeID, i);
                        if (edgeID.Equals(edgeID2) || math.all(int5 != accessRequirement))
                        {
                            continue;
                        }
                        ref Edge edge2 = ref m_PathfindData.GetEdge(edgeID2);
                        EdgeFlags edgeFlags = edge2.m_Specification.m_Flags;
                        if (DisallowConnection(method, flags, in edge2.m_Specification, ref edgeFlags, edge2.m_Owner))
                        {
                            continue;
                        }
                        bool flag2 = edge2.m_Specification.m_AccessRequirement != reference.m_AccessRequirement;
                        if (!(flag & flag2))
                        {
                            PathfindItemFlags pathfindItemFlags = flags;
                            if (flag2 && edge2.m_Specification.m_AccessRequirement != -1)
                            {
                                pathfindItemFlags |= PathfindItemFlags.ForbidExit | PathfindItemFlags.ReducedAccess;
                            }
                            if ((edgeFlags & EdgeFlags.AllowExit) != 0)
                            {
                                pathfindItemFlags &= ~PathfindItemFlags.ForbidExit;
                            }
                            if ((edgeFlags & EdgeFlags.AllowEnter) == 0)
                            {
                                pathfindItemFlags &= ~PathfindItemFlags.ReducedAccess;
                            }
                            CheckNextEdge(heapData.m_NodeIndex, edgeID2, pathNode, baseCost, pathfindItemFlags, in edge2);
                        }
                    }
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckNextEdge(int sourceIndex, EdgeID nextID, FullNode pathNode, float baseCost, PathfindItemFlags itemFlags, in Edge edge)
            {
                EdgeFlags flags = edge.m_Specification.m_Flags;
                RuleFlags rules = edge.m_Specification.m_Rules;
                rules = (RuleFlags)(
                    (uint)rules & (uint)(byte)(~(int)(((edge.m_Specification.m_Methods & PathMethod.Taxi) != 0) ? m_Parameters.m_TaxiIgnoredRules : m_Parameters.m_IgnoredRules))
                );
                float curvePos = math.select(edge.m_StartCurvePos, m_Parameters.m_ParkingDelta, edge.m_Owner == m_ParkingOwner);
                float curvePos2 = math.select(edge.m_EndCurvePos, m_Parameters.m_ParkingDelta, edge.m_Owner == m_ParkingOwner);
                float startDelta;
                bool3 directions;
                if (pathNode.Equals(new FullNode(edge.m_StartID, curvePos)))
                {
                    startDelta = 0f;
                    directions = new bool3((flags & m_Forward) != 0, (flags & m_ForwardMiddle) == m_ForwardMiddle, z: false);
                }
                else if (pathNode.Equals(new FullNode(edge.m_EndID, curvePos2)))
                {
                    startDelta = 1f;
                    directions = new bool3(x: false, (flags & m_BackwardMiddle) == m_BackwardMiddle, (flags & m_Backward) != 0);
                }
                else
                {
                    if (!pathNode.m_NodeID.Equals(edge.m_MiddleID))
                    {
                        return;
                    }
                    startDelta = pathNode.m_CurvePos;
                    directions = new bool3((flags & m_Forward) != 0, (flags & EdgeFlags.AllowMiddle) != 0, (flags & m_Backward) != 0);
                }
                bool reducedCost =
                    m_ParkingReset
                    && (
                        (itemFlags & PathfindItemFlags.ReducedCost) != 0
                        || (edge.m_Specification.m_Methods & (PathMethod.Parking | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0
                    );
                bool forbidExit = (itemFlags & PathfindItemFlags.ForbidExit) != 0;
                bool reducedAccess = (itemFlags & PathfindItemFlags.ReducedAccess) != 0;
                AddConnections(sourceIndex, nextID, in edge, flags, rules, baseCost, startDelta, directions, reducedCost, forbidExit, reducedAccess);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float CalculateCost(in PathSpecification pathSpecification, EdgeFlags flags, RuleFlags rules, float2 delta, Entity edgeEntity)
            {
                var laneRules = new LaneRules();
                var carFlags = new CarFlags();
                var sizeClass = new SizeClass();
                var energyTypes = new EnergyTypes();
                bool validLaneRules = false;
                if (
                    m_LaneRulesLookup.TryGetComponent(edgeEntity, out laneRules)
                    && m_CarLookup.TryGetComponent(m_Owner, out Car car)
                    && m_CarDataLookup.TryGetComponent(m_Owner, out CarData carData)
                )
                {
                    carFlags = car.m_Flags;
                    sizeClass = carData.m_SizeClass;
                    energyTypes = carData.m_EnergyType;
                    validLaneRules = true;
                }

                float num = PathUtils.CalculateSpeed(in pathSpecification, in m_Parameters);
                float num2 = delta.y - delta.x;
                float2 yz = math.select(
                    0f,
                    new float2(1f, 0.1f),
                    new bool2(
                        (
                            rules
                            & (
                                RuleFlags.ForbidCombustionEngines
                                | RuleFlags.ForbidTransitTraffic
                                | RuleFlags.ForbidHeavyTraffic
                                | RuleFlags.ForbidPrivateTraffic
                                | RuleFlags.ForbidSlowTraffic
                            )
                        ) != 0,
                        (rules & RuleFlags.AvoidBicycles) != 0
                    )
                );
                float4 value = pathSpecification.m_Costs.m_Value;
                value.xyw += pathSpecification.m_Length * new float3(1f / num, yz);
                value.y += math.select(0f, 100f, (flags & EdgeFlags.RequireAuthorization) != 0 != math.any(pathSpecification.m_AccessRequirement == m_AuthorizationMask));

                if (validLaneRules)
                {
                    value.y += math.select(
                        math.select(0f, -1 * math.min(10f, math.max(0, value.y)), LaneRulesUtils.IsPrefer(laneRules, carFlags, sizeClass, energyTypes)),
                        100f,
                        LaneRulesUtils.IsForbidden(laneRules, carFlags, sizeClass, energyTypes)
                    );
                }

                bool2 x = new float2(num2, 0f) >= new float2(0f, num2);
                x.x &= (flags & m_FreeForward) != 0;
                x.y &= (flags & m_FreeBackward) != 0;
                x.x |= (pathSpecification.m_Methods & m_Parameters.m_Methods) == PathMethod.Boarding;
                value.xyz = math.select(value.xyz, 0f, math.any(x));
                return math.dot(value, m_Parameters.m_Weights.m_Value) * math.abs(num2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float CalculateTotalCost(in LocationSpecification location, float baseCost, float endDelta)
            {
                float3 float5 = MathUtils.Position(location.m_Line, endDelta);
                float3 x = math.max(m_EndBounds.min - float5, float5 - m_EndBounds.max);
                return baseCost + math.length(math.max(x, 0f)) * m_HeuristicCostFactor;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddConnections(
                int sourceIndex,
                EdgeID id,
                in Edge edge,
                EdgeFlags flags,
                RuleFlags rules,
                float baseCost,
                float startDelta,
                bool3 directions,
                bool reducedCost,
                bool forbidExit,
                bool reducedAccess
            )
            {
                PathfindItemFlags pathfindItemFlags = (PathfindItemFlags)0;
                float num = 1f;
                if ((flags & EdgeFlags.SingleOnly) != 0)
                {
                    pathfindItemFlags |= PathfindItemFlags.SingleOnly;
                }
                if (reducedCost)
                {
                    pathfindItemFlags |= PathfindItemFlags.ReducedCost;
                    num = m_ReducedCostFactor;
                }
                if (forbidExit)
                {
                    pathfindItemFlags |= PathfindItemFlags.ForbidExit;
                }
                if (reducedAccess)
                {
                    pathfindItemFlags |= PathfindItemFlags.ReducedAccess;
                }
                float num2 = num * math.select(m_Random.NextFloat(0.5f, 1f), 1f, (m_Parameters.m_PathfindFlags & PathfindFlags.Stable) != 0);
                if (directions.x)
                {
                    float2 float5 = new float2(startDelta, 1f);
                    if (IsValidDelta(in edge.m_Specification, rules, float5))
                    {
                        float curvePos = math.select(edge.m_EndCurvePos, m_Parameters.m_ParkingDelta, edge.m_Owner == m_ParkingOwner);
                        AddHeapData(
                            pathNode: new FullNode(edge.m_EndID, curvePos),
                            sourceIndex: sourceIndex,
                            id: id,
                            edge: in edge,
                            flags: flags,
                            rules: rules,
                            baseCost: baseCost,
                            costFactor: num2,
                            edgeDelta: float5,
                            itemFlags: pathfindItemFlags
                        );
                    }
                }
                if (directions.y)
                {
                    int connectionCount = m_PathfindData.GetConnectionCount(edge.m_MiddleID);
                    if (connectionCount != 0)
                    {
                        int4 int5 = new int4(m_AccessMask, math.select(edge.m_Specification.m_AccessRequirement, -1, reducedAccess));
                        for (int i = 0; i < connectionCount; i++)
                        {
                            EdgeID edgeID = new EdgeID { m_Index = m_PathfindData.GetConnection(edge.m_MiddleID, i) };
                            int accessRequirement = m_PathfindData.GetAccessRequirement(edge.m_MiddleID, i);
                            if (id.Equals(edgeID) || math.all(int5 != accessRequirement))
                            {
                                continue;
                            }
                            ref Edge edge2 = ref m_PathfindData.GetEdge(edgeID);
                            EdgeFlags edgeFlags = edge2.m_Specification.m_Flags;
                            if (DisallowConnection(edge.m_Specification.m_Methods, pathfindItemFlags, in edge2.m_Specification, ref edgeFlags, edge2.m_Owner))
                            {
                                continue;
                            }
                            bool flag = edge2.m_Specification.m_AccessRequirement != edge.m_Specification.m_AccessRequirement;
                            if (forbidExit & flag)
                            {
                                continue;
                            }
                            PathfindItemFlags pathfindItemFlags2 = pathfindItemFlags;
                            if (flag && edge2.m_Specification.m_AccessRequirement != -1)
                            {
                                pathfindItemFlags2 |= PathfindItemFlags.ForbidExit | PathfindItemFlags.ReducedAccess;
                            }
                            if ((edgeFlags & EdgeFlags.AllowExit) != 0)
                            {
                                pathfindItemFlags2 &= ~PathfindItemFlags.ForbidExit;
                            }
                            if ((edgeFlags & EdgeFlags.AllowEnter) == 0)
                            {
                                pathfindItemFlags2 &= ~PathfindItemFlags.ReducedAccess;
                            }
                            if (edge.m_MiddleID.Equals(edge2.m_StartID) & ((edgeFlags & m_Forward) != 0))
                            {
                                float num3 = math.select(edge2.m_StartCurvePos, m_Parameters.m_ParkingDelta, edge2.m_Owner == m_ParkingOwner);
                                if ((directions.x & (num3 >= startDelta)) | (directions.z & (num3 <= startDelta)))
                                {
                                    float2 float6 = new float2(startDelta, num3);
                                    if (IsValidDelta(in edge.m_Specification, rules, float6))
                                    {
                                        AddHeapData(
                                            pathNode: new FullNode(edge2.m_StartID, num3),
                                            sourceIndex: sourceIndex,
                                            id: id,
                                            id2: edgeID,
                                            edge: in edge,
                                            flags: flags,
                                            rules: rules,
                                            baseCost: baseCost,
                                            costFactor: num2,
                                            edgeDelta: float6,
                                            itemFlags: pathfindItemFlags2
                                        );
                                    }
                                }
                            }
                            if (!(edge.m_MiddleID.Equals(edge2.m_EndID) & ((edgeFlags & m_Backward) != 0)))
                            {
                                continue;
                            }
                            float num4 = math.select(edge2.m_EndCurvePos, m_Parameters.m_ParkingDelta, edge2.m_Owner == m_ParkingOwner);
                            if ((directions.x & (num4 >= startDelta)) | (directions.z & (num4 <= startDelta)))
                            {
                                float2 float7 = new float2(startDelta, num4);
                                if (IsValidDelta(in edge.m_Specification, rules, float7))
                                {
                                    AddHeapData(
                                        pathNode: new FullNode(edge2.m_EndID, num4),
                                        sourceIndex: sourceIndex,
                                        id: id,
                                        id2: edgeID,
                                        edge: in edge,
                                        flags: flags,
                                        rules: rules,
                                        baseCost: baseCost,
                                        costFactor: num2,
                                        edgeDelta: float7,
                                        itemFlags: pathfindItemFlags2
                                    );
                                }
                            }
                        }
                    }
                }
                if (directions.z)
                {
                    float2 float8 = new float2(startDelta, 0f);
                    if (IsValidDelta(in edge.m_Specification, rules, float8))
                    {
                        float curvePos2 = math.select(edge.m_StartCurvePos, m_Parameters.m_ParkingDelta, edge.m_Owner == m_ParkingOwner);
                        AddHeapData(
                            pathNode: new FullNode(edge.m_StartID, curvePos2),
                            sourceIndex: sourceIndex,
                            id: id,
                            edge: in edge,
                            flags: flags,
                            rules: rules,
                            baseCost: baseCost,
                            costFactor: num2,
                            edgeDelta: float8,
                            itemFlags: pathfindItemFlags
                        );
                    }
                }
                FullNode pathNode = new FullNode(id, 0f);
                if (!TryGetFirstNodeIndex(pathNode, out var nodeIndex))
                {
                    return;
                }
                do
                {
                    ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
                    if (reference.m_PathNode.m_NodeID.Equals(pathNode.m_NodeID) && reference.m_SourceIndex < 0)
                    {
                        bool2 xz = directions.xz;
                        xz.x &= (reference.m_Flags & PathfindItemFlags.Forward) != 0;
                        xz.y &= (reference.m_Flags & PathfindItemFlags.Backward) != 0;
                        if ((xz.x & (reference.m_EdgeDelta.y >= startDelta)) | (xz.y & (reference.m_EdgeDelta.y <= startDelta)))
                        {
                            float2 float9 = new float2(startDelta, reference.m_EdgeDelta.y);
                            if (IsValidDelta(in edge.m_Specification, rules, float9))
                            {
                                FullNode fullNode = new FullNode(id, reference.m_EdgeDelta.y);
                                float num5 = reference.m_BaseCost;
                                if (reference.m_TotalCost < float.MaxValue)
                                {
                                    num5 = m_EndTargets[fullNode].m_Cost;
                                }
                                float num6 = baseCost + CalculateCost(in edge.m_Specification, flags, rules, float9, edge.m_Owner) * num2;
                                float num7 = num6 + num5 * num;
                                if (num7 < reference.m_TotalCost)
                                {
                                    PathfindItemFlags pathfindItemFlags3 = reference.m_Flags & (PathfindItemFlags.End | PathfindItemFlags.Forward | PathfindItemFlags.Backward);
                                    reference = new NodeData(
                                        -1 - sourceIndex,
                                        num7,
                                        num6,
                                        edge.m_Specification.m_AccessRequirement,
                                        id,
                                        default(EdgeID),
                                        float9,
                                        fullNode,
                                        pathfindItemFlags | pathfindItemFlags3,
                                        edge.m_Specification.m_Methods
                                    );
                                    HeapInsert(new HeapData(num7, nodeIndex));
                                }
                            }
                        }
                    }
                    nodeIndex = reference.m_NextNodeIndex;
                } while (nodeIndex != -1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool TryGetFirstNodeIndex(FullNode pathNode, out int nodeIndex)
            {
                int num = math.abs(pathNode.m_NodeID.m_Index) >> 4;
                int index = num >> 5;
                int num2 = 1 << (num & 0x1F);
                nodeIndex = m_NodeIndex[num];
                return (m_NodeIndexBits[index] & num2) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool GetOrAddNodeIndex(FullNode pathNode, out int nodeIndex)
            {
                int num = math.abs(pathNode.m_NodeID.m_Index) >> 4;
                int index = num >> 5;
                int num2 = 1 << (num & 0x1F);
                if ((m_NodeIndexBits[index] & num2) != 0)
                {
                    nodeIndex = m_NodeIndex[num];
                    ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
                    while (true)
                    {
                        if (reference.m_PathNode.Equals(pathNode))
                        {
                            return true;
                        }
                        nodeIndex = reference.m_NextNodeIndex;
                        if (nodeIndex == -1)
                        {
                            break;
                        }
                        reference = ref m_NodeData.ElementAt(nodeIndex);
                    }
                    nodeIndex = m_NodeData.Length;
                    reference.m_NextNodeIndex = nodeIndex;
                }
                else
                {
                    m_NodeIndexBits[index] |= num2;
                    nodeIndex = m_NodeData.Length;
                    m_NodeIndex[num] = nodeIndex;
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddHeapData(
                int sourceIndex,
                EdgeID id,
                in Edge edge,
                EdgeFlags flags,
                RuleFlags rules,
                float baseCost,
                float costFactor,
                FullNode pathNode,
                float2 edgeDelta,
                PathfindItemFlags itemFlags
            )
            {
                if (GetOrAddNodeIndex(pathNode, out var nodeIndex))
                {
                    ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
                    if (reference.m_SourceIndex < 0)
                    {
                        float baseCost2 = baseCost + CalculateCost(in edge.m_Specification, flags, rules, edgeDelta, edge.m_Owner) * costFactor;
                        float num = CalculateTotalCost(in edge.m_Location, baseCost2, edgeDelta.y);
                        if (num < reference.m_TotalCost)
                        {
                            reference = new NodeData(
                                -1 - sourceIndex,
                                num,
                                baseCost2,
                                edge.m_Specification.m_AccessRequirement,
                                id,
                                default(EdgeID),
                                edgeDelta,
                                pathNode,
                                itemFlags,
                                edge.m_Specification.m_Methods
                            );
                            HeapInsert(new HeapData(num, nodeIndex));
                        }
                    }
                }
                else
                {
                    float baseCost3 = baseCost + CalculateCost(in edge.m_Specification, flags, rules, edgeDelta, edge.m_Owner) * costFactor;
                    float totalCost = CalculateTotalCost(in edge.m_Location, baseCost3, edgeDelta.y);
                    m_NodeData.Add(
                        new NodeData(
                            -1 - sourceIndex,
                            totalCost,
                            baseCost3,
                            edge.m_Specification.m_AccessRequirement,
                            id,
                            default(EdgeID),
                            edgeDelta,
                            pathNode,
                            itemFlags,
                            edge.m_Specification.m_Methods
                        )
                    );
                    HeapInsert(new HeapData(totalCost, nodeIndex));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddHeapData(
                int sourceIndex,
                EdgeID id,
                EdgeID id2,
                in Edge edge,
                EdgeFlags flags,
                RuleFlags rules,
                float baseCost,
                float costFactor,
                FullNode pathNode,
                float2 edgeDelta,
                PathfindItemFlags itemFlags
            )
            {
                if (GetOrAddNodeIndex(pathNode, out var nodeIndex))
                {
                    ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
                    if (reference.m_SourceIndex < 0)
                    {
                        float baseCost2 = baseCost + CalculateCost(in edge.m_Specification, flags, rules, edgeDelta, edge.m_Owner) * costFactor;
                        float num = CalculateTotalCost(in edge.m_Location, baseCost2, edgeDelta.y);
                        if (num < reference.m_TotalCost)
                        {
                            reference = new NodeData(
                                -1 - sourceIndex,
                                num,
                                baseCost2,
                                edge.m_Specification.m_AccessRequirement,
                                id,
                                id2,
                                edgeDelta,
                                pathNode,
                                itemFlags | PathfindItemFlags.NextEdge,
                                edge.m_Specification.m_Methods
                            );
                            HeapInsert(new HeapData(num, nodeIndex));
                        }
                        else if (!id2.Equals(reference.m_NextID))
                        {
                            reference.m_NextID = default(EdgeID);
                            reference.m_Flags &= ~PathfindItemFlags.NextEdge;
                        }
                    }
                }
                else
                {
                    float baseCost3 = baseCost + CalculateCost(in edge.m_Specification, flags, rules, edgeDelta, edge.m_Owner) * costFactor;
                    float totalCost = CalculateTotalCost(in edge.m_Location, baseCost3, edgeDelta.y);
                    m_NodeData.Add(
                        new NodeData(
                            -1 - sourceIndex,
                            totalCost,
                            baseCost3,
                            edge.m_Specification.m_AccessRequirement,
                            id,
                            id2,
                            edgeDelta,
                            pathNode,
                            itemFlags | PathfindItemFlags.NextEdge,
                            edge.m_Specification.m_Methods
                        )
                    );
                    HeapInsert(new HeapData(totalCost, nodeIndex));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsValidDelta(in PathSpecification spec, RuleFlags rules, float2 delta)
            {
                if ((rules & RuleFlags.HasBlockage) == 0)
                {
                    return true;
                }
                if (!(math.min(delta.x, delta.y) > (float)(int)spec.m_BlockageEnd * 0.003921569f))
                {
                    return math.max(delta.x, delta.y) < (float)(int)spec.m_BlockageStart * 0.003921569f;
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool DisallowConnection(PathMethod prevMethod, PathfindItemFlags itemFlags, in PathSpecification newSpec, ref EdgeFlags edgeFlags, Entity newOwner)
            {
                if ((newSpec.m_Methods & m_Parameters.m_Methods) == 0 || ((itemFlags & PathfindItemFlags.SingleOnly) != 0 && (newSpec.m_Flags & EdgeFlags.SingleOnly) != 0))
                {
                    return true;
                }
                if ((newSpec.m_Methods & (PathMethod.Parking | PathMethod.Boarding | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0)
                {
                    if ((prevMethod & m_ParkingMethodMask) != 0)
                    {
                        edgeFlags |= EdgeFlags.AllowExit;
                        if (m_Parameters.m_ParkingTarget != newOwner)
                        {
                            return (newSpec.m_Flags & m_ParkingEdgeMask) == 0;
                        }
                        return false;
                    }
                    if ((prevMethod & (PathMethod.Parking | PathMethod.Boarding | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0)
                    {
                        return true;
                    }
                    return math.any(m_ParkingSize > new float2(newSpec.m_Density, newSpec.m_MaxSpeed));
                }
                return false;
            }

            public void CreatePath(
                int endNode,
                ref UnsafeList<PathfindPath> path,
                out float distance,
                out float duration,
                out int pathLength,
                out Entity origin,
                out Entity destination,
                out PathMethod methods
            )
            {
                distance = 0f;
                duration = 0f;
                pathLength = 0;
                methods = ~(
                    PathMethod.Pedestrian
                    | PathMethod.Road
                    | PathMethod.Parking
                    | PathMethod.PublicTransportDay
                    | PathMethod.Track
                    | PathMethod.Taxi
                    | PathMethod.CargoTransport
                    | PathMethod.CargoLoading
                    | PathMethod.Flying
                    | PathMethod.PublicTransportNight
                    | PathMethod.Boarding
                    | PathMethod.Offroad
                    | PathMethod.SpecialParking
                    | PathMethod.MediumRoad
                    | PathMethod.Bicycle
                    | PathMethod.BicycleParking
                );
                ref NodeData reference = ref m_NodeData.ElementAt(endNode);
                FullNode fullNode = default(FullNode);
                m_EndTargets.TryGetValue(reference.m_PathNode, out var item);
                destination = item.m_Entity;
                PathfindParameters pathfindParameters = m_Parameters;
                PathfindPath value = default(PathfindPath);
                while (true)
                {
                    ref Edge edge = ref m_PathfindData.GetEdge(reference.m_EdgeID);
                    bool test = (edge.m_Specification.m_Flags & EdgeFlags.OutsideConnection) != 0;
                    pathfindParameters.m_MaxSpeed = math.select(pathfindParameters.m_MaxSpeed, 277.77777f, test);
                    pathfindParameters.m_WalkSpeed = math.select(pathfindParameters.m_WalkSpeed, 277.77777f, test);
                    float num = PathUtils.CalculateLength(in edge.m_Specification, reference.m_EdgeDelta);
                    float num2 = PathUtils.CalculateSpeed(in edge.m_Specification, in pathfindParameters);
                    distance += num;
                    duration += num / num2;
                    pathLength++;
                    methods |= edge.m_Specification.m_Methods & m_Parameters.m_Methods;
                    if (path.IsCreated)
                    {
                        value.m_Target = edge.m_Owner;
                        value.m_TargetDelta = reference.m_EdgeDelta;
                        value.m_Flags = ~(
                            PathElementFlags.Secondary
                            | PathElementFlags.PathStart
                            | PathElementFlags.Action
                            | PathElementFlags.Return
                            | PathElementFlags.Reverse
                            | PathElementFlags.WaitPosition
                            | PathElementFlags.Leader
                            | PathElementFlags.Hangaround
                        );
                        if ((edge.m_Specification.m_Flags & EdgeFlags.Secondary) != 0)
                        {
                            value.m_Flags |= PathElementFlags.Secondary;
                        }
                        path.Add(in value);
                    }
                    if (reference.m_SourceIndex == int.MaxValue)
                    {
                        break;
                    }
                    reference = ref m_NodeData.ElementAt(reference.m_SourceIndex);
                }
                fullNode = new FullNode(reference.m_EdgeID, reference.m_EdgeDelta.x);
                m_StartTargets.TryGetValue(fullNode, out var item2);
                origin = item2.m_Entity;
                if (m_InvertPath)
                {
                    CommonUtils.Swap(ref origin, ref destination);
                }
                if (!path.IsCreated || path.Length <= 0)
                {
                    return;
                }
                if (m_InvertPath)
                {
                    for (int i = 0; i < path.Length; i++)
                    {
                        ref PathfindPath reference2 = ref path.ElementAt(i);
                        reference2.m_TargetDelta = reference2.m_TargetDelta.yx;
                    }
                }
                else
                {
                    int num3 = 0;
                    int num4 = path.Length - 1;
                    while (num3 < num4)
                    {
                        CommonUtils.Swap(ref path.ElementAt(num3++), ref path.ElementAt(num4--));
                    }
                }
                path.ElementAt(0).m_Flags |= PathElementFlags.PathStart;
            }
        }

        [BurstCompile]
        public struct PatchedPathfindJob : IJob
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

            public PathfindAction m_Action;

            [ReadOnly]
            public ComponentLookup<LaneRules> m_LaneRulesData;

            [ReadOnly]
            public ComponentLookup<Car> m_CarLookup;

            [ReadOnly]
            public ComponentLookup<CarData> m_CarDataLookup;

            public Entity m_Owner;

            public void Execute()
            {
                Execute(
                    m_PathfindData,
                    Allocator.Temp,
                    m_RandomSeed.GetRandom(0),
                    m_PathfindHeuristicData,
                    m_MaxPassengerTransportSpeed,
                    m_MaxCargoTransportSpeed,
                    ref m_Action.data,
                    m_LaneRulesData,
                    m_CarLookup,
                    m_CarDataLookup,
                    m_Owner
                );
            }

            public static void Execute(
                NativePathfindData pathfindData,
                Allocator allocator,
                Unity.Mathematics.Random random,
                PathfindHeuristicData pathfindHeuristicData,
                float maxPassengerTransportSpeed,
                float maxCargoTransportSpeed,
                ref PathfindActionData actionData,
                ComponentLookup<LaneRules> laneRulesData,
                ComponentLookup<Car> carLookup,
                ComponentLookup<CarData> carDataLookup,
                Entity owner
            )
            {
                PathfindResult value = new PathfindResult
                {
                    m_Distance = -1f,
                    m_Duration = -1f,
                    m_TotalCost = -1f,
                };
                if (actionData.m_StartTargets.Length == 0 || actionData.m_EndTargets.Length == 0)
                {
                    actionData.m_Result.Add(in value);
                    return;
                }
                UnsafeList<PathfindPath> unsafeList = default(UnsafeList<PathfindPath>);
                ref UnsafeList<PathfindPath> reference = ref unsafeList;
                reference = ref actionData.m_Path;
                PathfindParameters parameters = actionData.m_Parameters;
                actionData.m_Result.Capacity = math.max(1, parameters.m_MaxResultCount);
                if ((parameters.m_PathfindFlags & PathfindFlags.SkipPathfind) != 0)
                {
                    value.m_Distance = 0f;
                    value.m_Duration = 0f;
                    value.m_TotalCost = actionData.m_StartTargets[0].m_Cost + actionData.m_EndTargets[0].m_Cost;
                    value.m_GraphTraversal = 1;
                    value.m_PathLength = 1;
                    value.m_Origin = actionData.m_StartTargets[0].m_Entity;
                    value.m_Destination = actionData.m_EndTargets[0].m_Entity;
                }
                else
                {
                    PatchedPathfindExecutor pathfindExecutor = default(PatchedPathfindExecutor);
                    pathfindExecutor.Initialize(
                        pathfindData,
                        allocator,
                        random,
                        parameters,
                        pathfindHeuristicData,
                        maxPassengerTransportSpeed,
                        maxCargoTransportSpeed,
                        laneRulesData,
                        carLookup,
                        carDataLookup,
                        owner
                    );
                    pathfindExecutor.AddTargets(actionData.m_StartTargets, actionData.m_EndTargets, ref value.m_ErrorCode);
                    int endNode;
                    while (pathfindExecutor.FindEndNode(out endNode, out value.m_TotalCost, out value.m_GraphTraversal))
                    {
                        pathfindExecutor.CreatePath(
                            endNode,
                            ref reference,
                            out value.m_Distance,
                            out value.m_Duration,
                            out value.m_PathLength,
                            out value.m_Origin,
                            out value.m_Destination,
                            out value.m_Methods
                        );
                        actionData.m_Result.Add(in value);
                        value = new PathfindResult
                        {
                            m_Distance = -1f,
                            m_Duration = -1f,
                            m_TotalCost = -1f,
                        };
                        reference = ref unsafeList;
                    }
                    pathfindExecutor.Release();
                }
                if (actionData.m_Result.Length == 0)
                {
                    actionData.m_Result.Add(in value);
                }
            }
        }
    }
}
