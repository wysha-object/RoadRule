using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Newtonsoft.Json;
using RoadRule.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        private ValueBinding<int> m_GetToolStateBinding;
        private ValueBinding<GetLaneResultItem[]> m_GetAllLaneGroupBinding;
        private ValueBinding<GetLaneResultItem[]> m_GetAllLaneBinding;
        private GetterValueBinding<Dictionary<int, LaneGroupValue>> m_GetLanesBinding;
        private GetterValueBinding<int[]> m_GetSelectedLaneIndexBinding;
        private GetterValueBinding<EdgeValue[]> m_GetSelectedEdgeBinding;
        private GetterValueBinding<CompositionValue> m_GetCompositionBinding;

        private void AddUIBindings()
        {
            AddBinding(m_GetToolStateBinding = new ValueBinding<int>("RoadRule", "GetToolState", (int)ToolState.Disabled));
            AddBinding(m_GetAllLaneGroupBinding = new ValueBinding<GetLaneResultItem[]>("RoadRule", "GetAllLaneGroup", [], new ListWriter<GetLaneResultItem>()));
            AddBinding(m_GetAllLaneBinding = new ValueBinding<GetLaneResultItem[]>("RoadRule", "GetAllLane", [], new ListWriter<GetLaneResultItem>()));

            AddBinding(
                m_GetLanesBinding = new GetterValueBinding<Dictionary<int, LaneGroupValue>>(
                    "RoadRule",
                    "GetLanes",
                    () =>
                    {
                        var masterMap = new Dictionary<int, LaneGroupValue>();
                        var camera = m_CameraUpdateSystem.activeCamera;
                        if (camera == null)
                        {
                            return new Dictionary<int, LaneGroupValue>();
                        }
                        foreach (var kvp in GetMasterLaneDictionary())
                        {
                            var laneList = new List<LaneValue>();
                            foreach (var laneIndexKVP in kvp.Value.m_LaneIndexDictionary)
                            {
                                var laneIndex = laneIndexKVP.Key;
                                if (!EntityManager.TryGetComponent<Curve>(laneIndexKVP.Value[0], out var curve))
                                {
                                    return new Dictionary<int, LaneGroupValue>();
                                }

                                LaneRulesValue? laneRulesValue = null;
                                CarLaneValue? CarLaneValue = null;
                                foreach (var subLaneEntity in laneIndexKVP.Value)
                                {
                                    if (!EntityManager.TryGetComponent<LaneRules>(subLaneEntity, out var laneRules))
                                    {
                                        laneRules = new LaneRules();
                                    }
                                    if (!EntityManager.TryGetComponent<Game.Net.CarLane>(subLaneEntity, out var carLane))
                                    {
                                        return new Dictionary<int, LaneGroupValue>();
                                    }
                                    laneRulesValue =
                                        laneRulesValue == null
                                            ? LaneRulesValue.FromRules(laneRules)
                                            : LaneRulesValue.MergeRulesValues(laneRulesValue.Value, LaneRulesValue.FromRules(laneRules));
                                    CarLaneValue =
                                        CarLaneValue == null
                                            ? UISystem.CarLaneValue.FromCarLane(carLane)
                                            : UISystem.CarLaneValue.MergeLanePropertyValues(CarLaneValue.Value, UISystem.CarLaneValue.FromCarLane(carLane));
                                }

                                var worldPosition = curve.m_Bezier.d;
                                laneList.Add(
                                    new LaneValue
                                    {
                                        laneIndex = laneIndex,
                                        position = new PositionValue
                                        {
                                            x = worldPosition.x,
                                            y = worldPosition.y,
                                            z = worldPosition.z,
                                        },
                                        laneRules = laneRulesValue.Value,
                                        carLane = CarLaneValue.Value,
                                    }
                                );
                            }

                            if (
                                !EntityManager.TryGetComponent<Curve>(kvp.Value.m_MasterLaneEntities[0], out var masterCurve)
                                || !EntityManager.TryGetComponent<Game.Net.CarLane>(kvp.Value.m_MasterLaneEntities[0], out var masterCarLane)
                            )
                            {
                                return new Dictionary<int, LaneGroupValue>();
                            }
                            LaneRulesValue? masterLaneRulesValue = null;
                            CarLaneValue? masterCarLaneValue = null;
                            foreach (var masterLaneEntity in kvp.Value.m_MasterLaneEntities)
                            {
                                if (!EntityManager.TryGetComponent<LaneRules>(masterLaneEntity, out var laneRules))
                                {
                                    laneRules = new LaneRules();
                                }
                                masterLaneRulesValue =
                                    masterLaneRulesValue == null
                                        ? LaneRulesValue.FromRules(laneRules)
                                        : LaneRulesValue.MergeRulesValues(masterLaneRulesValue.Value, LaneRulesValue.FromRules(laneRules));
                                masterCarLaneValue =
                                    masterCarLaneValue == null
                                        ? CarLaneValue.FromCarLane(masterCarLane)
                                        : CarLaneValue.MergeLanePropertyValues(masterCarLaneValue.Value, CarLaneValue.FromCarLane(masterCarLane));
                            }

                            var masterWorldPosition = masterCurve.m_Bezier.d;
                            var masterScreenPoint = camera.WorldToScreenPoint(new Vector3(masterWorldPosition.x, masterWorldPosition.y, masterWorldPosition.z));
                            masterMap.Add(
                                kvp.Key,
                                new LaneGroupValue
                                {
                                    masterLane = new LaneValue
                                    {
                                        laneIndex = kvp.Key,
                                        position = new PositionValue
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
                    },
                    new DictionaryWriter<int, LaneGroupValue>()
                )
            );
            AddBinding(
                m_GetSelectedLaneIndexBinding = new GetterValueBinding<int[]>(
                    "RoadRule",
                    "GetSelectedLaneIndex",
                    () => SelectedLaneIndexSet.Select((item) => (int)item).ToArray(),
                    new ListWriter<int>()
                )
            );
            AddBinding(
                m_GetSelectedEdgeBinding = new GetterValueBinding<EdgeValue[]>(
                    "RoadRule",
                    "GetSelectedEdge",
                    () =>
                    {
                        var selectedEdgeList = new List<EdgeValue>();
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
                                new EdgeValue
                                {
                                    edgeEntity = new EntityValue { index = edge.m_Entity.Index, version = edge.m_Entity.Version },
                                    position = new PositionValue
                                    {
                                        x = worldPosition.x,
                                        y = worldPosition.y,
                                        z = worldPosition.z,
                                    },
                                }
                            );
                        }
                        return selectedEdgeList.ToArray();
                    },
                    new ListWriter<EdgeValue>()
                )
            );
            AddBinding(
                m_GetCompositionBinding = new GetterValueBinding<CompositionValue>(
                    "RoadRule",
                    "GetComposition",
                    () =>
                    {
                        float speedLimit = 60f;
                        if (m_CompositionParameters is CompositionParameters composition)
                        {
                            speedLimit = composition.m_SpeedLimit * 1.8f;
                        }
                        return new CompositionValue { speedLimit = speedLimit };
                    }
                )
            );

            AddUpdateBinding(
                new GetterValueBinding<CameraValue?>(
                    "RoadRule",
                    "GetCamera",
                    () =>
                    {
                        var camera = m_CameraUpdateSystem.activeCamera;
                        return camera == null ? null : CameraValue.FromCamera(camera);
                    },
                    new NullableStructWriter<CameraValue>(new ValueWriter<CameraValue>())
                )
            );

            AddBinding(
                new CallBinding<int, string>(
                    "RoadRule",
                    "SetToolState",
                    (inputValue) =>
                    {
                        SetToolState((ToolState)inputValue);
                        return "";
                    }
                )
            );
            AddBinding(
                new CallBinding<string, string>(
                    "RoadRule",
                    "UpdateLane",
                    (inputJsonString) =>
                    {
                        var inputValue = JsonConvert.DeserializeAnonymousType(
                            inputJsonString,
                            new
                            {
                                laneIndex = -1,
                                key = "",
                                value = "",
                            }
                        );

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

                        if (!laneIndexDictionary.ContainsKey(inputValue.laneIndex))
                        {
                            return "";
                        }

                        if (inputValue.key == "lane-rules")
                        {
                            var value = JsonConvert.DeserializeAnonymousType(inputValue.value, new LaneRulesValue());
                            foreach (var e in laneIndexDictionary[inputValue.laneIndex])
                            {
                                if (!EntityManager.TryGetComponent<LaneRules>(e, out var laneRules))
                                {
                                    laneRules = new LaneRules();
                                    EntityManager.AddComponentData(e, laneRules);
                                }

                                laneRules = LaneRulesValue.ApplyRulesValue(laneRules, value);
                                EntityManager.SetComponentData(e, laneRules);
                                EntityManager.AddComponentData(e, new Updated());
                            }
                        }
                        else if (inputValue.key == "car-lane")
                        {
                            var value = JsonConvert.DeserializeAnonymousType(inputValue.value, new CarLaneValue());
                            foreach (var e in laneIndexDictionary[inputValue.laneIndex])
                            {
                                if (!EntityManager.TryGetComponent<Game.Net.CarLane>(e, out var carLane))
                                {
                                    return "";
                                }
                                carLane = CarLaneValue.ApplyLanePropertyValue(carLane, value);
                                EntityManager.SetComponentData(e, carLane);
                                EntityManager.AddComponentData(e, new Updated());
                            }
                        }
                        else
                        {
                            return "";
                        }

                        m_GetLanesBinding.Update();
                        return "";
                    }
                )
            );
            AddBinding(
                new CallBinding<string, string>(
                    "RoadRule",
                    "LookAt",
                    (inputJsonString) =>
                    {
                        var inputValue = JsonConvert.DeserializeAnonymousType(
                            inputJsonString,
                            new
                            {
                                x = 0f,
                                y = 0f,
                                z = 0f,
                                distance = 0f,
                            }
                        );
                        var pivot = new float3(inputValue.x, inputValue.y, inputValue.z);
                        var zoom = inputValue.distance;

                        var cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
                        cameraUpdateSystem.activeCameraController.pivot = pivot;
                        cameraUpdateSystem.activeCameraController.zoom = zoom;
                        return "";
                    }
                )
            );
            AddBinding(
                new CallBinding<int, string>(
                    "RoadRule",
                    "AddSelectedLaneIndex",
                    (inputValue) =>
                    {
                        AddSelectedLaneIndex((byte)inputValue);
                        return "";
                    }
                )
            );
            AddBinding(
                new CallBinding<int, string>(
                    "RoadRule",
                    "RemoveSelectedLaneIndex",
                    (inputValue) =>
                    {
                        RemoveSelectedLaneIndex((byte)inputValue);
                        return "";
                    }
                )
            );
            AddBinding(
                new CallBinding<string, string>(
                    "RoadRule",
                    "ClearSelectedLaneIndex",
                    (inputValue) =>
                    {
                        ClearSelectedLaneIndex();
                        return "";
                    }
                )
            );
        }
    }
}
