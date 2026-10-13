using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using RoadRule.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        private ValueBinding<int> m_GetToolStateBinding;
        private ValueBinding<UITypes.GetLaneResultItemValue[]> m_GetAllLaneGroupBinding;
        private ValueBinding<UITypes.GetLaneResultItemValue[]> m_GetAllLaneBinding;
        private GetterValueBinding<Dictionary<int, UITypes.LaneGroupValue>> m_GetLanesBinding;
        private GetterValueBinding<int[]> m_GetSelectedLaneIndexBinding;
        private GetterValueBinding<UITypes.EdgeValue[]> m_GetSelectedEdgeBinding;
        private GetterValueBinding<UITypes.CompositionValue> m_GetCompositionBinding;

        private void AddUIBindings()
        {
            AddBinding(m_GetToolStateBinding = new ValueBinding<int>("RoadRule", "GetToolState", (int)ToolState.Disabled));
            AddBinding(
                m_GetAllLaneGroupBinding = new ValueBinding<UITypes.GetLaneResultItemValue[]>("RoadRule", "GetAllLaneGroup", [], new ListWriter<UITypes.GetLaneResultItemValue>())
            );
            AddBinding(m_GetAllLaneBinding = new ValueBinding<UITypes.GetLaneResultItemValue[]>("RoadRule", "GetAllLane", [], new ListWriter<UITypes.GetLaneResultItemValue>()));

            AddBinding(
                m_GetLanesBinding = new GetterValueBinding<Dictionary<int, UITypes.LaneGroupValue>>(
                    "RoadRule",
                    "GetLanes",
                    RoadRuleGetLanes,
                    new DictionaryWriter<int, UITypes.LaneGroupValue>()
                )
            );
            AddBinding(m_GetSelectedLaneIndexBinding = new GetterValueBinding<int[]>("RoadRule", "GetSelectedLaneIndex", RoadRuleGetSelectedLaneIndex, new ListWriter<int>()));
            AddBinding(
                m_GetSelectedEdgeBinding = new GetterValueBinding<UITypes.EdgeValue[]>("RoadRule", "GetSelectedEdge", RoadRuleGetSelectedEdge, new ListWriter<UITypes.EdgeValue>())
            );
            AddBinding(m_GetCompositionBinding = new GetterValueBinding<UITypes.CompositionValue>("RoadRule", "GetComposition", RoadRuleGetComposition));

            AddUpdateBinding(
                new GetterValueBinding<UITypes.CameraValue?>(
                    "RoadRule",
                    "GetCamera",
                    RoadRuleGetCamera,
                    new NullableStructWriter<UITypes.CameraValue>(new ValueWriter<UITypes.CameraValue>())
                )
            );

            AddBinding(new CallBinding<int, int>("RoadRule", "SetToolState", RoadRuleSetToolState));
            AddBinding(new CallBinding<UITypes.UpdateLaneInputValue, int>("RoadRule", "UpdateLane", RoadRuleUpdateLane));
            AddBinding(new CallBinding<UITypes.LookAtInputValue, string>("RoadRule", "LookAt", RoadRuleLookAt));
            AddBinding(new CallBinding<int, string>("RoadRule", "AddSelectedLaneIndex", RoadRuleAddSelectedLaneIndex));
            AddBinding(new CallBinding<int, string>("RoadRule", "RemoveSelectedLaneIndex", RoadRuleRemoveSelectedLaneIndex));
            AddBinding(new CallBinding<string, string>("RoadRule", "ClearSelectedLaneIndex", RoadRuleClearSelectedLaneIndex));
        }

        private Dictionary<int, UITypes.LaneGroupValue> RoadRuleGetLanes()
        {
            var masterMap = new Dictionary<int, UITypes.LaneGroupValue>();
            var camera = m_CameraUpdateSystem.activeCamera;
            if (camera == null)
            {
                return new Dictionary<int, UITypes.LaneGroupValue>();
            }
            foreach (var kvp in GetMasterLaneDictionary())
            {
                var laneList = new List<UITypes.LaneValue>();
                foreach (var laneIndexKVP in kvp.Value.m_LaneIndexDictionary)
                {
                    var laneIndex = laneIndexKVP.Key;
                    if (!EntityManager.TryGetComponent<Curve>(laneIndexKVP.Value[0], out var curve))
                    {
                        return new Dictionary<int, UITypes.LaneGroupValue>();
                    }

                    UITypes.LaneRulesValue? laneRulesValue = null;
                    UITypes.CarLaneValue? carLaneValue = null;
                    foreach (var subLaneEntity in laneIndexKVP.Value)
                    {
                        if (!EntityManager.TryGetComponent<LaneRules>(subLaneEntity, out var laneRules))
                        {
                            laneRules = new LaneRules();
                        }
                        if (!EntityManager.TryGetComponent<Game.Net.CarLane>(subLaneEntity, out var carLane))
                        {
                            return new Dictionary<int, UITypes.LaneGroupValue>();
                        }
                        laneRulesValue =
                            laneRulesValue == null
                                ? UITypes.LaneRulesValue.FromRules(laneRules)
                                : UITypes.LaneRulesValue.MergeRulesValues(laneRulesValue.Value, UITypes.LaneRulesValue.FromRules(laneRules));
                        carLaneValue =
                            carLaneValue == null
                                ? UITypes.CarLaneValue.FromCarLane(carLane)
                                : UITypes.CarLaneValue.MergeLanePropertyValues(carLaneValue.Value, UITypes.CarLaneValue.FromCarLane(carLane));
                    }

                    var worldPosition = curve.m_Bezier.d;
                    laneList.Add(
                        new UITypes.LaneValue
                        {
                            laneIndex = laneIndex,
                            position = new UITypes.PositionValue
                            {
                                x = worldPosition.x,
                                y = worldPosition.y,
                                z = worldPosition.z,
                            },
                            laneRules = laneRulesValue.Value,
                            carLane = carLaneValue.Value,
                        }
                    );
                }

                if (
                    !EntityManager.TryGetComponent<Curve>(kvp.Value.m_MasterLaneEntities[0], out var masterCurve)
                    || !EntityManager.TryGetComponent<Game.Net.CarLane>(kvp.Value.m_MasterLaneEntities[0], out var masterCarLane)
                )
                {
                    return new Dictionary<int, UITypes.LaneGroupValue>();
                }
                UITypes.LaneRulesValue? masterLaneRulesValue = null;
                UITypes.CarLaneValue? masterCarLaneValue = null;
                foreach (var masterLaneEntity in kvp.Value.m_MasterLaneEntities)
                {
                    if (!EntityManager.TryGetComponent<LaneRules>(masterLaneEntity, out var laneRules))
                    {
                        laneRules = new LaneRules();
                    }
                    masterLaneRulesValue =
                        masterLaneRulesValue == null
                            ? UITypes.LaneRulesValue.FromRules(laneRules)
                            : UITypes.LaneRulesValue.MergeRulesValues(masterLaneRulesValue.Value, UITypes.LaneRulesValue.FromRules(laneRules));
                    masterCarLaneValue =
                        masterCarLaneValue == null
                            ? UITypes.CarLaneValue.FromCarLane(masterCarLane)
                            : UITypes.CarLaneValue.MergeLanePropertyValues(masterCarLaneValue.Value, UITypes.CarLaneValue.FromCarLane(masterCarLane));
                }

                var masterWorldPosition = masterCurve.m_Bezier.d;
                var masterScreenPoint = camera.WorldToScreenPoint(new Vector3(masterWorldPosition.x, masterWorldPosition.y, masterWorldPosition.z));
                masterMap.Add(
                    kvp.Key,
                    new UITypes.LaneGroupValue
                    {
                        masterLane = new UITypes.LaneValue
                        {
                            laneIndex = kvp.Key,
                            position = new UITypes.PositionValue
                            {
                                x = masterWorldPosition.x,
                                y = masterWorldPosition.y,
                                z = masterWorldPosition.z,
                            },
                            laneRules = masterLaneRulesValue.Value,
                            carLane = masterCarLaneValue.Value,
                        },
                        lanes = laneList.ToArray(),
                    }
                );
            }
            return masterMap;
        }

        private int[] RoadRuleGetSelectedLaneIndex()
        {
            return SelectedLaneIndexSet.Select((item) => (int)item).ToArray();
        }

        private UITypes.EdgeValue[] RoadRuleGetSelectedEdge()
        {
            var selectedEdgeList = new List<UITypes.EdgeValue>();
            var camera = m_CameraUpdateSystem.activeCamera;
            if (camera == null)
            {
                return [];
            }
            foreach (var edge in SelectedEdgeList)
            {
                if (!EntityManager.TryGetComponent<EdgeGeometry>(edge.m_Entity, out var edgeGeometry))
                {
                    continue;
                }
                var worldPosition = math.lerp(edgeGeometry.m_Bounds.min, edgeGeometry.m_Bounds.max, 0.5f);
                selectedEdgeList.Add(
                    new UITypes.EdgeValue
                    {
                        edgeEntity = new UITypes.EntityValue { index = edge.m_Entity.Index, version = edge.m_Entity.Version },
                        position = new UITypes.PositionValue
                        {
                            x = worldPosition.x,
                            y = worldPosition.y,
                            z = worldPosition.z,
                        },
                    }
                );
            }
            return selectedEdgeList.ToArray();
        }

        private UITypes.CompositionValue RoadRuleGetComposition()
        {
            float speedLimit = 60f;
            if (m_CompositionParameters is CompositionParameters composition)
            {
                speedLimit = composition.m_SpeedLimit * 1.8f;
            }
            return new UITypes.CompositionValue { speedLimit = speedLimit };
        }

        private UITypes.CameraValue? RoadRuleGetCamera()
        {
            var camera = m_CameraUpdateSystem.activeCamera;
            return camera == null ? null : UITypes.CameraValue.FromCamera(camera);
        }

        private int RoadRuleSetToolState(int inputValue)
        {
            SetToolState((ToolState)inputValue);
            return 0;
        }

        private int RoadRuleUpdateLane(UITypes.UpdateLaneInputValue inputValue)
        {
            var masterLaneDictionary = GetMasterLaneDictionary();
            var laneIndexDictionary = new Dictionary<int, List<Entity>>();
            foreach (var kvp in masterLaneDictionary)
            {
                laneIndexDictionary[kvp.Key] = kvp.Value.m_MasterLaneEntities;
                foreach (var laneIndexKVP in kvp.Value.m_LaneIndexDictionary)
                {
                    laneIndexDictionary[laneIndexKVP.Key] = laneIndexKVP.Value;
                }
            }

            if (!laneIndexDictionary.TryGetValue(inputValue.laneIndex, out var laneEntities))
            {
                throw new InvalidOperationException($"Lane index {inputValue.laneIndex} is not available for updating.");
            }

            if (inputValue.key == UITypes.UpdateLaneType.LaneRules && inputValue.value is UITypes.LaneRulesValue laneRulesValue)
            {
                foreach (var e in laneEntities)
                {
                    if (!EntityManager.TryGetComponent<LaneRules>(e, out var laneRules))
                    {
                        laneRules = new LaneRules();
                        EntityManager.AddComponentData(e, laneRules);
                    }

                    laneRules = UITypes.LaneRulesValue.ApplyRulesValue(laneRules, laneRulesValue);
                    EntityManager.SetComponentData(e, laneRules);
                    EntityManager.AddComponentData(e, new Updated());
                }
            }
            else if (inputValue.key == UITypes.UpdateLaneType.CarLane && inputValue.value is UITypes.CarLaneValue carLaneValue)
            {
                foreach (var e in laneEntities)
                {
                    if (!EntityManager.HasComponent<Game.Net.CarLane>(e))
                    {
                        throw new InvalidOperationException($"Lane {e} has no CarLane component.");
                    }
                }
                foreach (var e in laneEntities)
                {
                    var carLane = EntityManager.GetComponentData<Game.Net.CarLane>(e);
                    carLane = UITypes.CarLaneValue.ApplyLanePropertyValue(carLane, carLaneValue);
                    EntityManager.SetComponentData(e, carLane);
                    EntityManager.AddComponentData(e, new Updated());
                }
            }

            m_GetLanesBinding.Update();
            return 0;
        }

        private string RoadRuleLookAt(UITypes.LookAtInputValue inputValue)
        {
            var pivot = new float3(inputValue.position.x, inputValue.position.y, inputValue.position.z);
            var zoom = inputValue.distance;

            var cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            cameraUpdateSystem.activeCameraController.pivot = pivot;
            cameraUpdateSystem.activeCameraController.zoom = zoom;
            return "";
        }

        private string RoadRuleAddSelectedLaneIndex(int inputValue)
        {
            AddSelectedLaneIndex((byte)inputValue);
            return "";
        }

        private string RoadRuleRemoveSelectedLaneIndex(int inputValue)
        {
            RemoveSelectedLaneIndex((byte)inputValue);
            return "";
        }

        private string RoadRuleClearSelectedLaneIndex(string inputValue)
        {
            ClearSelectedLaneIndex();
            return "";
        }
    }
}
