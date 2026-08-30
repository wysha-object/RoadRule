// Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Game.Simulation.CarNavigationSystem
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using RoadRule.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace RoadRule.Systems.Simulation
{
    public partial class PatchedCarNavigationSystem : GameSystemBase
    {
        [BurstCompile]
        private struct UpdateNavigationJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

            [ReadOnly]
            public ComponentTypeHandle<Moving> m_MovingType;

            [ReadOnly]
            public ComponentTypeHandle<Target> m_TargetType;

            [ReadOnly]
            public ComponentTypeHandle<Car> m_CarType;

            [ReadOnly]
            public ComponentTypeHandle<Bicycle> m_BicycleType;

            [ReadOnly]
            public ComponentTypeHandle<OutOfControl> m_OutOfControlType;

            [ReadOnly]
            public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

            [ReadOnly]
            public BufferTypeHandle<LayoutElement> m_LayoutElementType;

            public ComponentTypeHandle<CarNavigation> m_NavigationType;

            public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

            public ComponentTypeHandle<PathOwner> m_PathOwnerType;

            public ComponentTypeHandle<Blocker> m_BlockerType;

            public ComponentTypeHandle<Odometer> m_OdometerType;

            public BufferTypeHandle<CarNavigationLane> m_NavigationLaneType;

            public BufferTypeHandle<PathElement> m_PathElementType;

            [ReadOnly]
            public EntityStorageInfoLookup m_EntityStorageInfoLookup;

            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;

            [ReadOnly]
            public ComponentLookup<Unspawned> m_UnspawnedData;

            [ReadOnly]
            public ComponentLookup<Lane> m_LaneData;

            [ReadOnly]
            public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

            [ReadOnly]
            public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

            [ReadOnly]
            public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

            [ReadOnly]
            public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

            [ReadOnly]
            public ComponentLookup<MasterLane> m_MasterLaneData;

            [ReadOnly]
            public ComponentLookup<SlaveLane> m_SlaveLaneData;

            [ReadOnly]
            public ComponentLookup<AreaLane> m_AreaLaneData;

            [ReadOnly]
            public ComponentLookup<Curve> m_CurveData;

            [ReadOnly]
            public ComponentLookup<NodeLane> m_NodeLaneData;

            [ReadOnly]
            public ComponentLookup<LaneReservation> m_LaneReservationData;

            [ReadOnly]
            public ComponentLookup<LaneCondition> m_LaneConditionData;

            [ReadOnly]
            public ComponentLookup<LaneSignal> m_LaneSignalData;

            [ReadOnly]
            public ComponentLookup<Road> m_RoadData;

            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenterData;

            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;

            [ReadOnly]
            public ComponentLookup<Position> m_PositionData;

            [ReadOnly]
            public ComponentLookup<Moving> m_MovingData;

            [ReadOnly]
            public ComponentLookup<Bicycle> m_BicycleData;

            [ReadOnly]
            public ComponentLookup<Train> m_TrainData;

            [ReadOnly]
            public ComponentLookup<Controller> m_ControllerData;

            [ReadOnly]
            public ComponentLookup<LaneRules> m_CarLaneRulesData;

            [ReadOnly]
            public ComponentLookup<Vehicle> m_VehicleData;

            [ReadOnly]
            public ComponentLookup<Creature> m_CreatureData;

            [ReadOnly]
            public ComponentLookup<TrainData> m_PrefabTrainData;

            [ReadOnly]
            public ComponentLookup<BuildingData> m_PrefabBuildingData;

            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

            [ReadOnly]
            public ComponentLookup<VehicleSideEffectData> m_PrefabSideEffectData;

            [ReadOnly]
            public ComponentLookup<NetLaneData> m_PrefabLaneData;

            [ReadOnly]
            public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

            [ReadOnly]
            public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

            [ReadOnly]
            public BufferLookup<Game.Net.SubLane> m_Lanes;

            [ReadOnly]
            public BufferLookup<LaneObject> m_LaneObjects;

            [ReadOnly]
            public BufferLookup<LaneOverlap> m_LaneOverlaps;

            [ReadOnly]
            public BufferLookup<Game.Areas.Node> m_AreaNodes;

            [ReadOnly]
            public BufferLookup<Triangle> m_AreaTriangles;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CarTrailerLane> m_TrailerLaneData;

            [NativeDisableParallelForRestriction]
            public BufferLookup<BlockedLane> m_BlockedLanes;

            [ReadOnly]
            public RandomSeed m_RandomSeed;

            [ReadOnly]
            public uint m_SimulationFrame;

            [ReadOnly]
            public bool m_LeftHandTraffic;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

            [ReadOnly]
            public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            public LaneObjectCommandBuffer m_LaneObjectBuffer;

            public NativeQueue<CarNavigationHelpers.LaneReservation>.ParallelWriter m_LaneReservations;

            public NativeQueue<CarNavigationHelpers.LaneEffects>.ParallelWriter m_LaneEffects;

            public NativeQueue<CarNavigationHelpers.LaneSignal>.ParallelWriter m_LaneSignals;

            public NativeQueue<CarNavigationSystem.TrafficAmbienceEffect>.ParallelWriter m_TrafficAmbienceEffects;

            [ReadOnly]
            public ComponentLookup<Car> m_CarLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;

            [ReadOnly]
            public ComponentLookup<CarData> m_PrefabCarDataLookup;

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

            [ReadOnly]
            public ComponentLookup<Game.Vehicles.Taxi> m_TaxiLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref m_MovingType);
                NativeArray<Blocker> nativeArray4 = chunk.GetNativeArray(ref m_BlockerType);
                NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
                NativeArray<CarNavigation> nativeArray6 = chunk.GetNativeArray(ref m_NavigationType);
                NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
                NativeArray<PathOwner> nativeArray8 = chunk.GetNativeArray(ref m_PathOwnerType);
                BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_NavigationLaneType);
                BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
                BufferAccessor<LayoutElement> bufferAccessor3 = chunk.GetBufferAccessor(ref m_LayoutElementType);
                Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
                bool isBicycle = chunk.Has(ref m_BicycleType);
                if (chunk.Has(ref m_OutOfControlType))
                {
                    NativeList<BlockedLane> nativeList = new NativeList<BlockedLane>(16, Allocator.Temp);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        Entity entity = nativeArray[i];
                        Game.Objects.Transform transform = nativeArray2[i];
                        CarNavigation carNavigation = nativeArray6[i];
                        CarCurrentLane currentLane = nativeArray5[i];
                        Blocker blocker = nativeArray4[i];
                        PathOwner pathOwner = nativeArray8[i];
                        PrefabRef prefabRef = nativeArray7[i];
                        DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor[i];
                        DynamicBuffer<PathElement> pathElements = bufferAccessor2[i];
                        DynamicBuffer<BlockedLane> blockedLanes = m_BlockedLanes[entity];
                        ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                        Moving moving = default(Moving);
                        if (nativeArray3.Length != 0)
                        {
                            moving = nativeArray3[i];
                        }
                        CarNavigationHelpers.CurrentLaneCache currentLaneCache = new CarNavigationHelpers.CurrentLaneCache(
                            ref currentLane,
                            blockedLanes,
                            m_EntityStorageInfoLookup,
                            m_MovingObjectSearchTree
                        );
                        UpdateOutOfControl(
                            entity,
                            transform,
                            objectGeometryData,
                            ref carNavigation,
                            ref currentLane,
                            ref blocker,
                            ref pathOwner,
                            navigationLanes,
                            pathElements,
                            blockedLanes,
                            nativeList
                        );
                        currentLaneCache.CheckChanges(entity, ref currentLane, nativeList, m_LaneObjectBuffer, m_LaneObjects, transform, moving, carNavigation, objectGeometryData);
                        nativeArray6[i] = carNavigation;
                        nativeArray5[i] = currentLane;
                        nativeArray8[i] = pathOwner;
                        nativeArray4[i] = blocker;
                        nativeList.Clear();
                        if (bufferAccessor3.Length != 0)
                        {
                            UpdateOutOfControlTrailers(carNavigation, bufferAccessor3[i], nativeList);
                        }
                    }
                    nativeList.Dispose();
                    return;
                }
                if (nativeArray3.Length != 0)
                {
                    NativeArray<Target> nativeArray9 = chunk.GetNativeArray(ref m_TargetType);
                    NativeArray<Car> nativeArray10 = chunk.GetNativeArray(ref m_CarType);
                    NativeArray<Odometer> nativeArray11 = chunk.GetNativeArray(ref m_OdometerType);
                    NativeArray<PseudoRandomSeed> nativeArray12 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
                    NativeList<Entity> tempBuffer = default(NativeList<Entity>);
                    CarLaneSelectBuffer laneSelectBuffer = default(CarLaneSelectBuffer);
                    bool flag = nativeArray11.Length != 0;
                    for (int j = 0; j < chunk.Count; j++)
                    {
                        Entity entity2 = nativeArray[j];
                        Game.Objects.Transform transform2 = nativeArray2[j];
                        Moving moving2 = nativeArray3[j];
                        Target target = nativeArray9[j];
                        Car car = nativeArray10[j];
                        CarNavigation navigation = nativeArray6[j];
                        CarCurrentLane currentLane2 = nativeArray5[j];
                        PseudoRandomSeed pseudoRandomSeed = nativeArray12[j];
                        Blocker blocker2 = nativeArray4[j];
                        PathOwner pathOwner2 = nativeArray8[j];
                        PrefabRef prefabRef2 = nativeArray7[j];
                        DynamicBuffer<CarNavigationLane> navigationLanes2 = bufferAccessor[j];
                        DynamicBuffer<PathElement> pathElements2 = bufferAccessor2[j];
                        DynamicBuffer<BlockedLane> blockedLanes2 = m_BlockedLanes[entity2];
                        CarData prefabCarData = m_PrefabCarDataLookup[prefabRef2.m_Prefab];
                        ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
                        if (bufferAccessor3.Length != 0)
                        {
                            UpdateCarLimits(ref prefabCarData, bufferAccessor3[j]);
                        }
                        CarNavigationHelpers.CurrentLaneCache currentLaneCache2 = new CarNavigationHelpers.CurrentLaneCache(
                            ref currentLane2,
                            blockedLanes2,
                            m_EntityStorageInfoLookup,
                            m_MovingObjectSearchTree
                        );
                        int priority = VehicleUtils.GetPriority(car);
                        Odometer odometer = default(Odometer);
                        if (flag)
                        {
                            odometer = nativeArray11[j];
                        }
                        UpdateNavigationLanes(
                            ref random,
                            priority,
                            entity2,
                            transform2,
                            moving2,
                            target,
                            car,
                            isBicycle,
                            prefabCarData,
                            ref laneSelectBuffer,
                            ref currentLane2,
                            ref blocker2,
                            ref pathOwner2,
                            navigationLanes2,
                            pathElements2
                        );
                        UpdateNavigationTarget(
                            ref random,
                            priority,
                            entity2,
                            transform2,
                            moving2,
                            car,
                            pseudoRandomSeed,
                            prefabRef2,
                            prefabCarData,
                            objectGeometryData2,
                            isBicycle,
                            ref navigation,
                            ref currentLane2,
                            ref blocker2,
                            ref odometer,
                            ref pathOwner2,
                            ref tempBuffer,
                            navigationLanes2,
                            pathElements2
                        );
                        ReserveNavigationLanes(ref random, priority, entity2, prefabCarData, objectGeometryData2, car, ref navigation, ref currentLane2, navigationLanes2);
                        currentLaneCache2.CheckChanges(
                            entity2,
                            ref currentLane2,
                            default(NativeList<BlockedLane>),
                            m_LaneObjectBuffer,
                            m_LaneObjects,
                            transform2,
                            moving2,
                            navigation,
                            objectGeometryData2
                        );
                        m_TrafficAmbienceEffects.Enqueue(
                            new CarNavigationSystem.TrafficAmbienceEffect
                            {
                                m_Amount = CalculateNoise(ref currentLane2, prefabRef2, prefabCarData),
                                m_Position = transform2.m_Position,
                            }
                        );
                        nativeArray6[j] = navigation;
                        nativeArray5[j] = currentLane2;
                        nativeArray8[j] = pathOwner2;
                        nativeArray4[j] = blocker2;
                        if (flag)
                        {
                            nativeArray11[j] = odometer;
                        }
                        if (bufferAccessor3.Length != 0)
                        {
                            UpdateTrailers(navigation, currentLane2, bufferAccessor3[j], isBicycle);
                        }
                    }
                    laneSelectBuffer.Dispose();
                    if (tempBuffer.IsCreated)
                    {
                        tempBuffer.Dispose();
                    }
                    return;
                }
                for (int k = 0; k < chunk.Count; k++)
                {
                    Entity entity3 = nativeArray[k];
                    Game.Objects.Transform transform3 = nativeArray2[k];
                    CarNavigation navigation2 = nativeArray6[k];
                    CarCurrentLane currentLane3 = nativeArray5[k];
                    Blocker blocker3 = nativeArray4[k];
                    PathOwner pathOwner3 = nativeArray8[k];
                    PrefabRef prefabRef3 = nativeArray7[k];
                    DynamicBuffer<CarNavigationLane> navigationLanes3 = bufferAccessor[k];
                    DynamicBuffer<PathElement> pathElements3 = bufferAccessor2[k];
                    DynamicBuffer<BlockedLane> blockedLanes3 = m_BlockedLanes[entity3];
                    ObjectGeometryData objectGeometryData3 = m_PrefabObjectGeometryData[prefabRef3.m_Prefab];
                    CarNavigationHelpers.CurrentLaneCache currentLaneCache3 = new CarNavigationHelpers.CurrentLaneCache(
                        ref currentLane3,
                        blockedLanes3,
                        m_EntityStorageInfoLookup,
                        m_MovingObjectSearchTree
                    );
                    UpdateStopped(entity3, transform3, ref currentLane3, ref blocker3, ref pathOwner3, navigationLanes3, pathElements3, isBicycle);
                    currentLaneCache3.CheckChanges(
                        entity3,
                        ref currentLane3,
                        default(NativeList<BlockedLane>),
                        m_LaneObjectBuffer,
                        m_LaneObjects,
                        transform3,
                        default(Moving),
                        navigation2,
                        objectGeometryData3
                    );
                    nativeArray5[k] = currentLane3;
                    nativeArray8[k] = pathOwner3;
                    nativeArray4[k] = blocker3;
                    if (bufferAccessor3.Length != 0)
                    {
                        UpdateStoppedTrailers(navigation2, bufferAccessor3[k], isBicycle);
                    }
                }
            }

            private void UpdateCarLimits(ref CarData prefabCarData, DynamicBuffer<LayoutElement> layout)
            {
                for (int i = 1; i < layout.Length; i++)
                {
                    Entity vehicle = layout[i].m_Vehicle;
                    PrefabRef prefabRef = m_PrefabRefLookup[vehicle];
                    CarData carData = m_PrefabCarDataLookup[prefabRef.m_Prefab];
                    prefabCarData.m_Acceleration = math.min(prefabCarData.m_Acceleration, carData.m_Acceleration);
                    prefabCarData.m_Braking = math.min(prefabCarData.m_Braking, carData.m_Braking);
                    prefabCarData.m_MaxSpeed = math.min(prefabCarData.m_MaxSpeed, carData.m_MaxSpeed);
                    prefabCarData.m_Turning = math.min(prefabCarData.m_Turning, carData.m_Turning);
                }
            }

            private void UpdateTrailers(CarNavigation navigation, CarCurrentLane currentLane, DynamicBuffer<LayoutElement> layout, bool isBicycle)
            {
                Entity lane = currentLane.m_Lane;
                float2 nextPosition = currentLane.m_CurvePosition.xy;
                bool forceNext = (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0;
                for (int i = 1; i < layout.Length; i++)
                {
                    Entity vehicle = layout[i].m_Vehicle;
                    CarTrailerLane trailerLane = m_TrailerLaneData[vehicle];
                    Game.Objects.Transform transform = m_TransformData[vehicle];
                    Moving moving = m_MovingData[vehicle];
                    DynamicBuffer<BlockedLane> blockedLanes = m_BlockedLanes[vehicle];
                    PrefabRef prefabRef = m_PrefabRefLookup[vehicle];
                    ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                    CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(
                        ref trailerLane,
                        blockedLanes,
                        m_PrefabRefLookup,
                        m_MovingObjectSearchTree
                    );
                    if (trailerLane.m_Lane == Entity.Null)
                    {
                        TryFindCurrentLane(
                            ref trailerLane,
                            transform,
                            moving,
                            isBicycle,
                            CarNavigationHelpers.ResolveLaneRestrictionOwner(vehicle, m_OwnerData, m_PropertyRenterData)
                        );
                    }
                    UpdateTrailer(vehicle, transform, objectGeometryData, lane, nextPosition, forceNext, ref trailerLane);
                    trailerLaneCache.CheckChanges(
                        vehicle,
                        ref trailerLane,
                        default(NativeList<BlockedLane>),
                        m_LaneObjectBuffer,
                        m_LaneObjects,
                        transform,
                        moving,
                        navigation,
                        objectGeometryData
                    );
                    m_TrailerLaneData[vehicle] = trailerLane;
                    lane = trailerLane.m_Lane;
                    nextPosition = trailerLane.m_CurvePosition;
                }
            }

            private void UpdateOutOfControlTrailers(CarNavigation navigation, DynamicBuffer<LayoutElement> layout, NativeList<BlockedLane> tempBlockedLanes)
            {
                for (int i = 1; i < layout.Length; i++)
                {
                    Entity vehicle = layout[i].m_Vehicle;
                    CarTrailerLane trailerLane = m_TrailerLaneData[vehicle];
                    Game.Objects.Transform transform = m_TransformData[vehicle];
                    Moving moving = m_MovingData[vehicle];
                    DynamicBuffer<BlockedLane> blockedLanes = m_BlockedLanes[vehicle];
                    PrefabRef prefabRef = m_PrefabRefLookup[vehicle];
                    ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                    CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(
                        ref trailerLane,
                        blockedLanes,
                        m_PrefabRefLookup,
                        m_MovingObjectSearchTree
                    );
                    UpdateOutOfControl(vehicle, transform, objectGeometryData, ref trailerLane, blockedLanes, tempBlockedLanes);
                    trailerLaneCache.CheckChanges(vehicle, ref trailerLane, tempBlockedLanes, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
                    m_TrailerLaneData[vehicle] = trailerLane;
                    tempBlockedLanes.Clear();
                }
            }

            private void UpdateStoppedTrailers(CarNavigation navigation, DynamicBuffer<LayoutElement> layout, bool isBicycle)
            {
                for (int i = 1; i < layout.Length; i++)
                {
                    Entity vehicle = layout[i].m_Vehicle;
                    CarTrailerLane trailerLane = m_TrailerLaneData[vehicle];
                    Game.Objects.Transform transform = m_TransformData[vehicle];
                    DynamicBuffer<BlockedLane> blockedLanes = m_BlockedLanes[vehicle];
                    PrefabRef prefabRef = m_PrefabRefLookup[vehicle];
                    ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
                    CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(
                        ref trailerLane,
                        blockedLanes,
                        m_PrefabRefLookup,
                        m_MovingObjectSearchTree
                    );
                    if (trailerLane.m_Lane == Entity.Null)
                    {
                        TryFindCurrentLane(
                            ref trailerLane,
                            transform,
                            default(Moving),
                            isBicycle,
                            CarNavigationHelpers.ResolveLaneRestrictionOwner(vehicle, m_OwnerData, m_PropertyRenterData)
                        );
                    }
                    trailerLaneCache.CheckChanges(
                        vehicle,
                        ref trailerLane,
                        default(NativeList<BlockedLane>),
                        m_LaneObjectBuffer,
                        m_LaneObjects,
                        transform,
                        default(Moving),
                        navigation,
                        objectGeometryData
                    );
                    m_TrailerLaneData[vehicle] = trailerLane;
                }
            }

            private void UpdateStopped(
                Entity entity,
                Game.Objects.Transform transform,
                ref CarCurrentLane currentLane,
                ref Blocker blocker,
                ref PathOwner pathOwner,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<PathElement> pathElements,
                bool isBicycle
            )
            {
                if (currentLane.m_Lane == Entity.Null || (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Obsolete) != 0)
                {
                    TryFindCurrentLane(
                        ref currentLane,
                        transform,
                        default(Moving),
                        isBicycle,
                        CarNavigationHelpers.ResolveLaneRestrictionOwner(entity, m_OwnerData, m_PropertyRenterData)
                    );
                    navigationLanes.Clear();
                    pathElements.Clear();
                    pathOwner.m_ElementIndex = 0;
                    pathOwner.m_State |= PathFlags.Obsolete;
                }
                if (
                    (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.QueueReached) != 0
                    && (!m_CarLookup.HasComponent(blocker.m_Blocker) || (m_CarLookup[blocker.m_Blocker].m_Flags & CarFlags.Queueing) == 0)
                )
                {
                    currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.QueueReached;
                    blocker = default(Blocker);
                }
            }

            private void UpdateOutOfControl(
                Entity entity,
                Game.Objects.Transform transform,
                ObjectGeometryData prefabObjectGeometryData,
                ref CarTrailerLane trailerLane,
                DynamicBuffer<BlockedLane> blockedLanes,
                NativeList<BlockedLane> tempBlockedLanes
            )
            {
                float3 position = transform.m_Position;
                float3 float5 = math.forward(transform.m_Rotation);
                Line3.Segment line = new Line3.Segment(
                    position - float5 * math.max(0.1f, 0f - prefabObjectGeometryData.m_Bounds.min.z - prefabObjectGeometryData.m_Size.x * 0.5f),
                    position + float5 * math.max(0.1f, prefabObjectGeometryData.m_Bounds.max.z - prefabObjectGeometryData.m_Size.x * 0.5f)
                );
                float num = prefabObjectGeometryData.m_Size.x * 0.5f;
                Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(line), num);
                CarNavigationHelpers.FindBlockedLanesIterator iterator = new CarNavigationHelpers.FindBlockedLanesIterator
                {
                    m_Bounds = bounds,
                    m_Line = line,
                    m_Radius = num,
                    m_BlockedLanes = tempBlockedLanes,
                    m_SubLanes = m_Lanes,
                    m_MasterLaneData = m_MasterLaneData,
                    m_CurveData = m_CurveData,
                    m_PrefabRefData = m_PrefabRefLookup,
                    m_PrefabLaneData = m_PrefabLaneData,
                };
                m_NetSearchTree.Iterate(ref iterator);
                trailerLane = default(CarTrailerLane);
            }

            private void UpdateOutOfControl(
                Entity entity,
                Game.Objects.Transform transform,
                ObjectGeometryData prefabObjectGeometryData,
                ref CarNavigation carNavigation,
                ref CarCurrentLane currentLane,
                ref Blocker blocker,
                ref PathOwner pathOwner,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<PathElement> pathElements,
                DynamicBuffer<BlockedLane> blockedLanes,
                NativeList<BlockedLane> tempBlockedLanes
            )
            {
                float3 position = transform.m_Position;
                float3 float5 = math.forward(transform.m_Rotation);
                Line3.Segment line = new Line3.Segment(
                    position - float5 * math.max(0.1f, 0f - prefabObjectGeometryData.m_Bounds.min.z - prefabObjectGeometryData.m_Size.x * 0.5f),
                    position + float5 * math.max(0.1f, prefabObjectGeometryData.m_Bounds.max.z - prefabObjectGeometryData.m_Size.x * 0.5f)
                );
                float num = prefabObjectGeometryData.m_Size.x * 0.5f;
                Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(line), num);
                CarNavigationHelpers.FindBlockedLanesIterator iterator = new CarNavigationHelpers.FindBlockedLanesIterator
                {
                    m_Bounds = bounds,
                    m_Line = line,
                    m_Radius = num,
                    m_BlockedLanes = tempBlockedLanes,
                    m_SubLanes = m_Lanes,
                    m_MasterLaneData = m_MasterLaneData,
                    m_CurveData = m_CurveData,
                    m_PrefabRefData = m_PrefabRefLookup,
                    m_PrefabLaneData = m_PrefabLaneData,
                };
                m_NetSearchTree.Iterate(ref iterator);
                carNavigation = new CarNavigation { m_TargetPosition = transform.m_Position };
                currentLane = default(CarCurrentLane);
                blocker = default(Blocker);
                pathOwner.m_ElementIndex = 0;
                navigationLanes.Clear();
                pathElements.Clear();
            }

            private void UpdateNavigationLanes(
                ref Unity.Mathematics.Random random,
                int priority,
                Entity entity,
                Game.Objects.Transform transform,
                Moving moving,
                Target target,
                Car car,
                bool isBicycle,
                CarData prefabCarData,
                ref CarLaneSelectBuffer laneSelectBuffer,
                ref CarCurrentLane currentLane,
                ref Blocker blocker,
                ref PathOwner pathOwner,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<PathElement> pathElements
            )
            {
                int invalidPath = 10000000;
                if (currentLane.m_Lane == Entity.Null || (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Obsolete) != 0)
                {
                    invalidPath = -1;
                    TryFindCurrentLane(ref currentLane, transform, moving, isBicycle, CarNavigationHelpers.ResolveLaneRestrictionOwner(entity, m_OwnerData, m_PropertyRenterData));
                    if (currentLane.m_Lane == Entity.Null && (pathOwner.m_State & PathFlags.Updated) != 0 && pathElements.Length > pathOwner.m_ElementIndex)
                    {
                        PathElement pathElement = pathElements[pathOwner.m_ElementIndex];
                        Game.Vehicles.CarLaneFlags carLaneFlags = Game.Vehicles.CarLaneFlags.FixedLane;
                        if (m_ConnectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData))
                        {
                            carLaneFlags = (
                                ((componentData.m_Flags & ConnectionLaneFlags.Area) == 0)
                                    ? (carLaneFlags | Game.Vehicles.CarLaneFlags.Connection)
                                    : (carLaneFlags | Game.Vehicles.CarLaneFlags.Area)
                            );
                        }
                        currentLane = new CarCurrentLane(pathElement, carLaneFlags);
                        invalidPath = 10000000;
                    }
                    else if (currentLane.m_Lane == Entity.Null && (pathOwner.m_State & (PathFlags.Pending | PathFlags.Scheduled)) != 0)
                    {
                        invalidPath = 10000000;
                    }
                }
                else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 && (pathOwner.m_State & PathFlags.Append) == 0)
                {
                    ClearNavigationLanes(ref currentLane, navigationLanes, invalidPath);
                }
                else if ((pathOwner.m_State & PathFlags.Updated) == 0)
                {
                    FillNavigationPaths(
                        ref random,
                        priority,
                        entity,
                        transform,
                        target,
                        car,
                        isBicycle,
                        prefabCarData,
                        ref laneSelectBuffer,
                        ref currentLane,
                        ref blocker,
                        ref pathOwner,
                        navigationLanes,
                        pathElements,
                        ref invalidPath
                    );
                }
                if (invalidPath != 10000000)
                {
                    ClearNavigationLanes(moving, prefabCarData, ref currentLane, navigationLanes, invalidPath, isBicycle);
                    pathElements.Clear();
                    pathOwner.m_ElementIndex = 0;
                    pathOwner.m_State |= PathFlags.Obsolete;
                }
            }

            private void ClearNavigationLanes(ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes, int invalidPath)
            {
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ClearedForPathfind) == 0)
                {
                    currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
                }
                if (invalidPath > 0)
                {
                    for (int i = 0; i < navigationLanes.Length; i++)
                    {
                        if ((navigationLanes[i].m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.ClearedForPathfind)) == 0)
                        {
                            invalidPath = math.min(i, invalidPath);
                            break;
                        }
                    }
                }
                invalidPath = math.max(invalidPath, 0);
                if (invalidPath < navigationLanes.Length)
                {
                    navigationLanes.RemoveRange(invalidPath, navigationLanes.Length - invalidPath);
                }
            }

            private void ClearNavigationLanes(
                Moving moving,
                CarData prefabCarData,
                ref CarCurrentLane currentLane,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                int invalidPath,
                bool isBicycle
            )
            {
                if (invalidPath >= 0)
                {
                    VehicleUtils.ClearNavigationForPathfind(
                        moving,
                        prefabCarData,
                        isBicycle,
                        ref currentLane,
                        navigationLanes,
                        ref m_CarLaneData,
                        ref m_PedestrianLaneData,
                        ref m_CurveData
                    );
                }
                else
                {
                    currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
                }
                invalidPath = math.max(invalidPath, 0);
                if (invalidPath < navigationLanes.Length)
                {
                    navigationLanes.RemoveRange(invalidPath, navigationLanes.Length - invalidPath);
                }
            }

            private void TryFindCurrentLane(
                ref CarCurrentLane currentLane,
                Game.Objects.Transform transform,
                Moving moving,
                bool isBicycle,
                Entity accessRestrictionOwner = default(Entity)
            )
            {
                float num = 4f / 15f;
                currentLane.m_LaneFlags &= ~(
                    Game.Vehicles.CarLaneFlags.TransformTarget
                    | Game.Vehicles.CarLaneFlags.ParkingSpace
                    | Game.Vehicles.CarLaneFlags.Obsolete
                    | Game.Vehicles.CarLaneFlags.TurnLeft
                    | Game.Vehicles.CarLaneFlags.TurnRight
                    | Game.Vehicles.CarLaneFlags.Area
                );
                currentLane.m_Lane = Entity.Null;
                currentLane.m_ChangeLane = Entity.Null;
                float3 float5 = transform.m_Position + moving.m_Velocity * (num * 2f);
                float num2 = 100f;
                Bounds3 bounds = new Bounds3(float5 - num2, float5 + num2);
                CarNavigationHelpers.FindLaneIterator iterator = new CarNavigationHelpers.FindLaneIterator
                {
                    m_Bounds = bounds,
                    m_Position = float5,
                    m_MinDistance = num2,
                    m_Result = currentLane,
                    m_CarType = ((!isBicycle) ? RoadTypes.Car : RoadTypes.Bicycle),
                    m_AccessRestrictionOwner = accessRestrictionOwner,
                    m_SubLanes = m_Lanes,
                    m_AreaNodes = m_AreaNodes,
                    m_AreaTriangles = m_AreaTriangles,
                    m_CarLaneData = m_CarLaneData,
                    m_PedestrianLaneData = m_PedestrianLaneData,
                    m_MasterLaneData = m_MasterLaneData,
                    m_ConnectionLaneData = m_ConnectionLaneData,
                    m_CurveData = m_CurveData,
                    m_PrefabRefData = m_PrefabRefLookup,
                    m_PrefabCarLaneData = m_PrefabCarLaneData,
                };
                m_NetSearchTree.Iterate(ref iterator);
                m_StaticObjectSearchTree.Iterate(ref iterator);
                m_AreaSearchTree.Iterate(ref iterator);
                currentLane = iterator.m_Result;
            }

            private void TryFindCurrentLane(
                ref CarTrailerLane trailerLane,
                Game.Objects.Transform transform,
                Moving moving,
                bool isBicycle,
                Entity accessRestrictionOwner = default(Entity)
            )
            {
                float num = 4f / 15f;
                float3 float5 = transform.m_Position + moving.m_Velocity * (num * 2f);
                float num2 = 100f;
                Bounds3 bounds = new Bounds3(float5 - num2, float5 + num2);
                CarNavigationHelpers.FindLaneIterator iterator = new CarNavigationHelpers.FindLaneIterator
                {
                    m_Bounds = bounds,
                    m_Position = float5,
                    m_MinDistance = num2,
                    m_CarType = ((!isBicycle) ? RoadTypes.Car : RoadTypes.Bicycle),
                    m_AccessRestrictionOwner = accessRestrictionOwner,
                    m_SubLanes = m_Lanes,
                    m_AreaNodes = m_AreaNodes,
                    m_AreaTriangles = m_AreaTriangles,
                    m_CarLaneData = m_CarLaneData,
                    m_PedestrianLaneData = m_PedestrianLaneData,
                    m_MasterLaneData = m_MasterLaneData,
                    m_ConnectionLaneData = m_ConnectionLaneData,
                    m_CurveData = m_CurveData,
                    m_PrefabRefData = m_PrefabRefLookup,
                    m_PrefabCarLaneData = m_PrefabCarLaneData,
                };
                m_NetSearchTree.Iterate(ref iterator);
                m_StaticObjectSearchTree.Iterate(ref iterator);
                m_AreaSearchTree.Iterate(ref iterator);
                trailerLane.m_Lane = iterator.m_Result.m_Lane;
                trailerLane.m_CurvePosition = iterator.m_Result.m_CurvePosition.xy;
                trailerLane.m_NextLane = Entity.Null;
                trailerLane.m_NextPosition = default(float2);
            }

            private void FillNavigationPaths(
                ref Unity.Mathematics.Random random,
                int priority,
                Entity entity,
                Game.Objects.Transform transform,
                Target target,
                Car car,
                bool isBicycle,
                CarData prefabCarData,
                ref CarLaneSelectBuffer laneSelectBuffer,
                ref CarCurrentLane currentLane,
                ref Blocker blocker,
                ref PathOwner pathOwner,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<PathElement> pathElements,
                ref int invalidPath
            )
            {
                if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Waypoint)) == 0)
                {
                    for (int i = 0; i <= 12; i++)
                    {
                        if (i >= navigationLanes.Length)
                        {
                            if (i == 12)
                            {
                                if ((pathOwner.m_State & PathFlags.Pending) != 0)
                                {
                                    break;
                                }
                                int num = math.min(40000, pathElements.Length - pathOwner.m_ElementIndex);
                                if (num <= 0)
                                {
                                    break;
                                }
                                int num2 = random.NextInt(num) * (random.NextInt(num) + 1) / num;
                                PathElement pathElement = pathElements[pathOwner.m_ElementIndex + num2];
                                if (m_EntityStorageInfoLookup.Exists(pathElement.m_Target))
                                {
                                    break;
                                }
                                invalidPath = navigationLanes.Length;
                                return;
                            }
                            i = navigationLanes.Length;
                            if (pathOwner.m_ElementIndex >= pathElements.Length)
                            {
                                if ((pathOwner.m_State & PathFlags.Pending) != 0)
                                {
                                    break;
                                }
                                CarNavigationLane navLane = default(CarNavigationLane);
                                if (i > 0)
                                {
                                    CarNavigationLane value = navigationLanes[i - 1];
                                    if (
                                        (value.m_Flags & Game.Vehicles.CarLaneFlags.TransformTarget) == 0
                                        && (car.m_Flags & (CarFlags.StayOnRoad | CarFlags.AnyLaneTarget)) != (CarFlags.StayOnRoad | CarFlags.AnyLaneTarget)
                                        && GetTransformTarget(ref navLane.m_Lane, target)
                                    )
                                    {
                                        if ((value.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) == 0)
                                        {
                                            Entity lane = navLane.m_Lane;
                                            navLane.m_Lane = value.m_Lane;
                                            navLane.m_Flags = value.m_Flags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.Area);
                                            navLane.m_CurvePosition = value.m_CurvePosition.yy;
                                            float3 position = default(float3);
                                            if (
                                                VehicleUtils.CalculateTransformPosition(
                                                    ref position,
                                                    lane,
                                                    m_TransformData,
                                                    m_PositionData,
                                                    m_PrefabRefLookup,
                                                    m_PrefabBuildingData
                                                )
                                            )
                                            {
                                                UpdateSlaveLane(isBicycle, ref navLane, position);
                                            }
                                            if ((car.m_Flags & CarFlags.StayOnRoad) != 0)
                                            {
                                                navLane.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.GroupTarget;
                                                navigationLanes.Add(navLane);
                                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                                break;
                                            }
                                            navLane.m_Flags |= Game.Vehicles.CarLaneFlags.GroupTarget;
                                            navigationLanes.Add(navLane);
                                            currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                            continue;
                                        }
                                        navLane.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.TransformTarget;
                                        navigationLanes.Add(navLane);
                                        currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                        break;
                                    }
                                    value.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath;
                                    navigationLanes[i - 1] = value;
                                    currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                    break;
                                }
                                if (
                                    (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0
                                    || (car.m_Flags & CarFlags.StayOnRoad) != 0
                                    || !GetTransformTarget(ref navLane.m_Lane, target)
                                )
                                {
                                    currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndOfPath;
                                    break;
                                }
                                navLane.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.TransformTarget;
                                navigationLanes.Add(navLane);
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                break;
                            }
                            PathElement pathElement2 = pathElements[pathOwner.m_ElementIndex++];
                            CarNavigationLane navLane2 = new CarNavigationLane { m_Lane = pathElement2.m_Target, m_CurvePosition = pathElement2.m_TargetDelta };
                            if (m_CarLaneData.TryGetComponent(navLane2.m_Lane, out var componentData) || (isBicycle && m_PedestrianLaneData.HasComponent(navLane2.m_Lane)))
                            {
                                if (isBicycle && (componentData.m_Flags & Game.Net.CarLaneFlags.ForbidBicycles) != 0 && (pathElement2.m_Flags & PathElementFlags.PathStart) == 0)
                                {
                                    invalidPath = i;
                                    return;
                                }
                                if ((componentData.m_Flags & Game.Net.CarLaneFlags.Forward) == 0)
                                {
                                    bool flag =
                                        (componentData.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.GentleTurnLeft)) != 0;
                                    bool flag2 =
                                        (componentData.m_Flags & (Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnRight)) != 0;
                                    if (flag & !flag2)
                                    {
                                        navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnLeft;
                                    }
                                    if (flag2 & !flag)
                                    {
                                        navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnRight;
                                    }
                                }
                                if ((componentData.m_Flags & (Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout)) == Game.Net.CarLaneFlags.Roundabout)
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.Roundabout;
                                }
                                if ((componentData.m_Flags & Game.Net.CarLaneFlags.Twoway) != 0 && !isBicycle)
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.CanReverse;
                                }
                                if (
                                    (componentData.m_Flags & Game.Net.CarLaneFlags.Unsafe) != 0
                                    && (
                                        (componentData.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.UTurnRight)) != 0
                                        || (m_OwnerData.TryGetComponent(navLane2.m_Lane, out var componentData2) && m_CurveData.HasComponent(componentData2.m_Owner))
                                    )
                                )
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.RequestSpace;
                                }
                                navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                if (i == 0)
                                {
                                    if (
                                        (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0
                                        && m_ParkingLaneData.TryGetComponent(currentLane.m_Lane, out var componentData3)
                                    )
                                    {
                                        currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
                                        if ((componentData3.m_Flags & ParkingLaneFlags.ParkingRight) != 0)
                                        {
                                            currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.TurnLeft;
                                        }
                                        if ((componentData3.m_Flags & ParkingLaneFlags.ParkingLeft) != 0)
                                        {
                                            currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.TurnRight;
                                        }
                                    }
                                }
                                else
                                {
                                    CarNavigationLane value2 = navigationLanes[i - 1];
                                    if ((value2.m_Flags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0 && m_ParkingLaneData.TryGetComponent(value2.m_Lane, out var componentData4))
                                    {
                                        value2.m_Flags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
                                        if ((componentData4.m_Flags & ParkingLaneFlags.ParkingRight) != 0)
                                        {
                                            value2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnLeft;
                                        }
                                        if ((componentData4.m_Flags & ParkingLaneFlags.ParkingLeft) != 0)
                                        {
                                            value2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnRight;
                                        }
                                        navigationLanes[i - 1] = value2;
                                    }
                                }
                                if (
                                    i == 0
                                    && (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.FixedLane | Game.Vehicles.CarLaneFlags.Connection))
                                        == Game.Vehicles.CarLaneFlags.FixedLane
                                )
                                {
                                    GetSlaveLaneFromMasterLane(isBicycle, ref random, ref navLane2, currentLane);
                                }
                                else
                                {
                                    GetSlaveLaneFromMasterLane(isBicycle, ref random, ref navLane2);
                                }
                                if ((pathElement2.m_Flags & PathElementFlags.PathStart) != 0)
                                {
                                    Entity lane2;
                                    float prevCurvePos;
                                    if (i == 0)
                                    {
                                        lane2 = currentLane.m_Lane;
                                        prevCurvePos = currentLane.m_CurvePosition.z;
                                    }
                                    else
                                    {
                                        lane2 = navigationLanes[i - 1].m_Lane;
                                        prevCurvePos = navigationLanes[i - 1].m_CurvePosition.y;
                                    }
                                    if (IsContinuous(lane2, prevCurvePos, pathElement2.m_Target, pathElement2.m_TargetDelta.x, out var sameLane))
                                    {
                                        if (sameLane)
                                        {
                                            if (i == 0)
                                            {
                                                currentLane.m_CurvePosition.z = pathElement2.m_TargetDelta.y;
                                                continue;
                                            }
                                            CarNavigationLane value3 = navigationLanes[i - 1];
                                            value3.m_CurvePosition.y = pathElement2.m_TargetDelta.y;
                                            navigationLanes[i - 1] = value3;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.Interruption;
                                    }
                                }
                                navigationLanes.Add(navLane2);
                                continue;
                            }
                            if (m_ParkingLaneData.HasComponent(navLane2.m_Lane))
                            {
                                Game.Net.ParkingLane parkingLane = m_ParkingLaneData[navLane2.m_Lane];
                                navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
                                if ((parkingLane.m_Flags & ParkingLaneFlags.ParkingLeft) != 0)
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnLeft;
                                }
                                if ((parkingLane.m_Flags & ParkingLaneFlags.ParkingRight) != 0)
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnRight;
                                }
                                navigationLanes.Add(navLane2);
                                if (i > 0)
                                {
                                    float3 targetPosition = MathUtils.Position(m_CurveData[navLane2.m_Lane].m_Bezier, navLane2.m_CurvePosition.y);
                                    CarNavigationLane navLane3 = navigationLanes[i - 1];
                                    UpdateSlaveLane(isBicycle, ref navLane3, targetPosition);
                                    navigationLanes[i - 1] = navLane3;
                                }
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                break;
                            }
                            if (m_ConnectionLaneData.HasComponent(navLane2.m_Lane))
                            {
                                Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[navLane2.m_Lane];
                                navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
                                if ((connectionLane.m_Flags & ConnectionLaneFlags.Area) != 0)
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.Area;
                                }
                                else
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.Connection;
                                }
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
                                {
                                    navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.ParkingSpace;
                                    navigationLanes.Add(navLane2);
                                    break;
                                }
                                navigationLanes.Add(navLane2);
                                continue;
                            }
                            if (m_LaneData.HasComponent(navLane2.m_Lane))
                            {
                                if (pathOwner.m_ElementIndex >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0)
                                {
                                    pathOwner.m_ElementIndex--;
                                    break;
                                }
                                if (i > 0)
                                {
                                    float3 targetPosition2 = MathUtils.Position(m_CurveData[navLane2.m_Lane].m_Bezier, navLane2.m_CurvePosition.y);
                                    CarNavigationLane navLane4 = navigationLanes[i - 1];
                                    UpdateSlaveLane(isBicycle, ref navLane4, targetPosition2);
                                    navLane4.m_Flags |= Game.Vehicles.CarLaneFlags.Waypoint;
                                    if (pathOwner.m_ElementIndex >= pathElements.Length)
                                    {
                                        navLane4.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath;
                                    }
                                    navigationLanes[i - 1] = navLane4;
                                }
                                else
                                {
                                    currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Waypoint;
                                    if (pathOwner.m_ElementIndex >= pathElements.Length)
                                    {
                                        currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndOfPath;
                                    }
                                }
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                break;
                            }
                            if (!m_TransformData.HasComponent(navLane2.m_Lane))
                            {
                                invalidPath = i;
                                return;
                            }
                            if (pathOwner.m_ElementIndex >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0)
                            {
                                pathOwner.m_ElementIndex--;
                                break;
                            }
                            if ((car.m_Flags & CarFlags.StayOnRoad) == 0 || pathElements.Length > pathOwner.m_ElementIndex)
                            {
                                navLane2.m_Flags |= Game.Vehicles.CarLaneFlags.TransformTarget;
                                navigationLanes.Add(navLane2);
                                if (i > 0)
                                {
                                    float3 position2 = m_TransformData[navLane2.m_Lane].m_Position;
                                    CarNavigationLane navLane5 = navigationLanes[i - 1];
                                    UpdateSlaveLane(isBicycle, ref navLane5, position2);
                                    navigationLanes[i - 1] = navLane5;
                                }
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                            }
                        }
                        else
                        {
                            CarNavigationLane carNavigationLane = navigationLanes[i];
                            if (!m_EntityStorageInfoLookup.Exists(carNavigationLane.m_Lane))
                            {
                                invalidPath = i;
                                return;
                            }
                            if (
                                (carNavigationLane.m_Flags & (Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Waypoint))
                                != 0
                            )
                            {
                                break;
                            }
                        }
                    }
                }
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.UpdateOptimalLane) == 0)
                {
                    return;
                }
                currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IsBlocked) != 0)
                {
                    if (IsBlockedLane(currentLane.m_Lane, currentLane.m_CurvePosition.xz))
                    {
                        invalidPath = -1;
                        return;
                    }
                    for (int j = 0; j < navigationLanes.Length; j++)
                    {
                        CarNavigationLane carNavigationLane2 = navigationLanes[j];
                        if (IsBlockedLane(carNavigationLane2.m_Lane, carNavigationLane2.m_CurvePosition))
                        {
                            invalidPath = j;
                            return;
                        }
                    }
                    currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.FixedLane | Game.Vehicles.CarLaneFlags.IsBlocked);
                    currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.IgnoreBlocker;
                }
                PatchedCarLaneSelectIterator carLaneSelectIterator = new PatchedCarLaneSelectIterator
                {
                    m_OwnerData = m_OwnerData,
                    m_LaneData = m_LaneData,
                    m_CarLaneData = m_CarLaneData,
                    m_SlaveLaneData = m_SlaveLaneData,
                    m_LaneReservationData = m_LaneReservationData,
                    m_MovingData = m_MovingData,
                    m_ControllerData = m_ControllerData,
                    m_CarLaneRulesData = m_CarLaneRulesData,
                    m_Lanes = m_Lanes,
                    m_LaneObjects = m_LaneObjects,
                    m_Entity = entity,
                    m_Blocker = blocker.m_Blocker,
                    m_Priority = priority,
                    m_LeftHandTraffic = m_LeftHandTraffic,
                    m_TurnAhead = VehicleUtils.HasUpcomingTurn(navigationLanes),
                    m_ForbidLaneFlags = VehicleUtils.GetForbiddenLaneFlags(car, isBicycle),
                    m_PreferLaneFlags = VehicleUtils.GetPreferredLaneFlags(car),
                    m_PathMethods = (isBicycle ? PathMethod.Bicycle : PathMethod.Road),
                    m_CarLookup = m_CarLookup,
                    m_PrefabCarDataLookup = m_PrefabCarDataLookup,
                    m_PrefabRefLookup = m_PrefabRefLookup,
                    m_AmbulanceLookup = m_AmbulanceLookup,
                    m_DeliveryTruckLookup = m_DeliveryTruckLookup,
                    m_FireEngineLookup = m_FireEngineLookup,
                    m_GarbageTruckLookup = m_GarbageTruckLookup,
                    m_HearseLookup = m_HearseLookup,
                    m_MaintenanceVehicleLookup = m_MaintenanceVehicleLookup,
                    m_PersonalCarLookup = m_PersonalCarLookup,
                    m_PoliceCarLookup = m_PoliceCarLookup,
                    m_PostVanLookup = m_PostVanLookup,
                    m_PublicTransportLookup = m_PublicTransportLookup,
                    m_TaxiLookup = m_TaxiLookup,
                };
                carLaneSelectIterator.SetBuffer(ref laneSelectBuffer);
                if (navigationLanes.Length != 0)
                {
                    CarNavigationLane carNavigationLane3 = navigationLanes[navigationLanes.Length - 1];
                    carLaneSelectIterator.CalculateLaneCosts(carNavigationLane3, navigationLanes.Length - 1);
                    for (int num3 = navigationLanes.Length - 2; num3 >= 0; num3--)
                    {
                        CarNavigationLane carNavigationLane4 = navigationLanes[num3];
                        carLaneSelectIterator.CalculateLaneCosts(carNavigationLane4, carNavigationLane3, num3);
                        carNavigationLane3 = carNavigationLane4;
                    }
                    carLaneSelectIterator.UpdateOptimalLane(ref currentLane, navigationLanes[0]);
                    for (int k = 0; k < navigationLanes.Length; k++)
                    {
                        CarNavigationLane navLaneData = navigationLanes[k];
                        carLaneSelectIterator.UpdateOptimalLane(ref navLaneData);
                        navLaneData.m_Flags &= ~Game.Vehicles.CarLaneFlags.Reserved;
                        navigationLanes[k] = navLaneData;
                    }
                }
                else if (currentLane.m_CurvePosition.x != currentLane.m_CurvePosition.z)
                {
                    carLaneSelectIterator.UpdateOptimalLane(ref currentLane, default(CarNavigationLane));
                }
            }

            private bool IsContinuous(Entity prevLane, float prevCurvePos, Entity pathTarget, float nextCurvePos, out bool sameLane)
            {
                sameLane = false;
                if (m_SlaveLaneData.HasComponent(prevLane))
                {
                    SlaveLane slaveLane = m_SlaveLaneData[prevLane];
                    Entity owner = m_OwnerData[prevLane].m_Owner;
                    prevLane = m_Lanes[owner][slaveLane.m_MasterIndex].m_SubLane;
                    if (!m_MasterLaneData.HasComponent(prevLane))
                    {
                        return false;
                    }
                }
                if (prevLane == pathTarget && prevCurvePos == nextCurvePos)
                {
                    sameLane = true;
                    return true;
                }
                if (!m_LaneData.HasComponent(prevLane) || !m_LaneData.HasComponent(pathTarget))
                {
                    return false;
                }
                Lane lane = m_LaneData[prevLane];
                Lane lane2 = m_LaneData[pathTarget];
                return lane.m_EndNode.Equals(lane2.m_StartNode);
            }

            private bool IsBlockedLane(Entity lane, float2 range)
            {
                if (m_SlaveLaneData.HasComponent(lane))
                {
                    SlaveLane slaveLane = m_SlaveLaneData[lane];
                    Entity owner = m_OwnerData[lane].m_Owner;
                    lane = m_Lanes[owner][slaveLane.m_MasterIndex].m_SubLane;
                    if (!m_MasterLaneData.HasComponent(lane))
                    {
                        return false;
                    }
                }
                if (!m_CarLaneData.HasComponent(lane))
                {
                    return false;
                }
                Game.Net.CarLane carLane = m_CarLaneData[lane];
                if (carLane.m_BlockageEnd < carLane.m_BlockageStart)
                {
                    return false;
                }
                if (math.min(range.x, range.y) <= (float)(int)carLane.m_BlockageEnd * 0.003921569f)
                {
                    return math.max(range.x, range.y) >= (float)(int)carLane.m_BlockageStart * 0.003921569f;
                }
                return false;
            }

            private bool GetTransformTarget(ref Entity entity, Target target)
            {
                if (m_PropertyRenterData.HasComponent(target.m_Target))
                {
                    target.m_Target = m_PropertyRenterData[target.m_Target].m_Property;
                }
                if (m_TransformData.HasComponent(target.m_Target))
                {
                    entity = target.m_Target;
                    return true;
                }
                if (m_PositionData.HasComponent(target.m_Target))
                {
                    entity = target.m_Target;
                    return true;
                }
                return false;
            }

            private void UpdateSlaveLane(bool isBicycle, ref CarNavigationLane navLane, float3 targetPosition)
            {
                if (m_SlaveLaneData.TryGetComponent(navLane.m_Lane, out var componentData))
                {
                    Entity owner = m_OwnerData[navLane.m_Lane].m_Owner;
                    DynamicBuffer<Game.Net.SubLane> lanes = m_Lanes[owner];
                    PathMethod pathMethods = (isBicycle ? PathMethod.Bicycle : PathMethod.Road);
                    int index = NetUtils.ChooseClosestLane(
                        componentData.m_MinIndex,
                        componentData.m_MaxIndex,
                        targetPosition,
                        pathMethods,
                        lanes,
                        ref m_CurveData,
                        navLane.m_CurvePosition.y
                    );
                    navLane.m_Lane = lanes[index].m_SubLane;
                }
                navLane.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
            }

            private void GetSlaveLaneFromMasterLane(bool isBicycle, ref Unity.Mathematics.Random random, ref CarNavigationLane navLane, CarCurrentLane currentLane)
            {
                if (m_MasterLaneData.TryGetComponent(navLane.m_Lane, out var componentData))
                {
                    Owner owner = m_OwnerData[navLane.m_Lane];
                    DynamicBuffer<Game.Net.SubLane> lanes = m_Lanes[owner.m_Owner];
                    PathMethod pathMethods = (isBicycle ? PathMethod.Bicycle : PathMethod.Road);
                    if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
                    {
                        float3 position = default(float3);
                        if (VehicleUtils.CalculateTransformPosition(ref position, currentLane.m_Lane, m_TransformData, m_PositionData, m_PrefabRefLookup, m_PrefabBuildingData))
                        {
                            int index = NetUtils.ChooseClosestLane(
                                componentData.m_MinIndex,
                                componentData.m_MaxIndex,
                                position,
                                pathMethods,
                                lanes,
                                ref m_CurveData,
                                navLane.m_CurvePosition.y
                            );
                            navLane.m_Lane = lanes[index].m_SubLane;
                            navLane.m_Flags |= Game.Vehicles.CarLaneFlags.FixedStart;
                        }
                        else
                        {
                            navLane.m_Lane = GetRandomLane(isBicycle, ref random, componentData, lanes);
                        }
                    }
                    else
                    {
                        float3 comparePosition = MathUtils.Position(m_CurveData[currentLane.m_Lane].m_Bezier, currentLane.m_CurvePosition.z);
                        int index2 = NetUtils.ChooseClosestLane(
                            componentData.m_MinIndex,
                            componentData.m_MaxIndex,
                            comparePosition,
                            pathMethods,
                            lanes,
                            ref m_CurveData,
                            navLane.m_CurvePosition.x
                        );
                        navLane.m_Lane = lanes[index2].m_SubLane;
                        navLane.m_Flags |= Game.Vehicles.CarLaneFlags.FixedStart;
                    }
                }
                else
                {
                    navLane.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
                }
            }

            private void GetSlaveLaneFromMasterLane(bool isBicycle, ref Unity.Mathematics.Random random, ref CarNavigationLane navLane)
            {
                if (m_MasterLaneData.TryGetComponent(navLane.m_Lane, out var componentData))
                {
                    Entity owner = m_OwnerData[navLane.m_Lane].m_Owner;
                    DynamicBuffer<Game.Net.SubLane> lanes = m_Lanes[owner];
                    navLane.m_Lane = GetRandomLane(isBicycle, ref random, componentData, lanes);
                }
                else
                {
                    navLane.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
                }
            }

            private Entity GetRandomLane(bool isBicycle, ref Unity.Mathematics.Random random, MasterLane masterLane, DynamicBuffer<Game.Net.SubLane> lanes)
            {
                Entity result = Entity.Null;
                PathMethod pathMethod = (isBicycle ? PathMethod.Bicycle : PathMethod.Road);
                int num = 0;
                for (int i = masterLane.m_MinIndex; i <= masterLane.m_MaxIndex; i++)
                {
                    Game.Net.SubLane subLane = lanes[i];
                    if ((subLane.m_PathMethods & pathMethod) != 0 && random.NextInt(++num) == 0)
                    {
                        result = subLane.m_SubLane;
                    }
                }
                if (num == 0)
                {
                    int index = random.NextInt(masterLane.m_MinIndex, masterLane.m_MaxIndex + 1);
                    result = lanes[index].m_SubLane;
                }
                return result;
            }

            private bool GetNextLane(Entity prevLane, Entity nextLane, out Entity selectedLane)
            {
                if (m_SlaveLaneData.TryGetComponent(nextLane, out var componentData) && m_LaneData.TryGetComponent(prevLane, out var componentData2))
                {
                    Entity owner = m_OwnerData[nextLane].m_Owner;
                    DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[owner];
                    int num = math.min(componentData.m_MaxIndex, dynamicBuffer.Length - 1);
                    for (int i = componentData.m_MinIndex; i <= num; i++)
                    {
                        if (m_LaneData[dynamicBuffer[i].m_SubLane].m_StartNode.Equals(componentData2.m_EndNode))
                        {
                            selectedLane = dynamicBuffer[i].m_SubLane;
                            return true;
                        }
                    }
                }
                selectedLane = Entity.Null;
                return false;
            }

            private void CheckBlocker(ref CarCurrentLane currentLane, ref Blocker blocker, ref CarLaneSpeedIterator laneIterator)
            {
                if (laneIterator.m_Blocker != blocker.m_Blocker)
                {
                    currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.IgnoreBlocker | Game.Vehicles.CarLaneFlags.QueueReached);
                }
                if (laneIterator.m_Blocker != Entity.Null)
                {
                    if (!m_MovingData.HasComponent(laneIterator.m_Blocker))
                    {
                        if (m_CarLookup.HasComponent(laneIterator.m_Blocker))
                        {
                            if ((m_CarLookup[laneIterator.m_Blocker].m_Flags & CarFlags.Queueing) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Queue) != 0)
                            {
                                if (laneIterator.m_MaxSpeed <= 3f)
                                {
                                    currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.QueueReached;
                                }
                            }
                            else
                            {
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                                if (laneIterator.m_MaxSpeed <= 3f)
                                {
                                    currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.IsBlocked;
                                }
                            }
                        }
                        else
                        {
                            currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                            if (laneIterator.m_MaxSpeed <= 3f)
                            {
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.IsBlocked;
                            }
                        }
                    }
                    else if (laneIterator.m_Blocker != blocker.m_Blocker)
                    {
                        currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
                    }
                }
                blocker.m_Blocker = laneIterator.m_Blocker;
                blocker.m_Type = laneIterator.m_BlockerType;
                blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(laneIterator.m_MaxSpeed * 2.2949998f), 0, 255);
            }

            private void UpdateTrailer(
                Entity entity,
                Game.Objects.Transform transform,
                ObjectGeometryData prefabObjectGeometryData,
                Entity nextLane,
                float2 nextPosition,
                bool forceNext,
                ref CarTrailerLane trailerLane
            )
            {
                if (forceNext)
                {
                    trailerLane.m_Lane = nextLane;
                    trailerLane.m_CurvePosition = nextPosition;
                    trailerLane.m_NextLane = Entity.Null;
                    trailerLane.m_NextPosition = default(float2);
                    if (m_CurveData.HasComponent(nextLane))
                    {
                        MathUtils.Distance(m_CurveData[nextLane].m_Bezier, transform.m_Position, out trailerLane.m_CurvePosition.x);
                    }
                    return;
                }
                if (nextLane != Entity.Null)
                {
                    if (trailerLane.m_Lane == nextLane)
                    {
                        trailerLane.m_CurvePosition.y = nextPosition.y;
                        trailerLane.m_NextLane = Entity.Null;
                        trailerLane.m_NextPosition = default(float2);
                        nextLane = Entity.Null;
                        nextPosition = default(float2);
                    }
                    else if (trailerLane.m_NextLane == nextLane)
                    {
                        trailerLane.m_NextPosition.y = nextPosition.y;
                        nextLane = Entity.Null;
                        nextPosition = default(float2);
                    }
                    else if (trailerLane.m_NextLane == Entity.Null)
                    {
                        trailerLane.m_NextLane = nextLane;
                        trailerLane.m_NextPosition = nextPosition;
                        nextLane = Entity.Null;
                        nextPosition = default(float2);
                    }
                }
                float3 float5 = float.MaxValue;
                float3 float6 = default(float3);
                if (m_CurveData.HasComponent(trailerLane.m_Lane))
                {
                    float5.x = MathUtils.Distance(m_CurveData[trailerLane.m_Lane].m_Bezier, transform.m_Position, out float6.x);
                }
                if (m_CurveData.HasComponent(trailerLane.m_NextLane))
                {
                    float5.y = MathUtils.Distance(m_CurveData[trailerLane.m_NextLane].m_Bezier, transform.m_Position, out float6.y);
                }
                if (m_CurveData.HasComponent(nextLane))
                {
                    float5.z = MathUtils.Distance(m_CurveData[nextLane].m_Bezier, transform.m_Position, out float6.z);
                }
                if (math.all(float5.z < float5.xy) | forceNext)
                {
                    trailerLane.m_Lane = nextLane;
                    trailerLane.m_CurvePosition = new float2(float6.z, nextPosition.y);
                    trailerLane.m_NextLane = Entity.Null;
                    trailerLane.m_NextPosition = default(float2);
                }
                else if (float5.y < float5.x)
                {
                    trailerLane.m_Lane = trailerLane.m_NextLane;
                    trailerLane.m_CurvePosition = new float2(float6.y, trailerLane.m_NextPosition.y);
                    trailerLane.m_NextLane = nextLane;
                    trailerLane.m_NextPosition = nextPosition;
                }
                else
                {
                    trailerLane.m_CurvePosition.x = float6.x;
                }
            }

            private void UpdateNavigationTarget(
                ref Unity.Mathematics.Random random,
                int priority,
                Entity entity,
                Game.Objects.Transform transform,
                Moving moving,
                Car car,
                PseudoRandomSeed pseudoRandomSeed,
                PrefabRef prefabRef,
                CarData prefabCarData,
                ObjectGeometryData prefabObjectGeometryData,
                bool isBicycle,
                ref CarNavigation navigation,
                ref CarCurrentLane currentLane,
                ref Blocker blocker,
                ref Odometer odometer,
                ref PathOwner pathOwner,
                ref NativeList<Entity> tempBuffer,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<PathElement> pathElements
            )
            {
                float num = 4f / 15f;
                float num2 = math.length(moving.m_Velocity);
                float speedLimitFactor = VehicleUtils.GetSpeedLimitFactor(car);
                VehicleUtils.GetDrivingStyle(m_SimulationFrame, pseudoRandomSeed, isBicycle, out var safetyTime);
                PathFlags pathFlags = (
                    ((pathOwner.m_State & PathFlags.Append) != 0) ? PathFlags.Obsolete : (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)
                );
                bool flag = (pathOwner.m_State & pathFlags) != 0;
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
                {
                    prefabCarData.m_MaxSpeed = 277.77777f;
                    prefabCarData.m_Acceleration = 277.77777f;
                    prefabCarData.m_Braking = 277.77777f;
                }
                else
                {
                    num2 = math.min(num2, prefabCarData.m_MaxSpeed);
                    prefabCarData.m_Acceleration = math.select(prefabCarData.m_Acceleration, 0f, flag);
                }
                Bounds1 speedRange = (
                    ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) == 0)
                        ? VehicleUtils.CalculateSpeedRange(prefabCarData, num2, num)
                        : new Bounds1(0f, prefabCarData.m_MaxSpeed)
                );
                bool flag2 = blocker.m_Type == BlockerType.Temporary;
                bool flag3 = math.asuint(navigation.m_MaxSpeed) >> 31 != 0;
                CarLaneSpeedIterator laneIterator = new CarLaneSpeedIterator
                {
                    m_TransformData = m_TransformData,
                    m_MovingData = m_MovingData,
                    m_CarData = m_CarLookup,
                    m_BicycleData = m_BicycleData,
                    m_TrainData = m_TrainData,
                    m_ControllerData = m_ControllerData,
                    m_LaneReservationData = m_LaneReservationData,
                    m_LaneConditionData = m_LaneConditionData,
                    m_LaneSignalData = m_LaneSignalData,
                    m_CurveData = m_CurveData,
                    m_CarLaneData = m_CarLaneData,
                    m_PedestrianLaneData = m_PedestrianLaneData,
                    m_ParkingLaneData = m_ParkingLaneData,
                    m_UnspawnedData = m_UnspawnedData,
                    m_CreatureData = m_CreatureData,
                    m_PrefabRefData = m_PrefabRefLookup,
                    m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
                    m_PrefabCarData = m_PrefabCarDataLookup,
                    m_PrefabTrainData = m_PrefabTrainData,
                    m_PrefabParkingLaneData = m_PrefabParkingLaneData,
                    m_LaneOverlapData = m_LaneOverlaps,
                    m_LaneObjectData = m_LaneObjects,
                    m_Entity = entity,
                    m_Ignore = (((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IgnoreBlocker) != 0) ? blocker.m_Blocker : Entity.Null),
                    m_TempBuffer = tempBuffer,
                    m_Priority = priority,
                    m_TimeStep = num,
                    m_SafeTimeStep = num + safetyTime,
                    m_DistanceOffset = math.select(0f, math.max(-0.5f, -0.5f * math.lengthsq(1.5f - num2)), num2 < 1.5f),
                    m_SpeedLimitFactor = speedLimitFactor,
                    m_CurrentSpeed = num2,
                    m_PrefabCar = prefabCarData,
                    m_PrefabObjectGeometry = prefabObjectGeometryData,
                    m_SpeedRange = speedRange,
                    m_PushBlockers = ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.PushBlockers) != 0),
                    m_IsBicycle = isBicycle,
                    m_MaxSpeed = speedRange.max,
                    m_CanChangeLane = 1f,
                    m_CurrentPosition = transform.m_Position,
                };
                Game.Vehicles.CarLaneFlags carLaneFlags = (Game.Vehicles.CarLaneFlags)0u;
                float4 falseValue = math.select(new float4(-0.5f, 0.5f, 0.002f, 0.1f), new float4(-0.5f, 0.5f, 0.02f, 0.2f), isBicycle);
                float4 trueValue = math.select(new float4(0f, 0f, 0.01f, 0.1f), new float4(0.25f, -0.25f, 0.1f, 0.2f), isBicycle);
                float lanePosition = currentLane.m_LanePosition;
                if (currentLane.m_ChangeLane != Entity.Null)
                {
                    float2 x = math.select(
                        falseValue.xy,
                        trueValue.xy,
                        new bool2(
                            (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight)) == Game.Vehicles.CarLaneFlags.TurnRight,
                            (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight)) == Game.Vehicles.CarLaneFlags.TurnLeft
                        )
                    );
                    lanePosition = math.clamp(currentLane.m_LanePosition, math.cmin(x), math.cmax(x));
                    lanePosition = 0f - lanePosition;
                }
                float num3 = 11.111112f;
                float num4;
                Game.Net.CarLaneFlags laneFlags;
                if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.ParkingSpace)) != 0)
                {
                    if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0 && ((currentLane.m_CurvePosition.x != currentLane.m_CurvePosition.z) | flag))
                    {
                        navigation.m_TargetPosition = transform.m_Position;
                    }
                    laneIterator.IterateParkingTarget(currentLane.m_Lane, currentLane.m_CurvePosition.xz);
                    laneIterator.IterateTarget(navigation.m_TargetPosition);
                    navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
                    CheckBlocker(ref currentLane, ref blocker, ref laneIterator);
                }
                else
                {
                    if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) == 0)
                    {
                        if (currentLane.m_Lane == Entity.Null)
                        {
                            navigation.m_MaxSpeed = math.max(0f, num2 - prefabCarData.m_Braking * num);
                            blocker.m_Blocker = Entity.Null;
                            blocker.m_Type = BlockerType.None;
                            blocker.m_MaxSpeed = byte.MaxValue;
                            return;
                        }
                        PrefabRef prefabRef2 = m_PrefabRefLookup[currentLane.m_Lane];
                        NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef2.m_Prefab];
                        m_NodeLaneData.TryGetComponent(currentLane.m_Lane, out var componentData);
                        float laneOffset = VehicleUtils.GetLaneOffset(
                            prefabObjectGeometryData,
                            prefabLaneData,
                            componentData,
                            currentLane.m_CurvePosition.x,
                            currentLane.m_LanePosition,
                            isBicycle
                        );
                        num4 = laneOffset;
                        if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.HighBeams) != 0)
                        {
                            if (!m_CarLaneData.HasComponent(currentLane.m_Lane) || !AllowHighBeams(transform, blocker, ref currentLane, navigationLanes, 100f, 2f))
                            {
                                currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.HighBeams;
                            }
                        }
                        else if (
                            m_CarLaneData.HasComponent(currentLane.m_Lane)
                            && (m_CarLaneData[currentLane.m_Lane].m_Flags & Game.Net.CarLaneFlags.Highway) != 0
                            && !IsLit(currentLane.m_Lane)
                            && AllowHighBeams(transform, blocker, ref currentLane, navigationLanes, 150f, 0f)
                        )
                        {
                            currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.HighBeams;
                        }
                        Entity nextLane = Entity.Null;
                        float2 nextOffset = 0f;
                        if (navigationLanes.Length > 0)
                        {
                            CarNavigationLane carNavigationLane = navigationLanes[0];
                            nextLane = carNavigationLane.m_Lane;
                            nextOffset = carNavigationLane.m_CurvePosition;
                        }
                        if (currentLane.m_ChangeLane != Entity.Null)
                        {
                            PrefabRef prefabRef3 = m_PrefabRefLookup[currentLane.m_ChangeLane];
                            NetLaneData prefabLaneData2 = m_PrefabLaneData[prefabRef3.m_Prefab];
                            m_NodeLaneData.TryGetComponent(currentLane.m_ChangeLane, out var componentData2);
                            num4 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData2, componentData2, currentLane.m_CurvePosition.x, lanePosition, isBicycle);
                            if (
                                !laneIterator.IterateFirstLane(
                                    currentLane.m_Lane,
                                    currentLane.m_ChangeLane,
                                    currentLane.m_CurvePosition,
                                    nextLane,
                                    nextOffset,
                                    currentLane.m_ChangeProgress,
                                    laneOffset,
                                    num4,
                                    (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0,
                                    out laneFlags
                                )
                            )
                            {
                                goto IL_07e9;
                            }
                        }
                        else if (
                            !laneIterator.IterateFirstLane(
                                currentLane.m_Lane,
                                currentLane.m_CurvePosition,
                                nextLane,
                                nextOffset,
                                laneOffset,
                                (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0,
                                out laneFlags
                            )
                        )
                        {
                            goto IL_07e9;
                        }
                        goto IL_09cd;
                    }
                    if (
                        isBicycle
                        && m_ConnectionLaneData.TryGetComponent(currentLane.m_Lane, out var componentData3)
                        && (componentData3.m_Flags & ConnectionLaneFlags.Pedestrian) != 0
                    )
                    {
                        num3 = 5.555556f;
                    }
                    navigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, navigation.m_TargetPosition);
                    laneIterator.IterateTarget(navigation.m_TargetPosition, num3);
                    navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
                    blocker.m_Blocker = Entity.Null;
                    blocker.m_Type = BlockerType.None;
                    blocker.m_MaxSpeed = byte.MaxValue;
                }
                goto IL_0a0a;
                IL_09cd:
                navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
                CheckBlocker(ref currentLane, ref blocker, ref laneIterator);
                if (laneIterator.m_TempBuffer.IsCreated)
                {
                    tempBuffer = laneIterator.m_TempBuffer;
                    tempBuffer.Clear();
                }
                goto IL_0a0a;
                IL_0a0a:
                float num5 = math.select(prefabCarData.m_PivotOffset, 0f - prefabCarData.m_PivotOffset, flag3);
                float3 position = transform.m_Position;
                if (num5 < 0f)
                {
                    position += math.rotate(transform.m_Rotation, new float3(0f, 0f, num5));
                    num5 = 0f - num5;
                }
                float num6 = math.lerp(math.distance(position, navigation.m_TargetPosition), 0f, laneIterator.m_Oncoming);
                float num7 = math.max(1f, navigation.m_MaxSpeed * num) + num5;
                float num8 = num7;
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
                {
                    float brakingDistance = VehicleUtils.GetBrakingDistance(prefabCarData, math.min(prefabCarData.m_MaxSpeed, num3), num);
                    num8 = math.max(num7, brakingDistance + 1f + num5);
                    num6 = math.select(num6, 0f, currentLane.m_ChangeProgress != 0f);
                }
                if (currentLane.m_ChangeLane != Entity.Null)
                {
                    float num9 = 0.05f;
                    float num10 = 1f + prefabObjectGeometryData.m_Bounds.max.z * num9;
                    float2 x2 = math.select(new float2(0.4f, 0.6f), new float2(0.6f, 1f), isBicycle);
                    x2.y *= math.saturate(num2 * num9);
                    x2 *= laneIterator.m_CanChangeLane * num;
                    x2.x = math.min(x2.x, math.max(0f, 1f - currentLane.m_ChangeProgress));
                    currentLane.m_ChangeProgress = math.min(num10, currentLane.m_ChangeProgress + math.csum(x2));
                    if (currentLane.m_ChangeProgress == num10 || (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) != 0)
                    {
                        ApplySideEffects(ref currentLane, speedLimitFactor, isBicycle, prefabRef, prefabCarData);
                        currentLane.m_LanePosition = lanePosition;
                        currentLane.m_Lane = currentLane.m_ChangeLane;
                        currentLane.m_ChangeLane = Entity.Null;
                        currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
                    }
                }
                if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight)) == 0)
                {
                    currentLane.m_LaneFlags |= carLaneFlags;
                }
                bool num11 = blocker.m_Type == BlockerType.Temporary;
                if (num11 != flag2 || currentLane.m_Duration >= 30f)
                {
                    ApplySideEffects(ref currentLane, speedLimitFactor, isBicycle, prefabRef, prefabCarData);
                }
                if (num11)
                {
                    if (currentLane.m_Duration >= 5f)
                    {
                        currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.PushBlockers;
                    }
                }
                else if (currentLane.m_Duration >= 5f)
                {
                    currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.PushBlockers;
                }
                currentLane.m_Duration += num;
                if (num2 > 0.01f)
                {
                    float num12 = num2 * num;
                    currentLane.m_Distance += num12;
                    odometer.m_Distance += num12;
                    carLaneFlags = currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
                    float4 float5 = math.select(
                        falseValue,
                        trueValue,
                        new bool4(
                            carLaneFlags == Game.Vehicles.CarLaneFlags.TurnRight,
                            carLaneFlags == Game.Vehicles.CarLaneFlags.TurnLeft,
                            carLaneFlags != (Game.Vehicles.CarLaneFlags)0u,
                            w: true
                        )
                    );
                    float5.zw = math.min(1f, num12 * float5.zw);
                    if (isBicycle && laneIterator.m_LaneOffsetPush.y != 0f)
                    {
                        PrefabRef prefabRef4 = m_PrefabRefLookup[currentLane.m_Lane];
                        if (m_PrefabLaneData.TryGetComponent(prefabRef4.m_Prefab, out var componentData4))
                        {
                            float num13 = laneIterator.m_LaneOffsetPush.x / (laneIterator.m_LaneOffsetPush.y * math.max(1f, componentData4.m_Width));
                            float num14 = math.clamp(currentLane.m_LanePosition + num13 * 10f, -0.5f, 0.5f);
                            float5.x = math.clamp(float5.x, num14 - 0.25f, num14);
                            float5.y = math.clamp(float5.y, num14, num14 + 0.25f);
                            float5.z = math.min(float5.z + math.abs(num13) * 0.5f, 1f);
                        }
                    }
                    currentLane.m_LanePosition -= (math.max(0f, currentLane.m_LanePosition - 0.5f) + math.min(0f, currentLane.m_LanePosition + 0.5f)) * float5.w;
                    currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, random.NextFloat(float5.x, float5.y), float5.z);
                }
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
                {
                    if (currentLane.m_Distance > 10f + num2 * 0.5f)
                    {
                        currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.ResetSpeed;
                        currentLane.m_Distance = 0f;
                        currentLane.m_Duration = 0f;
                    }
                    else if (currentLane.m_Duration > 60f)
                    {
                        blocker.m_Blocker = entity;
                        blocker.m_Type = BlockerType.Spawn;
                    }
                }
                if (num6 < num8)
                {
                    while (true)
                    {
                        if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0)
                        {
                            currentLane.m_CurvePosition.xy = currentLane.m_CurvePosition.z;
                            if (flag)
                            {
                                navigation.m_TargetRotation = default(quaternion);
                                break;
                            }
                            Curve curve = m_CurveData[currentLane.m_Lane];
                            if (m_ParkingLaneData.HasComponent(currentLane.m_Lane))
                            {
                                Game.Net.ParkingLane parkingLane = m_ParkingLaneData[currentLane.m_Lane];
                                PrefabRef prefabRef5 = m_PrefabRefLookup[currentLane.m_Lane];
                                ParkingLaneData parkingLaneData = m_PrefabParkingLaneData[prefabRef5.m_Prefab];
                                Game.Objects.Transform ownerTransform = default(Game.Objects.Transform);
                                if (m_OwnerData.TryGetComponent(currentLane.m_Lane, out var componentData5) && m_TransformData.HasComponent(componentData5.m_Owner))
                                {
                                    ownerTransform = m_TransformData[componentData5.m_Owner];
                                }
                                Game.Objects.Transform transform2 = VehicleUtils.CalculateParkingSpaceTarget(
                                    parkingLane,
                                    parkingLaneData,
                                    prefabObjectGeometryData,
                                    curve,
                                    ownerTransform,
                                    currentLane.m_CurvePosition.x
                                );
                                navigation.m_TargetPosition = transform2.m_Position;
                                navigation.m_TargetRotation = transform2.m_Rotation;
                            }
                            else
                            {
                                Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[currentLane.m_Lane];
                                navigation.m_TargetPosition = VehicleUtils.GetConnectionParkingPosition(connectionLane, curve.m_Bezier, currentLane.m_CurvePosition.x);
                                navigation.m_TargetRotation = quaternion.LookRotationSafe(MathUtils.Tangent(curve.m_Bezier, currentLane.m_CurvePosition.x), math.up());
                            }
                            num6 = math.distance(position, navigation.m_TargetPosition);
                            if (num6 >= 1f + num5)
                            {
                                navigation.m_TargetRotation = default(quaternion);
                            }
                        }
                        else if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
                        {
                            bool flag4 = false;
                            if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
                            {
                                quaternion targetRotation = CalculateNavigationRotation(currentLane.m_Lane, navigationLanes);
                                flag4 = !targetRotation.Equals(navigation.m_TargetRotation);
                                navigation.m_TargetRotation = targetRotation;
                            }
                            else
                            {
                                navigation.m_TargetRotation = default(quaternion);
                            }
                            if (MoveTarget(position, ref navigation.m_TargetPosition, num7, currentLane.m_Lane) | flag4)
                            {
                                break;
                            }
                        }
                        else if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
                        {
                            navigation.m_TargetRotation = default(quaternion);
                            currentLane.m_LanePosition = math.clamp(currentLane.m_LanePosition, -0.5f, 0.5f);
                            float navigationSize = VehicleUtils.GetNavigationSize(prefabObjectGeometryData);
                            bool num15 = MoveAreaTarget(
                                ref random,
                                transform.m_Position,
                                pathOwner,
                                navigationLanes,
                                pathElements,
                                ref navigation.m_TargetPosition,
                                num8,
                                currentLane.m_Lane,
                                ref currentLane.m_CurvePosition,
                                currentLane.m_LanePosition,
                                navigationSize
                            );
                            navigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, navigation.m_TargetPosition);
                            currentLane.m_ChangeProgress = 0f;
                            if (num15)
                            {
                                break;
                            }
                        }
                        else
                        {
                            navigation.m_TargetRotation = default(quaternion);
                            if (currentLane.m_ChangeLane != Entity.Null)
                            {
                                Curve curve2 = m_CurveData[currentLane.m_Lane];
                                Curve curve3 = m_CurveData[currentLane.m_ChangeLane];
                                PrefabRef prefabRef6 = m_PrefabRefLookup[currentLane.m_Lane];
                                PrefabRef prefabRef7 = m_PrefabRefLookup[currentLane.m_ChangeLane];
                                NetLaneData prefabLaneData3 = m_PrefabLaneData[prefabRef6.m_Prefab];
                                NetLaneData prefabLaneData4 = m_PrefabLaneData[prefabRef7.m_Prefab];
                                m_NodeLaneData.TryGetComponent(currentLane.m_Lane, out var componentData6);
                                m_NodeLaneData.TryGetComponent(currentLane.m_ChangeLane, out var componentData7);
                                float2 x3 = math.select(
                                    falseValue.xy,
                                    trueValue.xy,
                                    new bool2(
                                        (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight))
                                            == Game.Vehicles.CarLaneFlags.TurnRight,
                                        (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight))
                                            == Game.Vehicles.CarLaneFlags.TurnLeft
                                    )
                                );
                                lanePosition = math.clamp(currentLane.m_LanePosition, math.cmin(x3), math.cmax(x3));
                                lanePosition = 0f - lanePosition;
                                if (
                                    MoveTarget(
                                        position,
                                        ref navigation.m_TargetPosition,
                                        num7,
                                        curve2.m_Bezier,
                                        curve3.m_Bezier,
                                        currentLane.m_ChangeProgress,
                                        ref currentLane.m_CurvePosition,
                                        prefabObjectGeometryData,
                                        prefabLaneData3,
                                        prefabLaneData4,
                                        componentData6,
                                        componentData7,
                                        currentLane.m_LanePosition,
                                        lanePosition,
                                        isBicycle
                                    )
                                )
                                {
                                    if ((prefabLaneData3.m_Flags & LaneFlags.Twoway) == 0)
                                    {
                                        currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.CanReverse;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                Curve curve4 = m_CurveData[currentLane.m_Lane];
                                PrefabRef prefabRef8 = m_PrefabRefLookup[currentLane.m_Lane];
                                NetLaneData prefabLaneData5 = m_PrefabLaneData[prefabRef8.m_Prefab];
                                m_NodeLaneData.TryGetComponent(currentLane.m_Lane, out var componentData8);
                                float laneOffset2 = VehicleUtils.GetLaneOffset(
                                    prefabObjectGeometryData,
                                    prefabLaneData5,
                                    componentData8,
                                    currentLane.m_CurvePosition.x,
                                    currentLane.m_LanePosition,
                                    isBicycle
                                );
                                if (laneIterator.m_Oncoming != 0f)
                                {
                                    float num16 =
                                        prefabLaneData5.m_Width + math.lerp(componentData8.m_WidthOffset.x, componentData8.m_WidthOffset.y, currentLane.m_CurvePosition.x);
                                    float num17 = prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x;
                                    float num18 = math.lerp(laneOffset2, num17 * math.select(0.5f, -0.5f, m_LeftHandTraffic), math.min(1f, laneIterator.m_Oncoming));
                                    laneOffset2 = math.select(laneOffset2, num18, (!m_LeftHandTraffic & (num18 > laneOffset2)) | (m_LeftHandTraffic & (num18 < laneOffset2)));
                                    currentLane.m_LanePosition = laneOffset2 / math.max(0.1f, num16 - num17);
                                }
                                float lanePosition2 = math.select(
                                    currentLane.m_LanePosition,
                                    0f - currentLane.m_LanePosition,
                                    currentLane.m_CurvePosition.z < currentLane.m_CurvePosition.x
                                );
                                if (
                                    MoveTarget(
                                        position,
                                        ref navigation.m_TargetPosition,
                                        num7,
                                        curve4.m_Bezier,
                                        ref currentLane.m_CurvePosition,
                                        prefabObjectGeometryData,
                                        prefabLaneData5,
                                        componentData8,
                                        lanePosition2,
                                        isBicycle
                                    )
                                )
                                {
                                    if ((prefabLaneData5.m_Flags & LaneFlags.Twoway) == 0)
                                    {
                                        currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.CanReverse;
                                    }
                                    break;
                                }
                            }
                        }
                        if (navigationLanes.Length == 0)
                        {
                            num6 = math.distance(position, navigation.m_TargetPosition);
                            if (num6 < 1f + num5 && num2 < 0.1f)
                            {
                                currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndReached;
                            }
                            break;
                        }
                        CarNavigationLane carNavigationLane2 = navigationLanes[0];
                        if (
                            (carNavigationLane2.m_Flags & (Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Validated))
                                == Game.Vehicles.CarLaneFlags.ParkingSpace
                            || !m_EntityStorageInfoLookup.Exists(carNavigationLane2.m_Lane)
                        )
                        {
                            break;
                        }
                        if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
                        {
                            if ((carNavigationLane2.m_Flags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
                            {
                                carNavigationLane2.m_Flags |= Game.Vehicles.CarLaneFlags.ResetSpeed;
                            }
                            else if ((carNavigationLane2.m_Flags & Game.Vehicles.CarLaneFlags.Connection) == 0)
                            {
                                num6 = math.distance(position, navigation.m_TargetPosition);
                                if (num6 >= 1f + num5 || num2 > 3f)
                                {
                                    break;
                                }
                                carNavigationLane2.m_Flags |= Game.Vehicles.CarLaneFlags.ResetSpeed;
                            }
                        }
                        if (
                            (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.HighBeams) != 0
                            && m_CarLaneData.TryGetComponent(carNavigationLane2.m_Lane, out var componentData9)
                            && (componentData9.m_Flags & Game.Net.CarLaneFlags.Highway) != 0
                            && !IsLit(carNavigationLane2.m_Lane)
                        )
                        {
                            carNavigationLane2.m_Flags |= Game.Vehicles.CarLaneFlags.HighBeams;
                        }
                        ApplySideEffects(ref currentLane, speedLimitFactor, isBicycle, prefabRef, prefabCarData);
                        if (
                            currentLane.m_ChangeLane != Entity.Null
                            && GetNextLane(currentLane.m_Lane, carNavigationLane2.m_Lane, out var selectedLane)
                            && selectedLane != carNavigationLane2.m_Lane
                        )
                        {
                            currentLane.m_Lane = selectedLane;
                            currentLane.m_ChangeLane = carNavigationLane2.m_Lane;
                        }
                        else
                        {
                            currentLane.m_Lane = carNavigationLane2.m_Lane;
                            currentLane.m_ChangeLane = Entity.Null;
                            currentLane.m_ChangeProgress = 0f;
                        }
                        currentLane.m_CurvePosition = carNavigationLane2.m_CurvePosition.xxy;
                        currentLane.m_LaneFlags = carNavigationLane2.m_Flags | (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.PushBlockers);
                        navigationLanes.RemoveAt(0);
                    }
                }
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
                {
                    VehicleCollisionIterator vehicleCollisionIterator = new VehicleCollisionIterator
                    {
                        m_OwnerData = m_OwnerData,
                        m_TransformData = m_TransformData,
                        m_MovingData = m_MovingData,
                        m_ControllerData = m_ControllerData,
                        m_CreatureData = m_CreatureData,
                        m_CurveData = m_CurveData,
                        m_AreaLaneData = m_AreaLaneData,
                        m_PrefabRefData = m_PrefabRefLookup,
                        m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
                        m_PrefabLaneData = m_PrefabLaneData,
                        m_AreaNodes = m_AreaNodes,
                        m_StaticObjectSearchTree = m_StaticObjectSearchTree,
                        m_MovingObjectSearchTree = m_MovingObjectSearchTree,
                        m_TerrainHeightData = m_TerrainHeightData,
                        m_Entity = entity,
                        m_CurrentLane = currentLane.m_Lane,
                        m_CurvePosition = currentLane.m_CurvePosition.z,
                        m_TimeStep = num,
                        m_PrefabObjectGeometry = prefabObjectGeometryData,
                        m_SpeedRange = speedRange,
                        m_CurrentPosition = transform.m_Position,
                        m_CurrentVelocity = moving.m_Velocity,
                        m_MinDistance = num8,
                        m_TargetPosition = navigation.m_TargetPosition,
                        m_MaxSpeed = navigation.m_MaxSpeed,
                        m_LanePosition = currentLane.m_LanePosition,
                        m_Blocker = blocker.m_Blocker,
                        m_BlockerType = blocker.m_Type,
                    };
                    if (vehicleCollisionIterator.m_MaxSpeed != 0f && !flag3)
                    {
                        vehicleCollisionIterator.IterateFirstLane(currentLane.m_Lane);
                        vehicleCollisionIterator.m_MaxSpeed = math.select(vehicleCollisionIterator.m_MaxSpeed, 0f, vehicleCollisionIterator.m_MaxSpeed < 0.1f);
                        if (!navigation.m_TargetPosition.Equals(vehicleCollisionIterator.m_TargetPosition))
                        {
                            navigation.m_TargetPosition = vehicleCollisionIterator.m_TargetPosition;
                            currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, vehicleCollisionIterator.m_LanePosition, 0.1f);
                            currentLane.m_ChangeProgress = 1f;
                        }
                        navigation.m_MaxSpeed = vehicleCollisionIterator.m_MaxSpeed;
                        blocker.m_Blocker = vehicleCollisionIterator.m_Blocker;
                        blocker.m_Type = vehicleCollisionIterator.m_BlockerType;
                        blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(vehicleCollisionIterator.m_MaxSpeed * 2.2949998f), 0, 255);
                    }
                    navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, math.distance(transform.m_Position.xz, navigation.m_TargetPosition.xz) / num);
                }
                else
                {
                    navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, math.distance(transform.m_Position, navigation.m_TargetPosition) / num);
                }
                if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) != 0)
                {
                    return;
                }
                float3 float6 = navigation.m_TargetPosition - position;
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
                {
                    float6.xz = MathUtils.ClampLength(float6.xz, num7);
                    float6.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, position + float6) - position.y;
                }
                num6 = math.length(float6);
                float3 x4 = math.forward(transform.m_Rotation);
                if (flag3)
                {
                    if (num6 < 1f + num5 || math.dot(x4, math.normalizesafe(float6)) < math.select(0.8f, 0.1f, isBicycle))
                    {
                        navigation.m_MaxSpeed = 0f - math.min(3f, navigation.m_MaxSpeed);
                    }
                    else if (num2 >= 0.1f)
                    {
                        navigation.m_MaxSpeed = 0f - math.max(0f, math.min(navigation.m_MaxSpeed, num2 - prefabCarData.m_Braking * num));
                    }
                }
                else
                {
                    if (
                        !(num6 >= 1f + num5)
                        || (!(currentLane.m_ChangeLane == Entity.Null) && !(currentLane.m_ChangeProgress <= 0f))
                        || !(math.dot(x4, math.normalizesafe(float6)) < math.select(0.7f, 0f, isBicycle))
                    )
                    {
                        return;
                    }
                    if (num2 >= 0.1f)
                    {
                        navigation.m_MaxSpeed = math.max(0f, math.min(navigation.m_MaxSpeed, num2 - prefabCarData.m_Braking * num));
                        return;
                    }
                    navigation.m_MaxSpeed = 0f - math.min(3f, navigation.m_MaxSpeed);
                    if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) == 0 && !isBicycle)
                    {
                        currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.CanReverse;
                    }
                }
                return;
                IL_07e9:
                int num19 = 0;
                while (true)
                {
                    if (num19 < navigationLanes.Length)
                    {
                        CarNavigationLane carNavigationLane3 = navigationLanes[num19];
                        if ((carNavigationLane3.m_Flags & (Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.Area)) == 0)
                        {
                            if ((carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.Connection) != 0)
                            {
                                laneIterator.m_PrefabCar.m_MaxSpeed = 277.77777f;
                                laneIterator.m_PrefabCar.m_Acceleration = 277.77777f;
                                laneIterator.m_PrefabCar.m_Braking = 277.77777f;
                                laneIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
                            }
                            else
                            {
                                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
                                {
                                    goto IL_09bf;
                                }
                                if ((carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.Interruption) != 0)
                                {
                                    laneIterator.m_PrefabCar.m_MaxSpeed = 3f;
                                }
                            }
                            if (
                                (num19 == 0 || (carNavigationLane3.m_Flags & (Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Roundabout)) == 0)
                                && carLaneFlags == (Game.Vehicles.CarLaneFlags)0u
                                && (carNavigationLane3.m_Flags & (Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Validated))
                                    != Game.Vehicles.CarLaneFlags.ParkingSpace
                            )
                            {
                                carLaneFlags |= carNavigationLane3.m_Flags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
                            }
                            bool test = (carNavigationLane3.m_Lane == currentLane.m_Lane) | (carNavigationLane3.m_Lane == currentLane.m_ChangeLane);
                            float falseValue2 = math.select(-1f, 2f, carNavigationLane3.m_CurvePosition.y < carNavigationLane3.m_CurvePosition.x);
                            falseValue2 = math.select(falseValue2, currentLane.m_CurvePosition.y, test);
                            bool num20 = laneIterator.IterateNextLane(
                                carNavigationLane3.m_Lane,
                                carNavigationLane3.m_CurvePosition,
                                num4,
                                falseValue2,
                                navigationLanes.AsNativeArray().GetSubArray(num19 + 1, navigationLanes.Length - 1 - num19),
                                (carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0,
                                ref laneFlags,
                                out var needSignal
                            );
                            if (needSignal)
                            {
                                m_LaneSignals.Enqueue(new CarNavigationHelpers.LaneSignal(entity, carNavigationLane3.m_Lane, priority));
                            }
                            if (num20)
                            {
                                break;
                            }
                            num19++;
                            continue;
                        }
                    }
                    goto IL_09bf;
                    IL_09bf:
                    laneIterator.IterateTarget(laneIterator.m_CurrentPosition);
                    break;
                }
                goto IL_09cd;
            }

            private quaternion CalculateNavigationRotation(Entity sourceLocation, DynamicBuffer<CarNavigationLane> navigationLanes)
            {
                float3 float5 = default(float3);
                bool flag = false;
                if (m_TransformData.TryGetComponent(sourceLocation, out var componentData))
                {
                    float5 = componentData.m_Position;
                    flag = true;
                }
                for (int i = 0; i < navigationLanes.Length; i++)
                {
                    CarNavigationLane carNavigationLane = navigationLanes[i];
                    if (m_TransformData.TryGetComponent(carNavigationLane.m_Lane, out componentData))
                    {
                        if (flag)
                        {
                            float3 value = componentData.m_Position - float5;
                            if (MathUtils.TryNormalize(ref value))
                            {
                                return quaternion.LookRotationSafe(value, math.up());
                            }
                        }
                        else
                        {
                            float5 = componentData.m_Position;
                            flag = true;
                        }
                    }
                    else
                    {
                        if (!m_CurveData.TryGetComponent(carNavigationLane.m_Lane, out var componentData2))
                        {
                            continue;
                        }
                        float3 float6 = MathUtils.Position(componentData2.m_Bezier, carNavigationLane.m_CurvePosition.x);
                        if (flag)
                        {
                            float3 value2 = float6 - float5;
                            if (MathUtils.TryNormalize(ref value2))
                            {
                                return quaternion.LookRotationSafe(value2, math.up());
                            }
                        }
                        else
                        {
                            float5 = float6;
                            flag = true;
                        }
                        if (carNavigationLane.m_CurvePosition.x != carNavigationLane.m_CurvePosition.y)
                        {
                            float3 float7 = MathUtils.Tangent(componentData2.m_Bezier, carNavigationLane.m_CurvePosition.x);
                            float7 = math.select(float7, -float7, carNavigationLane.m_CurvePosition.y < carNavigationLane.m_CurvePosition.x);
                            if (MathUtils.TryNormalize(ref float7))
                            {
                                return quaternion.LookRotationSafe(float7, math.up());
                            }
                        }
                    }
                }
                return default(quaternion);
            }

            private bool IsLit(Entity lane)
            {
                if (m_OwnerData.TryGetComponent(lane, out var componentData) && m_RoadData.TryGetComponent(componentData.m_Owner, out var componentData2))
                {
                    return (componentData2.m_Flags & (Game.Net.RoadFlags.IsLit | Game.Net.RoadFlags.LightsOff)) == Game.Net.RoadFlags.IsLit;
                }
                return false;
            }

            private float CalculateNoise(ref CarCurrentLane currentLaneData, PrefabRef prefabRefData, CarData prefabCarData)
            {
                if (m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab) && m_CarLaneData.HasComponent(currentLaneData.m_Lane))
                {
                    VehicleSideEffectData vehicleSideEffectData = m_PrefabSideEffectData[prefabRefData.m_Prefab];
                    Game.Net.CarLane carLaneData = m_CarLaneData[currentLaneData.m_Lane];
                    float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabCarData, carLaneData);
                    float num = math.select(currentLaneData.m_Distance / currentLaneData.m_Duration, maxDriveSpeed, currentLaneData.m_Duration == 0f) / prefabCarData.m_MaxSpeed;
                    num = math.saturate(num * num);
                    return math.lerp(vehicleSideEffectData.m_Min.z, vehicleSideEffectData.m_Max.z, num) * currentLaneData.m_Duration;
                }
                return 0f;
            }

            private void ApplySideEffects(ref CarCurrentLane currentLane, float speedLimitFactor, bool isBicycle, PrefabRef prefabRefData, CarData prefabCarData)
            {
                if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
                {
                    return;
                }
                if (m_CarLaneData.HasComponent(currentLane.m_Lane) && (currentLane.m_Duration != 0f || currentLane.m_Distance != 0f))
                {
                    Game.Net.CarLane carLaneData = m_CarLaneData[currentLane.m_Lane];
                    Curve curve = m_CurveData[currentLane.m_Lane];
                    carLaneData.m_SpeedLimit *= speedLimitFactor;
                    float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabCarData, carLaneData);
                    float num = 1f / math.max(1f, curve.m_Length);
                    float3 sideEffects = default(float3);
                    if (m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab))
                    {
                        VehicleSideEffectData vehicleSideEffectData = m_PrefabSideEffectData[prefabRefData.m_Prefab];
                        float num2 = math.select(currentLane.m_Distance / currentLane.m_Duration, maxDriveSpeed, currentLane.m_Duration == 0f) / prefabCarData.m_MaxSpeed;
                        num2 = math.saturate(num2 * num2);
                        sideEffects = math.lerp(vehicleSideEffectData.m_Min, vehicleSideEffectData.m_Max, num2);
                        float x = math.min(1f, currentLane.m_Distance * num);
                        sideEffects *= new float3(x, currentLane.m_Duration, currentLane.m_Duration);
                    }
                    maxDriveSpeed = math.min(prefabCarData.m_MaxSpeed, carLaneData.m_SpeedLimit);
                    float2 float5 = new float2(currentLane.m_Duration * maxDriveSpeed, currentLane.m_Distance) * num;
                    float5 = math.select(float5, -float5, isBicycle);
                    m_LaneEffects.Enqueue(new CarNavigationHelpers.LaneEffects(currentLane.m_Lane, sideEffects, float5));
                }
                currentLane.m_Duration = 0f;
                currentLane.m_Distance = 0f;
            }

            private bool AllowHighBeams(
                Game.Objects.Transform transform,
                Blocker blocker,
                ref CarCurrentLane currentLaneData,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                float maxDistance,
                float minOffset
            )
            {
                if (blocker.m_Blocker != Entity.Null && m_TransformData.TryGetComponent(blocker.m_Blocker, out var componentData))
                {
                    float3 float5 = componentData.m_Position - transform.m_Position;
                    if (
                        math.lengthsq(float5) < maxDistance * maxDistance
                        && math.dot(math.forward(transform.m_Rotation), float5) > minOffset
                        && m_VehicleData.HasComponent(blocker.m_Blocker)
                    )
                    {
                        return false;
                    }
                }
                float num = maxDistance - m_CurveData[currentLaneData.m_Lane].m_Length * math.abs(currentLaneData.m_CurvePosition.z - currentLaneData.m_CurvePosition.x);
                Entity entity = Entity.Null;
                if (m_OwnerData.TryGetComponent(currentLaneData.m_Lane, out var componentData2) && entity != componentData2.m_Owner)
                {
                    if (!AllowHighBeams(transform, componentData2.m_Owner, maxDistance, minOffset))
                    {
                        return false;
                    }
                    entity = componentData2.m_Owner;
                }
                for (int i = 0; i < navigationLanes.Length; i++)
                {
                    if (!(num > 0f))
                    {
                        break;
                    }
                    CarNavigationLane carNavigationLane = navigationLanes[i];
                    if (!m_CarLaneData.HasComponent(carNavigationLane.m_Lane))
                    {
                        break;
                    }
                    if (m_OwnerData.TryGetComponent(carNavigationLane.m_Lane, out componentData2) && entity != componentData2.m_Owner)
                    {
                        if (!AllowHighBeams(transform, componentData2.m_Owner, maxDistance, minOffset))
                        {
                            return false;
                        }
                        entity = componentData2.m_Owner;
                    }
                    num -= m_CurveData[carNavigationLane.m_Lane].m_Length * math.abs(carNavigationLane.m_CurvePosition.y - carNavigationLane.m_CurvePosition.x);
                }
                return true;
            }

            private bool AllowHighBeams(Game.Objects.Transform transform, Entity owner, float maxDistance, float minOffset)
            {
                if (m_Lanes.TryGetBuffer(owner, out var bufferData))
                {
                    float3 x = math.forward(transform.m_Rotation);
                    maxDistance *= maxDistance;
                    for (int i = 0; i < bufferData.Length; i++)
                    {
                        Game.Net.SubLane subLane = bufferData[i];
                        if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Track)) == 0 || !m_LaneObjects.TryGetBuffer(subLane.m_SubLane, out var bufferData2))
                        {
                            continue;
                        }
                        for (int j = 0; j < bufferData2.Length; j++)
                        {
                            LaneObject laneObject = bufferData2[j];
                            if (m_TransformData.TryGetComponent(laneObject.m_LaneObject, out var componentData))
                            {
                                float3 float5 = componentData.m_Position - transform.m_Position;
                                if (math.lengthsq(float5) < maxDistance && math.dot(x, float5) > minOffset && m_VehicleData.HasComponent(laneObject.m_LaneObject))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }

            private void ReserveNavigationLanes(
                ref Unity.Mathematics.Random random,
                int priority,
                Entity entity,
                CarData prefabCarData,
                ObjectGeometryData prefabObjectGeometryData,
                Car carData,
                ref CarNavigation navigationData,
                ref CarCurrentLane currentLaneData,
                DynamicBuffer<CarNavigationLane> navigationLanes
            )
            {
                float timeStep = 4f / 15f;
                if (!m_CarLaneData.HasComponent(currentLaneData.m_Lane))
                {
                    return;
                }
                Curve curve = m_CurveData[currentLaneData.m_Lane];
                bool flag = currentLaneData.m_CurvePosition.z < currentLaneData.m_CurvePosition.x;
                float num = math.max(0f, VehicleUtils.GetBrakingDistance(prefabCarData, math.abs(navigationData.m_MaxSpeed), timeStep) - 0.01f);
                float num2 = num;
                float num3 = num2 / math.max(1E-06f, curve.m_Length) + 1E-06f;
                currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.x + math.select(num3, 0f - num3, flag);
                num2 -= curve.m_Length * math.abs(currentLaneData.m_CurvePosition.z - currentLaneData.m_CurvePosition.x);
                int i = 0;
                if ((carData.m_Flags & CarFlags.Emergency) != 0 && num > 1f)
                {
                    if (currentLaneData.m_ChangeLane != Entity.Null)
                    {
                        ReserveOtherLanesInGroup(currentLaneData.m_ChangeLane, 102);
                    }
                    else
                    {
                        ReserveOtherLanesInGroup(currentLaneData.m_Lane, 102);
                    }
                }
                if ((currentLaneData.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0 && m_LaneReservationData.HasComponent(currentLaneData.m_Lane))
                {
                    m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(currentLaneData.m_Lane, 0f, 96));
                }
                if (navigationLanes.Length > 0)
                {
                    CarNavigationLane carNavigationLane = navigationLanes[0];
                    if ((carNavigationLane.m_Flags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0 && m_LaneReservationData.HasComponent(carNavigationLane.m_Lane))
                    {
                        m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(carNavigationLane.m_Lane, 0f, 96));
                    }
                }
                bool2 bool5 = currentLaneData.m_CurvePosition.yz > currentLaneData.m_CurvePosition.zy;
                if (flag ? bool5.y : bool5.x)
                {
                    currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.z;
                    while (i < navigationLanes.Length && num2 > 0f)
                    {
                        CarNavigationLane value = navigationLanes[i];
                        if (!m_CarLaneData.HasComponent(value.m_Lane))
                        {
                            break;
                        }
                        curve = m_CurveData[value.m_Lane];
                        if (m_LaneReservationData.HasComponent(value.m_Lane))
                        {
                            num3 = num2 / math.max(1E-06f, curve.m_Length);
                            num3 = math.max(value.m_CurvePosition.x, math.min(value.m_CurvePosition.y, value.m_CurvePosition.x + num3));
                            m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(value.m_Lane, num3, priority));
                        }
                        if ((carData.m_Flags & CarFlags.Emergency) != 0)
                        {
                            ReserveOtherLanesInGroup(value.m_Lane, 102);
                            if (m_LaneSignalData.HasComponent(value.m_Lane))
                            {
                                m_LaneSignals.Enqueue(new CarNavigationHelpers.LaneSignal(entity, value.m_Lane, priority));
                            }
                        }
                        num2 -= curve.m_Length * math.abs(value.m_CurvePosition.y - value.m_CurvePosition.x);
                        value.m_Flags |= Game.Vehicles.CarLaneFlags.Reserved;
                        navigationLanes[i++] = value;
                    }
                }
                if ((carData.m_Flags & CarFlags.Emergency) != 0)
                {
                    num2 += num;
                    if (random.NextInt(4) != 0)
                    {
                        num2 += prefabObjectGeometryData.m_Bounds.max.z + 1f;
                    }
                    for (; i < navigationLanes.Length; i++)
                    {
                        if (!(num2 > 0f))
                        {
                            break;
                        }
                        CarNavigationLane carNavigationLane2 = navigationLanes[i];
                        if (m_CarLaneData.HasComponent(carNavigationLane2.m_Lane))
                        {
                            curve = m_CurveData[carNavigationLane2.m_Lane];
                            bool flag2 = true;
                            if (m_LaneSignalData.TryGetComponent(carNavigationLane2.m_Lane, out var componentData))
                            {
                                flag2 = (componentData.m_Flags & LaneSignalFlags.Physical) == 0 || componentData.m_Signal == LaneSignalType.Go;
                                m_LaneSignals.Enqueue(new CarNavigationHelpers.LaneSignal(entity, carNavigationLane2.m_Lane, priority));
                            }
                            if (flag2 && m_LaneReservationData.HasComponent(carNavigationLane2.m_Lane))
                            {
                                m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(carNavigationLane2.m_Lane, 0f, priority));
                            }
                            num2 -= curve.m_Length * math.abs(carNavigationLane2.m_CurvePosition.y - carNavigationLane2.m_CurvePosition.x);
                            continue;
                        }
                        break;
                    }
                }
                else
                {
                    if ((currentLaneData.m_LaneFlags & Game.Vehicles.CarLaneFlags.Roundabout) == 0)
                    {
                        return;
                    }
                    num2 += num * 0.5f;
                    if (random.NextInt(2) != 0)
                    {
                        num2 += prefabObjectGeometryData.m_Bounds.max.z + 1f;
                    }
                    for (; i < navigationLanes.Length; i++)
                    {
                        if (!(num2 > 0f))
                        {
                            break;
                        }
                        CarNavigationLane carNavigationLane3 = navigationLanes[i];
                        if (m_CarLaneData.HasComponent(carNavigationLane3.m_Lane) && (carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.Roundabout) != 0)
                        {
                            curve = m_CurveData[carNavigationLane3.m_Lane];
                            if (m_LaneReservationData.HasComponent(carNavigationLane3.m_Lane))
                            {
                                m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(carNavigationLane3.m_Lane, 0f, priority));
                            }
                            num2 -= curve.m_Length * math.abs(carNavigationLane3.m_CurvePosition.y - carNavigationLane3.m_CurvePosition.x);
                            continue;
                        }
                        break;
                    }
                }
            }

            private void ReserveOtherLanesInGroup(Entity lane, int priority)
            {
                if (!m_SlaveLaneData.HasComponent(lane))
                {
                    return;
                }
                SlaveLane slaveLane = m_SlaveLaneData[lane];
                Owner owner = m_OwnerData[lane];
                DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
                int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
                for (int i = slaveLane.m_MinIndex; i <= num; i++)
                {
                    Entity subLane = dynamicBuffer[i].m_SubLane;
                    if (subLane != lane && m_LaneReservationData.HasComponent(subLane))
                    {
                        m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(subLane, 0f, priority));
                    }
                }
            }

            private bool MoveAreaTarget(
                ref Unity.Mathematics.Random random,
                float3 comparePosition,
                PathOwner pathOwner,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<PathElement> pathElements,
                ref float3 targetPosition,
                float minDistance,
                Entity target,
                ref float3 curveDelta,
                float lanePosition,
                float navigationSize
            )
            {
                if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete | PathFlags.Updated)) != 0)
                {
                    return true;
                }
                Entity owner = m_OwnerData[target].m_Owner;
                AreaLane areaLane = m_AreaLaneData[target];
                DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[owner];
                int num = math.min(pathOwner.m_ElementIndex, pathElements.Length);
                NativeArray<PathElement> subArray = pathElements.AsNativeArray().GetSubArray(num, pathElements.Length - num);
                num = 0;
                bool flag = curveDelta.z < curveDelta.x;
                float lanePosition2 = math.select(lanePosition, 0f - lanePosition, flag);
                if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
                {
                    float3 position = nodes[areaLane.m_Nodes.x].m_Position;
                    float3 position2 = nodes[areaLane.m_Nodes.y].m_Position;
                    float3 position3 = nodes[areaLane.m_Nodes.w].m_Position;
                    if (
                        VehicleUtils.SetTriangleTarget(
                            position,
                            position2,
                            position3,
                            comparePosition,
                            num,
                            navigationLanes,
                            subArray,
                            ref targetPosition,
                            minDistance,
                            lanePosition2,
                            curveDelta.z,
                            navigationSize,
                            isSingle: true,
                            m_TransformData,
                            m_AreaLaneData,
                            m_CurveData
                        )
                    )
                    {
                        return true;
                    }
                    curveDelta.y = curveDelta.z;
                }
                else
                {
                    bool4 bool5 = new bool4(curveDelta.yz < 0.5f, curveDelta.yz > 0.5f);
                    int2 int5 = math.select(areaLane.m_Nodes.x, areaLane.m_Nodes.w, bool5.zw);
                    float3 position4 = nodes[int5.x].m_Position;
                    float3 position5 = nodes[areaLane.m_Nodes.y].m_Position;
                    float3 position6 = nodes[areaLane.m_Nodes.z].m_Position;
                    float3 position7 = nodes[int5.y].m_Position;
                    if (math.any(bool5.xy & bool5.wz))
                    {
                        if (
                            VehicleUtils.SetAreaTarget(
                                position4,
                                position4,
                                position5,
                                position6,
                                position7,
                                owner,
                                nodes,
                                comparePosition,
                                num,
                                navigationLanes,
                                subArray,
                                ref targetPosition,
                                minDistance,
                                lanePosition2,
                                curveDelta.z,
                                navigationSize,
                                flag,
                                m_TransformData,
                                m_AreaLaneData,
                                m_CurveData,
                                m_OwnerData
                            )
                        )
                        {
                            return true;
                        }
                        curveDelta.y = 0.5f;
                        bool5.xz = false;
                    }
                    if (
                        VehicleUtils.GetPathElement(num, navigationLanes, subArray, out var pathElement)
                        && m_OwnerData.TryGetComponent(pathElement.m_Target, out var componentData)
                        && componentData.m_Owner == owner
                    )
                    {
                        bool4 bool6 = new bool4(pathElement.m_TargetDelta < 0.5f, pathElement.m_TargetDelta > 0.5f);
                        if (math.any(!bool5.xz) & math.any(bool5.yw) & math.any(bool6.xy & bool6.wz))
                        {
                            AreaLane areaLane2 = m_AreaLaneData[pathElement.m_Target];
                            int5 = math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, bool6.zw);
                            position4 = nodes[int5.x].m_Position;
                            float3 prev = math.select(position5, position6, position4.Equals(position5));
                            position5 = nodes[areaLane2.m_Nodes.y].m_Position;
                            position6 = nodes[areaLane2.m_Nodes.z].m_Position;
                            position7 = nodes[int5.y].m_Position;
                            bool flag2 = pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x;
                            if (
                                VehicleUtils.SetAreaTarget(
                                    lanePosition: math.select(lanePosition, 0f - lanePosition, flag2),
                                    prev2: prev,
                                    prev: position4,
                                    left: position5,
                                    right: position6,
                                    next: position7,
                                    areaEntity: owner,
                                    nodes: nodes,
                                    comparePosition: comparePosition,
                                    elementIndex: num + 1,
                                    navigationLanes: navigationLanes,
                                    pathElements: subArray,
                                    targetPosition: ref targetPosition,
                                    minDistance: minDistance,
                                    curveDelta: pathElement.m_TargetDelta.y,
                                    navigationSize: navigationSize,
                                    isBackward: flag2,
                                    transforms: m_TransformData,
                                    areaLanes: m_AreaLaneData,
                                    curves: m_CurveData,
                                    owners: m_OwnerData
                                )
                            )
                            {
                                return true;
                            }
                        }
                        curveDelta.y = curveDelta.z;
                        return false;
                    }
                    if (
                        VehicleUtils.SetTriangleTarget(
                            position5,
                            position6,
                            position7,
                            comparePosition,
                            num,
                            navigationLanes,
                            subArray,
                            ref targetPosition,
                            minDistance,
                            lanePosition2,
                            curveDelta.z,
                            navigationSize,
                            isSingle: false,
                            m_TransformData,
                            m_AreaLaneData,
                            m_CurveData
                        )
                    )
                    {
                        return true;
                    }
                    curveDelta.y = curveDelta.z;
                }
                return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
            }

            private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Entity target)
            {
                if (VehicleUtils.CalculateTransformPosition(ref targetPosition, target, m_TransformData, m_PositionData, m_PrefabRefLookup, m_PrefabBuildingData))
                {
                    return math.distance(comparePosition, targetPosition) >= minDistance;
                }
                return false;
            }

            private bool MoveTarget(
                float3 comparePosition,
                ref float3 targetPosition,
                float minDistance,
                Bezier4x3 curve,
                ref float3 curveDelta,
                ObjectGeometryData prefabObjectGeometryData,
                NetLaneData prefabLaneData,
                NodeLane nodeLane,
                float lanePosition,
                bool isBicycle
            )
            {
                float laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, nodeLane, curveDelta.z, lanePosition, isBicycle);
                float3 lanePosition2 = VehicleUtils.GetLanePosition(curve, curveDelta.z, laneOffset);
                if (math.distance(comparePosition, lanePosition2) < minDistance)
                {
                    curveDelta.x = curveDelta.z;
                    targetPosition = lanePosition2;
                    return false;
                }
                float2 xz = curveDelta.xz;
                for (int i = 0; i < 8; i++)
                {
                    float num = math.lerp(xz.x, xz.y, 0.5f);
                    laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, nodeLane, num, lanePosition, isBicycle);
                    float3 lanePosition3 = VehicleUtils.GetLanePosition(curve, num, laneOffset);
                    if (math.distance(comparePosition, lanePosition3) < minDistance)
                    {
                        xz.x = num;
                    }
                    else
                    {
                        xz.y = num;
                    }
                }
                curveDelta.x = xz.y;
                laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, nodeLane, xz.y, lanePosition, isBicycle);
                targetPosition = VehicleUtils.GetLanePosition(curve, xz.y, laneOffset);
                return true;
            }

            private bool MoveTarget(
                float3 comparePosition,
                ref float3 targetPosition,
                float minDistance,
                Bezier4x3 curve1,
                Bezier4x3 curve2,
                float curveSelect,
                ref float3 curveDelta,
                ObjectGeometryData prefabObjectGeometryData,
                NetLaneData prefabLaneData1,
                NetLaneData prefabLaneData2,
                NodeLane nodeLane1,
                NodeLane nodeLane2,
                float lanePosition1,
                float lanePosition2,
                bool isBicycle
            )
            {
                curveSelect = math.saturate(curveSelect);
                float laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData1, nodeLane1, curveDelta.z, lanePosition1, isBicycle);
                float laneOffset2 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData2, nodeLane2, curveDelta.z, lanePosition2, isBicycle);
                float3 lanePosition3 = VehicleUtils.GetLanePosition(curve1, curveDelta.z, laneOffset);
                float3 lanePosition4 = VehicleUtils.GetLanePosition(curve2, curveDelta.z, laneOffset2);
                if (MathUtils.Distance(new Line3.Segment(lanePosition3, lanePosition4), comparePosition, out var t) < minDistance)
                {
                    curveDelta.x = curveDelta.z;
                    targetPosition = math.lerp(lanePosition3, lanePosition4, curveSelect);
                    return false;
                }
                float2 xz = curveDelta.xz;
                for (int i = 0; i < 8; i++)
                {
                    float num = math.lerp(xz.x, xz.y, 0.5f);
                    laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData1, nodeLane1, num, lanePosition1, isBicycle);
                    laneOffset2 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData2, nodeLane2, num, lanePosition2, isBicycle);
                    float3 lanePosition5 = VehicleUtils.GetLanePosition(curve1, num, laneOffset);
                    float3 lanePosition6 = VehicleUtils.GetLanePosition(curve2, num, laneOffset2);
                    if (MathUtils.Distance(new Line3.Segment(lanePosition5, lanePosition6), comparePosition, out t) < minDistance)
                    {
                        xz.x = num;
                    }
                    else
                    {
                        xz.y = num;
                    }
                }
                curveDelta.x = xz.y;
                laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData1, nodeLane1, xz.y, lanePosition1, isBicycle);
                laneOffset2 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData2, nodeLane2, xz.y, lanePosition2, isBicycle);
                float3 lanePosition7 = VehicleUtils.GetLanePosition(curve1, xz.y, laneOffset);
                float3 lanePosition8 = VehicleUtils.GetLanePosition(curve2, xz.y, laneOffset2);
                targetPosition = math.lerp(lanePosition7, lanePosition8, curveSelect);
                return true;
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private SimulationSystem m_SimulationSystem;

        private TerrainSystem m_TerrainSystem;

        private Game.Net.SearchSystem m_NetSearchSystem;

        private Game.Areas.SearchSystem m_AreaSearchSystem;

        private Game.Objects.SearchSystem m_ObjectSearchSystem;

        private CityConfigurationSystem m_CityConfigurationSystem;

        private CarNavigationSystem.Actions m_Actions;

        private EntityQuery m_VehicleQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
            m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
            m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
            m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            m_Actions = base.World.GetOrCreateSystemManaged<CarNavigationSystem.Actions>();
            m_VehicleQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<UpdateFrame>(),
                ComponentType.ReadWrite<CarCurrentLane>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<TripSource>(),
                ComponentType.Exclude<ParkedCar>()
            );
        }

        [Preserve]
        protected override void OnUpdate()
        {
            uint index = m_SimulationSystem.frameIndex % 16;
            m_VehicleQuery.ResetFilter();
            m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
            m_Actions.m_LaneReservationQueue = new NativeQueue<CarNavigationHelpers.LaneReservation>(Allocator.TempJob);
            m_Actions.m_LaneEffectsQueue = new NativeQueue<CarNavigationHelpers.LaneEffects>(Allocator.TempJob);
            m_Actions.m_LaneSignalQueue = new NativeQueue<CarNavigationHelpers.LaneSignal>(Allocator.TempJob);
            m_Actions.m_TrafficAmbienceQueue = new NativeQueue<CarNavigationSystem.TrafficAmbienceEffect>(Allocator.TempJob);
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(
                new UpdateNavigationJob
                {
                    m_EntityType = SystemAPI.GetEntityTypeHandle(),
                    m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(true),
                    m_MovingType = SystemAPI.GetComponentTypeHandle<Game.Objects.Moving>(true),
                    m_TargetType = SystemAPI.GetComponentTypeHandle<Game.Common.Target>(true),
                    m_CarType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.Car>(true),
                    m_BicycleType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.Bicycle>(true),
                    m_OutOfControlType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.OutOfControl>(true),
                    m_PseudoRandomSeedType = SystemAPI.GetComponentTypeHandle<Game.Common.PseudoRandomSeed>(true),
                    m_PrefabRefType = SystemAPI.GetComponentTypeHandle<Game.Prefabs.PrefabRef>(true),
                    m_LayoutElementType = SystemAPI.GetBufferTypeHandle<Game.Vehicles.LayoutElement>(true),
                    m_NavigationType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.CarNavigation>(false),
                    m_CurrentLaneType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.CarCurrentLane>(false),
                    m_PathOwnerType = SystemAPI.GetComponentTypeHandle<Game.Pathfind.PathOwner>(false),
                    m_BlockerType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.Blocker>(false),
                    m_OdometerType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.Odometer>(false),
                    m_NavigationLaneType = SystemAPI.GetBufferTypeHandle<Game.Vehicles.CarNavigationLane>(false),
                    m_PathElementType = SystemAPI.GetBufferTypeHandle<Game.Pathfind.PathElement>(false),
                    m_EntityStorageInfoLookup = SystemAPI.GetEntityStorageInfoLookup(),
                    m_OwnerData = SystemAPI.GetComponentLookup<Game.Common.Owner>(true),
                    m_UnspawnedData = SystemAPI.GetComponentLookup<Game.Objects.Unspawned>(true),
                    m_LaneData = SystemAPI.GetComponentLookup<Game.Net.Lane>(true),
                    m_CarLaneData = SystemAPI.GetComponentLookup<Game.Net.CarLane>(true),
                    m_PedestrianLaneData = SystemAPI.GetComponentLookup<Game.Net.PedestrianLane>(true),
                    m_ParkingLaneData = SystemAPI.GetComponentLookup<Game.Net.ParkingLane>(true),
                    m_ConnectionLaneData = SystemAPI.GetComponentLookup<Game.Net.ConnectionLane>(true),
                    m_MasterLaneData = SystemAPI.GetComponentLookup<Game.Net.MasterLane>(true),
                    m_SlaveLaneData = SystemAPI.GetComponentLookup<Game.Net.SlaveLane>(true),
                    m_AreaLaneData = SystemAPI.GetComponentLookup<Game.Net.AreaLane>(true),
                    m_CurveData = SystemAPI.GetComponentLookup<Game.Net.Curve>(true),
                    m_NodeLaneData = SystemAPI.GetComponentLookup<Game.Net.NodeLane>(true),
                    m_LaneReservationData = SystemAPI.GetComponentLookup<Game.Net.LaneReservation>(true),
                    m_LaneConditionData = SystemAPI.GetComponentLookup<Game.Net.LaneCondition>(true),
                    m_LaneSignalData = SystemAPI.GetComponentLookup<Game.Net.LaneSignal>(true),
                    m_RoadData = SystemAPI.GetComponentLookup<Game.Net.Road>(true),
                    m_PropertyRenterData = SystemAPI.GetComponentLookup<Game.Buildings.PropertyRenter>(true),
                    m_TransformData = SystemAPI.GetComponentLookup<Game.Objects.Transform>(true),
                    m_PositionData = SystemAPI.GetComponentLookup<Game.Routes.Position>(true),
                    m_MovingData = SystemAPI.GetComponentLookup<Game.Objects.Moving>(true),
                    m_BicycleData = SystemAPI.GetComponentLookup<Game.Vehicles.Bicycle>(true),
                    m_TrainData = SystemAPI.GetComponentLookup<Game.Vehicles.Train>(true),
                    m_ControllerData = SystemAPI.GetComponentLookup<Game.Vehicles.Controller>(true),
                    m_CarLaneRulesData = SystemAPI.GetComponentLookup<LaneRules>(isReadOnly: true),
                    m_VehicleData = SystemAPI.GetComponentLookup<Game.Vehicles.Vehicle>(true),
                    m_CreatureData = SystemAPI.GetComponentLookup<Game.Creatures.Creature>(true),
                    m_PrefabTrainData = SystemAPI.GetComponentLookup<Game.Prefabs.TrainData>(true),
                    m_PrefabBuildingData = SystemAPI.GetComponentLookup<Game.Prefabs.BuildingData>(true),
                    m_PrefabObjectGeometryData = SystemAPI.GetComponentLookup<Game.Prefabs.ObjectGeometryData>(true),
                    m_PrefabSideEffectData = SystemAPI.GetComponentLookup<Game.Prefabs.VehicleSideEffectData>(true),
                    m_PrefabLaneData = SystemAPI.GetComponentLookup<Game.Prefabs.NetLaneData>(true),
                    m_PrefabCarLaneData = SystemAPI.GetComponentLookup<Game.Prefabs.CarLaneData>(true),
                    m_PrefabParkingLaneData = SystemAPI.GetComponentLookup<Game.Prefabs.ParkingLaneData>(true),
                    m_Lanes = SystemAPI.GetBufferLookup<Game.Net.SubLane>(true),
                    m_LaneObjects = SystemAPI.GetBufferLookup<Game.Net.LaneObject>(true),
                    m_LaneOverlaps = SystemAPI.GetBufferLookup<Game.Net.LaneOverlap>(true),
                    m_AreaNodes = SystemAPI.GetBufferLookup<Game.Areas.Node>(true),
                    m_AreaTriangles = SystemAPI.GetBufferLookup<Game.Areas.Triangle>(true),
                    m_TrailerLaneData = SystemAPI.GetComponentLookup<Game.Vehicles.CarTrailerLane>(false),
                    m_BlockedLanes = SystemAPI.GetBufferLookup<Game.Objects.BlockedLane>(false),
                    m_RandomSeed = RandomSeed.Next(),
                    m_SimulationFrame = m_SimulationSystem.frameIndex,
                    m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
                    m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out var dependencies),
                    m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out var dependencies2),
                    m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out var dependencies3),
                    m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out var dependencies4),
                    m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
                    m_LaneObjectBuffer = m_Actions.m_LaneObjectUpdater.Begin(Allocator.TempJob),
                    m_LaneReservations = m_Actions.m_LaneReservationQueue.AsParallelWriter(),
                    m_LaneEffects = m_Actions.m_LaneEffectsQueue.AsParallelWriter(),
                    m_LaneSignals = m_Actions.m_LaneSignalQueue.AsParallelWriter(),
                    m_TrafficAmbienceEffects = m_Actions.m_TrafficAmbienceQueue.AsParallelWriter(),
                    m_CarLookup = SystemAPI.GetComponentLookup<Game.Vehicles.Car>(true),
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<Game.Prefabs.PrefabRef>(true),
                    m_PrefabCarDataLookup = SystemAPI.GetComponentLookup<Game.Prefabs.CarData>(true),
                    m_AmbulanceLookup = SystemAPI.GetComponentLookup<Game.Vehicles.Ambulance>(true),
                    m_DeliveryTruckLookup = SystemAPI.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true),
                    m_FireEngineLookup = SystemAPI.GetComponentLookup<Game.Vehicles.FireEngine>(true),
                    m_GarbageTruckLookup = SystemAPI.GetComponentLookup<Game.Vehicles.GarbageTruck>(true),
                    m_HearseLookup = SystemAPI.GetComponentLookup<Game.Vehicles.Hearse>(true),
                    m_MaintenanceVehicleLookup = SystemAPI.GetComponentLookup<Game.Vehicles.MaintenanceVehicle>(true),
                    m_PersonalCarLookup = SystemAPI.GetComponentLookup<Game.Vehicles.PersonalCar>(true),
                    m_PoliceCarLookup = SystemAPI.GetComponentLookup<Game.Vehicles.PoliceCar>(true),
                    m_PostVanLookup = SystemAPI.GetComponentLookup<Game.Vehicles.PostVan>(true),
                    m_PublicTransportLookup = SystemAPI.GetComponentLookup<Game.Vehicles.PublicTransport>(true),
                    m_TaxiLookup = SystemAPI.GetComponentLookup<Game.Vehicles.Taxi>(true),
                },
                m_VehicleQuery,
                JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3, dependencies4)
            );
            m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
            m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
            m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
            m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
            m_TerrainSystem.AddCPUHeightReader(jobHandle);
            m_Actions.m_Dependency = jobHandle;
            base.Dependency = jobHandle;
        }

        [Preserve]
        public PatchedCarNavigationSystem() { }
    }
}
