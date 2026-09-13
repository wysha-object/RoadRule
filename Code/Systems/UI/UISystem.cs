using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.Entities;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.UI;
using RoadRule.Systems.Pathfind;
using RoadRule.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Systems.UI
{
    public partial class UISystem : UISystemBase
    {
        public enum ToolState
        {
            Disabled = 0,
            Choosing = 1,
            Choosed = 2,
        }

        public struct MasterLaneValue
        {
            public List<Entity> m_MasterLaneEntities { get; set; } = new List<Entity>();
            public Dictionary<byte, List<Entity>> m_LaneIndexDictionary { get; set; } = new Dictionary<byte, List<Entity>>();

            public MasterLaneValue() { }
        }

        public struct CompositionParameters : INativeDisposable, IDisposable
        {
            public NativeHashMap<byte, CompositionLaneParameters> m_CompositionLaneParameters;

            public float m_SpeedLimit;

            public CompositionParameters(AllocatorManager.AllocatorHandle allocator)
            {
                m_CompositionLaneParameters = new NativeHashMap<byte, CompositionLaneParameters>(16, allocator);
            }

            public static CompositionParameters CreateCompositionParameters(
                AllocatorManager.AllocatorHandle allocator,
                RoadComposition roadComposition,
                DynamicBuffer<NetCompositionLane> netCompositionLaneBuffer
            )
            {
                var compositionParameters = new CompositionParameters(allocator);

                foreach (var netCompositionLane in netCompositionLaneBuffer)
                {
                    if (((netCompositionLane.m_Flags & LaneFlags.Road) != 0) || ((netCompositionLane.m_Flags & LaneFlags.Master) != 0))
                    {
                        compositionParameters.m_CompositionLaneParameters.Add(
                            netCompositionLane.m_Index,
                            new CompositionLaneParameters
                            {
                                m_IsInvert = (netCompositionLane.m_Flags & LaneFlags.Invert) != 0,
                                m_IsMaster = (netCompositionLane.m_Flags & LaneFlags.Master) != 0,
                            }
                        );
                    }
                }

                compositionParameters.m_SpeedLimit = roadComposition.m_SpeedLimit;

                return compositionParameters;
            }

            public JobHandle Dispose(JobHandle inputDeps)
            {
                if (m_CompositionLaneParameters.IsCreated)
                {
                    inputDeps = m_CompositionLaneParameters.Dispose(inputDeps);
                }
                return inputDeps;
            }

            public void Dispose()
            {
                if (m_CompositionLaneParameters.IsCreated)
                {
                    m_CompositionLaneParameters.Dispose();
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CompositionParameters parameters))
                {
                    return false;
                }

                foreach (var kvp in m_CompositionLaneParameters)
                {
                    if (!parameters.m_CompositionLaneParameters.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                    {
                        return false;
                    }
                }
                foreach (var kvp in parameters.m_CompositionLaneParameters)
                {
                    if (!m_CompositionLaneParameters.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                    {
                        return false;
                    }
                }

                if (!Equals(m_SpeedLimit, parameters.m_SpeedLimit))
                {
                    return false;
                }

                return true;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_CompositionLaneParameters, m_SpeedLimit);
            }
        }

        public struct CompositionLaneParameters
        {
            public bool m_IsInvert;

            public bool m_IsMaster;

            public override bool Equals(object obj)
            {
                return obj is CompositionLaneParameters parameters && Equals(m_IsInvert, parameters.m_IsInvert) && Equals(m_IsMaster, parameters.m_IsMaster);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_IsInvert, m_IsMaster);
            }
        }

        public struct EdgeParameters : INativeDisposable, IDisposable
        {
            public Entity m_Entity;

            public float m_Width;

            public NativeHashMap<byte, LaneParameters> m_CompositionLaneParameters;

            public EdgeParameters(AllocatorManager.AllocatorHandle allocator)
            {
                m_CompositionLaneParameters = new NativeHashMap<byte, LaneParameters>(16, allocator);
            }

            public static EdgeParameters CreateCompositionParameters(
                AllocatorManager.AllocatorHandle allocator,
                Entity edgeEntity,
                NetCompositionData netCompositionData,
                DynamicBuffer<Game.Net.SubLane> subLaneBuffer,
                DynamicBuffer<NetCompositionLane> netCompositionLaneBuffer,
                ComponentLookup<Lane> laneLookup,
                ComponentLookup<Game.Net.CarLane> carLaneLookup,
                ComponentLookup<NetLaneData> netLaneDataLookup
            )
            {
                var compositionParameters = new EdgeParameters(allocator);

                compositionParameters.m_Entity = edgeEntity;

                compositionParameters.m_Width = netCompositionData.m_Width;

                foreach (var netCompositionLane in netCompositionLaneBuffer)
                {
                    if (((netCompositionLane.m_Flags & LaneFlags.Road) != 0) || ((netCompositionLane.m_Flags & LaneFlags.Master) != 0))
                    {
                        compositionParameters.m_CompositionLaneParameters.Add(
                            netCompositionLane.m_Index,
                            new LaneParameters(allocator) { m_Width = netLaneDataLookup[netCompositionLane.m_Lane].m_Width, m_Position = netCompositionLane.m_Position }
                        );
                    }
                }

                foreach (var subLane in subLaneBuffer)
                {
                    if (!carLaneLookup.TryGetComponent(subLane.m_SubLane, out var _))
                    {
                        continue;
                    }
                    if (laneLookup.TryGetComponent(subLane.m_SubLane, out var lane))
                    {
                        byte laneIndex = (byte)(lane.m_MiddleNode.GetLaneIndex() & 0xff);

                        if (compositionParameters.m_CompositionLaneParameters.TryGetValue(laneIndex, out var laneParameters))
                        {
                            laneParameters.m_SubLaneEntities.Add(subLane.m_SubLane);
                            compositionParameters.m_CompositionLaneParameters[laneIndex] = laneParameters;
                        }
                    }
                }

                return compositionParameters;
            }

            public JobHandle Dispose(JobHandle inputDeps)
            {
                if (m_CompositionLaneParameters.IsCreated)
                {
                    foreach (var kvp in m_CompositionLaneParameters)
                    {
                        inputDeps = kvp.Value.Dispose(inputDeps);
                    }
                    inputDeps = m_CompositionLaneParameters.Dispose(inputDeps);
                }
                return inputDeps;
            }

            public void Dispose()
            {
                Dispose(default);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is EdgeParameters parameters))
                {
                    return false;
                }

                if (!Equals(m_Width, parameters.m_Width))
                {
                    return false;
                }

                foreach (var kvp in m_CompositionLaneParameters)
                {
                    if (!parameters.m_CompositionLaneParameters.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                    {
                        return false;
                    }
                }
                foreach (var kvp in parameters.m_CompositionLaneParameters)
                {
                    if (!m_CompositionLaneParameters.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_Width, m_CompositionLaneParameters);
            }
        }

        public struct LaneParameters : INativeDisposable, IDisposable
        {
            public float m_Width;

            public float3 m_Position;

            public NativeHashSet<Entity> m_SubLaneEntities;

            public LaneParameters(AllocatorManager.AllocatorHandle allocator)
            {
                m_SubLaneEntities = new NativeHashSet<Entity>(16, allocator);
            }

            public JobHandle Dispose(JobHandle inputDeps)
            {
                if (m_SubLaneEntities.IsCreated)
                {
                    inputDeps = m_SubLaneEntities.Dispose(inputDeps);
                }
                return inputDeps;
            }

            void IDisposable.Dispose()
            {
                Dispose(default);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is LaneParameters parameters))
                {
                    return false;
                }

                if (!Equals(m_Width, parameters.m_Width) || !Equals(m_Position, parameters.m_Position))
                {
                    return false;
                }

                foreach (var subLaneEntity in m_SubLaneEntities)
                {
                    if (!parameters.m_SubLaneEntities.Contains(subLaneEntity))
                    {
                        return false;
                    }
                }
                foreach (var subLaneEntity in parameters.m_SubLaneEntities)
                {
                    if (!m_SubLaneEntities.Contains(subLaneEntity))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_Width, m_Position, m_SubLaneEntities);
            }
        }

        public List<EdgeParameters> SelectedEdgeList { get; private set; } = new List<EdgeParameters>();
        public HashSet<byte> SelectedLaneIndexSet { get; private set; } = new HashSet<byte>();
        private CompositionParameters? m_CompositionParameters;
        private Tool.ToolSystem m_ToolSystem;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private ObsoleteCheckSystem m_ObsoleteCheckSystem;

        public void ModificationUpdate()
        {
            Dependency = ScheduleOverlayJob(Dependency);
        }

        public void AddSelectedEdgeEntity(Entity selectedEdgeEntity)
        {
            if (selectedEdgeEntity == null || selectedEdgeEntity == Entity.Null || SelectedEdgeList.Any(e => e.m_Entity == selectedEdgeEntity))
            {
                return;
            }

            if (
                !EntityManager.TryGetComponent<Composition>(selectedEdgeEntity, out var composition)
                || !EntityManager.TryGetComponent<NetCompositionData>(composition.m_Edge, out var netCompositionData)
                || !EntityManager.TryGetComponent<RoadComposition>(composition.m_Edge, out var roadComposition)
                || !EntityManager.TryGetBuffer<Game.Net.SubLane>(selectedEdgeEntity, true, out var subLaneBuffer)
                || !EntityManager.TryGetBuffer<NetCompositionLane>(composition.m_Edge, true, out var netCompositionLaneBuffer)
            )
            {
                return;
            }
            var compositionParameters = CompositionParameters.CreateCompositionParameters(Allocator.Persistent, roadComposition, netCompositionLaneBuffer);
            if (SelectedEdgeList.Count == 0)
            {
                m_CompositionParameters = compositionParameters;
            }
            else
            {
                if (!compositionParameters.Equals(m_CompositionParameters))
                {
                    compositionParameters.Dispose();
                    return;
                }
                else
                {
                    compositionParameters.Dispose();
                }
            }

            var edgeCompositionParameters = EdgeParameters.CreateCompositionParameters(
                Allocator.Persistent,
                selectedEdgeEntity,
                netCompositionData,
                subLaneBuffer,
                netCompositionLaneBuffer,
                SystemAPI.GetComponentLookup<Lane>(true),
                SystemAPI.GetComponentLookup<Game.Net.CarLane>(true),
                SystemAPI.GetComponentLookup<NetLaneData>(true)
            );
            SelectedEdgeList.Add(edgeCompositionParameters);
            HandleSelectedEdgeEntityUpdate();
        }

        public void RemoveSelectedEdgeEntity(Entity selectedEdgeEntity)
        {
            List<int> removeIndexSet = new List<int>();
            for (int i = 0; i < SelectedEdgeList.Count; i++)
            {
                var edge = SelectedEdgeList[i];

                if (edge.m_Entity == selectedEdgeEntity)
                {
                    removeIndexSet.Add(i);
                    edge.Dispose();
                }
            }
            foreach (var index in removeIndexSet.OrderByDescending(i => i))
            {
                SelectedEdgeList.RemoveAt(index);
            }

            HandleSelectedEdgeEntityUpdate();
        }

        public void ClearSelectedEdgeEntity()
        {
            foreach (var edge in SelectedEdgeList)
            {
                edge.Dispose();
            }
            SelectedEdgeList.Clear();
            HandleSelectedEdgeEntityUpdate();
        }

        private void HandleSelectedEdgeEntityUpdate()
        {
            if (SelectedEdgeList.Count == 0)
            {
                m_CompositionParameters = null;
                if (new ToolState[] { ToolState.Choosed }.Contains(GetToolState()))
                {
                    SetToolState(ToolState.Choosing);
                }
            }
            else
            {
                SetToolState(ToolState.Choosed);
            }
            m_GetLanesBinding.Update();
            m_GetSelectedEdgeBinding.Update();
            m_GetCompositionBinding.Update();
        }

        public void AddSelectedLaneIndex(byte selectedLaneIndex)
        {
            var masterLaneDictionary = GetMasterLaneDictionary();
            if (
                selectedLaneIndex < 0
                || !(masterLaneDictionary.ContainsKey(selectedLaneIndex) || masterLaneDictionary.Any(kvp => kvp.Value.m_LaneIndexDictionary.ContainsKey(selectedLaneIndex)))
            )
            {
                return;
            }
            SelectedLaneIndexSet.Add(selectedLaneIndex);
            HandleSelectedLaneIndexUpdate();
        }

        public void RemoveSelectedLaneIndex(byte selectedLaneIndex)
        {
            SelectedLaneIndexSet.Remove(selectedLaneIndex);
            HandleSelectedLaneIndexUpdate();
        }

        public void ClearSelectedLaneIndex()
        {
            SelectedLaneIndexSet.Clear();
            HandleSelectedLaneIndexUpdate();
        }

        private void HandleSelectedLaneIndexUpdate()
        {
            m_GetSelectedLaneIndexBinding.Update();
        }

        public ToolState GetToolState()
        {
            return (ToolState)m_GetToolStateBinding.value;
        }

        public void SetToolState(ToolState toolState)
        {
            if (toolState == GetToolState())
            {
                return;
            }
            m_GetToolStateBinding.Update((int)toolState);
            ClearSelectedLaneIndex();
            switch (toolState)
            {
                case ToolState.Disabled:
                    ClearSelectedEdgeEntity();
                    m_ToolSystem.Disable();
                    break;
                case ToolState.Choosing:
                    ClearSelectedEdgeEntity();
                    m_ToolSystem.Enable();
                    break;
                case ToolState.Choosed:
                    m_ToolSystem.Enable();
                    break;
            }
        }

        public Dictionary<byte, MasterLaneValue> GetMasterLaneDictionary()
        {
            var result = new Dictionary<byte, MasterLaneValue>();

            if (m_CompositionParameters is CompositionParameters compositionParameters)
            {
                byte? master = null;
                byte? invertMaster = null;

                bool haveInvert = false;
                foreach (var kvp in compositionParameters.m_CompositionLaneParameters)
                {
                    if (kvp.Value.m_IsInvert)
                    {
                        haveInvert = true;
                    }
                }

                foreach (var kvp in compositionParameters.m_CompositionLaneParameters)
                {
                    if (kvp.Value.m_IsMaster)
                    {
                        if (haveInvert && kvp.Value.m_IsInvert)
                        {
                            invertMaster = kvp.Key;
                        }
                        else
                        {
                            master = kvp.Key;
                        }
                    }
                }

                if (master == null)
                {
                    foreach (var kvp in compositionParameters.m_CompositionLaneParameters)
                    {
                        if (kvp.Value.m_IsInvert)
                        {
                            invertMaster = kvp.Key;
                        }
                        else
                        {
                            master = kvp.Key;
                        }
                    }
                }

                if (haveInvert && invertMaster == null)
                {
                    foreach (var kvp in compositionParameters.m_CompositionLaneParameters)
                    {
                        if (kvp.Value.m_IsInvert)
                        {
                            invertMaster = kvp.Key;
                        }
                    }
                }

                if (master != null)
                {
                    result[master.Value] = new MasterLaneValue();
                }
                if (invertMaster != null)
                {
                    result[invertMaster.Value] = new MasterLaneValue();
                }

                foreach (var selectedEdge in SelectedEdgeList)
                {
                    foreach (var kvp in selectedEdge.m_CompositionLaneParameters)
                    {
                        var laneParameters = compositionParameters.m_CompositionLaneParameters[kvp.Key];

                        if (kvp.Key == master)
                        {
                            foreach (var subLaneEntity in kvp.Value.m_SubLaneEntities)
                            {
                                result[master.Value].m_MasterLaneEntities.Add(subLaneEntity);
                            }
                        }
                        else if (kvp.Key == invertMaster)
                        {
                            foreach (var subLaneEntity in kvp.Value.m_SubLaneEntities)
                            {
                                result[invertMaster.Value].m_MasterLaneEntities.Add(subLaneEntity);
                            }
                        }
                        else
                        {
                            var masterLaneIndex = laneParameters.m_IsInvert ? invertMaster : master;
                            if (!result[masterLaneIndex.Value].m_LaneIndexDictionary.ContainsKey(kvp.Key))
                            {
                                result[masterLaneIndex.Value].m_LaneIndexDictionary[kvp.Key] = new List<Entity>();
                            }
                            foreach (var subLaneEntity in kvp.Value.m_SubLaneEntities)
                            {
                                result[masterLaneIndex.Value].m_LaneIndexDictionary[kvp.Key].Add(subLaneEntity);
                            }
                        }
                    }
                }
            }

            return result;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<Tool.ToolSystem>();
            m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_ObsoleteCheckSystem = World.GetOrCreateSystemManaged<ObsoleteCheckSystem>();

            AddUIBindings();
            SetupKeyBindings();
        }

        private float3 m_PrevCameraPosition;
        private Vector3 m_PrevCameraRotation;

        protected override void OnUpdate()
        {
            var camera = m_CameraUpdateSystem.activeCamera;
            if (camera != null && (!Equals(m_PrevCameraPosition, camera.transform.position) || !Equals(m_PrevCameraRotation, camera.transform.rotation.eulerAngles)))
            {
                m_PrevCameraPosition = camera.transform.position;
                m_PrevCameraRotation = camera.transform.rotation.eulerAngles;
                m_GetLanesBinding.Update();
                m_GetSelectedEdgeBinding.Update();
                m_GetCameraBinding.Update();
            }
        }
    }
}
