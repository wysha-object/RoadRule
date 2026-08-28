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

        public List<Entity> SelectedEdgeEntityList { get; private set; } = new List<Entity>();
        public HashSet<int> SelectedLaneIndexSet { get; private set; } = new HashSet<int>();
        private Entity m_CompositionEdgePrefabEntity;
        private Tool.ToolSystem m_ToolSystem;
        private CameraUpdateSystem m_CameraUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<Tool.ToolSystem>();
            m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();

            AddUIBindings();
        }

        private float3 m_CameraPosition;
        private Vector3 m_CameraRotation;

        protected override void OnUpdate()
        {
            var camera = m_CameraUpdateSystem.activeCamera;
            if (camera != null && (!Equals(m_CameraPosition, camera.transform.position) || !Equals(m_CameraRotation, camera.transform.rotation.eulerAngles)))
            {
                m_CameraPosition = camera.transform.position;
                m_CameraRotation = camera.transform.rotation.eulerAngles;
                m_GetLanesBinding.Update();
                m_GetSelectedEdgeBinding.Update();
                m_GetCameraBinding.Update();
            }
        }

        public void ModificationUpdate()
        {
            var overlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            var buffer = overlayRenderSystem.GetBuffer(out var overlayRenderDependencies);
            var dictionary = GetMasterLaneDictionary();

            var selectedEdgeEntityArray = new NativeArray<Entity>(SelectedEdgeEntityList.ToArray(), Allocator.TempJob);
            var selectedLaneIndexArray = new NativeArray<int>(SelectedLaneIndexSet.ToArray(), Allocator.TempJob);
            var masterLaneIndexMap = new NativeHashMap<int, UnsafeList<int>>(dictionary.Count, Allocator.TempJob);
            foreach (var kvp in dictionary)
            {
                var keys = kvp.Value.m_LaneIndexDictionary.Keys;
                var list = new UnsafeList<int>(keys.Count, Allocator.TempJob);
                foreach (var key in keys)
                {
                    list.Add(key);
                }
                if (list.Length == 0)
                {
                    list.Add(kvp.Key);
                }
                masterLaneIndexMap.Add(kvp.Key, list);
            }

            Dependency = new OverlayJobHandle
            {
                m_NetCompositionDataLookup = SystemAPI.GetComponentLookup<NetCompositionData>(true),
                m_NetCompositionLaneBufferLookup = SystemAPI.GetBufferLookup<NetCompositionLane>(true),
                m_EdgeGeometryLookup = SystemAPI.GetComponentLookup<EdgeGeometry>(true),
                m_NetLaneDataLookup = SystemAPI.GetComponentLookup<NetLaneData>(true),
                m_SelectedEdgeEntityArray = selectedEdgeEntityArray,
                m_SelectedLaneIndexArray = selectedLaneIndexArray,
                m_CompositionEdgePrefabEntity = m_CompositionEdgePrefabEntity,
                m_MasterLaneIndexMap = masterLaneIndexMap,
                m_OverlayRenderSystemBuffer = buffer,
            }.Schedule(JobHandle.CombineDependencies(Dependency, overlayRenderDependencies));

            foreach (var kvp in masterLaneIndexMap)
            {
                var cloned = masterLaneIndexMap[kvp.Key];
                Dependency = cloned.Dispose(Dependency);
            }
            Dependency = masterLaneIndexMap.Dispose(Dependency);
            Dependency = selectedLaneIndexArray.Dispose(Dependency);
            Dependency = selectedEdgeEntityArray.Dispose(Dependency);
        }

        public void AddSelectedEdgeEntity(Entity selectedEdgeEntity)
        {
            if (selectedEdgeEntity == null || selectedEdgeEntity == Entity.Null)
            {
                return;
            }
            if (SelectedEdgeEntityList.Count == 0)
            {
                if (!EntityManager.TryGetComponent<Composition>(selectedEdgeEntity, out var composition))
                {
                    return;
                }
                m_CompositionEdgePrefabEntity = composition.m_Edge;
            }
            else
            {
                if (!EntityManager.TryGetComponent<Composition>(selectedEdgeEntity, out var composition) || composition.m_Edge != m_CompositionEdgePrefabEntity)
                {
                    return;
                }
            }
            SelectedEdgeEntityList.Add(selectedEdgeEntity);
            HandleSelectedEdgeEntityUpdate();
        }

        public void RemoveSelectedEdgeEntity(Entity selectedEdgeEntity)
        {
            SelectedEdgeEntityList.Remove(selectedEdgeEntity);
            HandleSelectedEdgeEntityUpdate();
        }

        public void ClearSelectedEdgeEntity()
        {
            SelectedEdgeEntityList.Clear();
            HandleSelectedEdgeEntityUpdate();
        }

        private void HandleSelectedEdgeEntityUpdate()
        {
            if (SelectedEdgeEntityList.Count == 0)
            {
                m_CompositionEdgePrefabEntity = Entity.Null;
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
        }

        public void AddSelectedLaneIndex(int selectedLaneIndex)
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

        public void RemoveSelectedLaneIndex(int selectedLaneIndex)
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

        public struct MasterLaneValue
        {
            public List<Entity> m_MasterLaneEntities { get; set; } = new List<Entity>();
            public Dictionary<int, List<Entity>> m_LaneIndexDictionary { get; set; } = new Dictionary<int, List<Entity>>();

            public MasterLaneValue() { }
        }

        public Dictionary<int, MasterLaneValue> GetMasterLaneDictionary()
        {
            var result = new Dictionary<int, MasterLaneValue>();

            if (EntityManager.TryGetBuffer<NetCompositionLane>(m_CompositionEdgePrefabEntity, true, out var netCompositionLaneBuffer))
            {
                int master = -1;
                int invertMaster = -1;

                foreach (var netCompositionLane in netCompositionLaneBuffer)
                {
                    if ((netCompositionLane.m_Flags & LaneFlags.Master) != 0)
                    {
                        if ((netCompositionLane.m_Flags & LaneFlags.Invert) != 0)
                        {
                            invertMaster = netCompositionLane.m_Index;
                        }
                        else
                        {
                            master = netCompositionLane.m_Index;
                        }
                    }
                }

                if (master == -1)
                {
                    foreach (var netCompositionLane in netCompositionLaneBuffer)
                    {
                        if ((netCompositionLane.m_Flags & LaneFlags.Road) != 0)
                        {
                            if ((netCompositionLane.m_Flags & LaneFlags.Invert) != 0)
                            {
                                invertMaster = netCompositionLane.m_Index;
                            }
                            else
                            {
                                master = netCompositionLane.m_Index;
                            }
                        }
                    }
                }

                if (master >= 0)
                {
                    result[master] = new MasterLaneValue();
                }
                if (invertMaster >= 0)
                {
                    result[invertMaster] = new MasterLaneValue();
                }

                foreach (var selectedEdgeEntity in SelectedEdgeEntityList)
                {
                    if (EntityManager.TryGetBuffer<Game.Net.SubLane>(selectedEdgeEntity, true, out var subLaneBuffer))
                    {
                        foreach (var subLane in subLaneBuffer)
                        {
                            var e = subLane.m_SubLane;
                            if (
                                !EntityManager.TryGetComponent<Lane>(e, out var lane)
                                || !EntityManager.HasComponent<EdgeLane>(e)
                                || (subLane.m_PathMethods & PathMethod.Road) == 0
                                || EntityManager.HasComponent<Game.Net.SecondaryLane>(subLane.m_SubLane)
                            )
                            {
                                continue;
                            }

                            int laneIndex = lane.m_MiddleNode.GetLaneIndex() & 0xff;

                            var netCompositionLane = netCompositionLaneBuffer.ElementAt(laneIndex);
                            if (!netCompositionLane.m_Flags.HasFlag(LaneFlags.Road))
                            {
                                continue;
                            }

                            if (laneIndex == master)
                            {
                                result[master].m_MasterLaneEntities.Add(selectedEdgeEntity);
                            }
                            else if (laneIndex == invertMaster)
                            {
                                result[invertMaster].m_MasterLaneEntities.Add(selectedEdgeEntity);
                            }
                            else
                            {
                                var masterLaneIndex = (netCompositionLane.m_Flags & LaneFlags.Invert) != 0 ? invertMaster : master;
                                if (!result[masterLaneIndex].m_LaneIndexDictionary.ContainsKey(laneIndex))
                                {
                                    result[masterLaneIndex].m_LaneIndexDictionary[laneIndex] = new List<Entity>();
                                }
                                result[masterLaneIndex].m_LaneIndexDictionary[laneIndex].Add(e);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
