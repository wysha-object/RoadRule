using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Rendering;
using Game.Vehicles;
using Newtonsoft.Json;
using RoadRule.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        private ValueBinding<int> m_GetToolStateBinding;
        private GetterValueBinding<string> m_LocalisationBinding;
        private GetterValueBinding<string> m_GetCameraBinding;
        private GetterValueBinding<string> m_GetLanesBinding;
        private GetterValueBinding<string> m_GetSelectedLaneIndexBinding;
        private GetterValueBinding<string> m_GetSelectedEdgeBinding;

        private void AddUIBindings()
        {
            AddBinding(m_GetToolStateBinding = new ValueBinding<int>("RoadRule", "GetToolState", (int)ToolState.Disabled));

            AddBinding(
                m_GetCameraBinding = new GetterValueBinding<string>(
                    "RoadRule",
                    "GetCamera",
                    () =>
                    {
                        var camera = m_CameraUpdateSystem.activeCamera;
                        if (camera == null)
                        {
                            return JsonConvert.SerializeObject(
                                new
                                {
                                    position = new
                                    {
                                        x = 0f,
                                        y = 0f,
                                        z = 0f,
                                    },
                                    rotation = new
                                    {
                                        x = 0f,
                                        y = 0f,
                                        z = 0f,
                                    },
                                }
                            );
                        }
                        var position = camera.transform.position;
                        var rotation = camera.transform.rotation;
                        return JsonConvert.SerializeObject(
                            new
                            {
                                position = new
                                {
                                    x = position.x,
                                    y = position.y,
                                    z = position.z,
                                },
                                rotation = new
                                {
                                    x = rotation.eulerAngles.x,
                                    y = rotation.eulerAngles.y,
                                    z = rotation.eulerAngles.z,
                                },
                            }
                        );
                    }
                )
            );
            AddBinding(
                m_GetLanesBinding = new GetterValueBinding<string>(
                    "RoadRule",
                    "GetLanes",
                    () =>
                    {
                        var masterMap = new Dictionary<int, object>();
                        var camera = m_CameraUpdateSystem.activeCamera;
                        if (camera == null)
                        {
                            return JsonConvert.SerializeObject(new Dictionary<int, object>());
                        }
                        foreach (var kvp in GetMasterLaneDictionary())
                        {
                            var laneList = new List<object>();
                            foreach (var laneIndexKVP in kvp.Value.m_LaneIndexDictionary)
                            {
                                var laneIndex = laneIndexKVP.Key;
                                if (!EntityManager.TryGetComponent<Curve>(laneIndexKVP.Value[0], out var curve))
                                {
                                    return JsonConvert.SerializeObject(new Dictionary<int, object>());
                                }

                                LaneRulesValue? laneRulesValue = null;
                                foreach (var subLaneEntity in laneIndexKVP.Value)
                                {
                                    if (!EntityManager.TryGetComponent<LaneRules>(subLaneEntity, out var laneRules))
                                    {
                                        laneRules = new LaneRules();
                                    }
                                    laneRulesValue =
                                        laneRulesValue == null
                                            ? LaneRulesValue.FromRules(laneRules)
                                            : LaneRulesValue.MergeRulesValues(laneRulesValue.Value, LaneRulesValue.FromRules(laneRules));
                                }

                                var worldPosition = curve.m_Bezier.d;
                                var screenPoint = camera.WorldToScreenPoint(new Vector3(worldPosition.x, worldPosition.y, worldPosition.z));
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
                                        screenPoint = new ScreenPointValue { top = Screen.height - screenPoint.y, left = screenPoint.x },
                                        laneRules = laneRulesValue.Value,
                                    }
                                );
                            }

                            if (!EntityManager.TryGetComponent<Curve>(kvp.Value.m_MasterLaneEntities[0], out var masterCurve))
                            {
                                return JsonConvert.SerializeObject(new Dictionary<int, object>());
                            }
                            LaneRulesValue? masterLaneRulesValue = null;
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
                            }

                            var masterWorldPosition = masterCurve.m_Bezier.d;
                            var masterScreenPoint = camera.WorldToScreenPoint(new Vector3(masterWorldPosition.x, masterWorldPosition.y, masterWorldPosition.z));
                            masterMap.Add(
                                kvp.Key,
                                new
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
                                        screenPoint = new ScreenPointValue { top = Screen.height - masterScreenPoint.y, left = masterScreenPoint.x },
                                        laneRules = masterLaneRulesValue.Value,
                                    },
                                    lanes = laneList,
                                }
                            );
                        }
                        return JsonConvert.SerializeObject(masterMap);
                    }
                )
            );
            AddBinding(
                m_GetSelectedLaneIndexBinding = new GetterValueBinding<string>(
                    "RoadRule",
                    "GetSelectedLaneIndex",
                    () =>
                    {
                        return JsonConvert.SerializeObject(SelectedLaneIndexSet);
                    }
                )
            );
            AddBinding(
                m_GetSelectedEdgeBinding = new GetterValueBinding<string>(
                    "RoadRule",
                    "GetSelectedEdge",
                    () =>
                    {
                        var selectedEdgeList = new List<object>();
                        var camera = m_CameraUpdateSystem.activeCamera;
                        if (camera == null)
                        {
                            return JsonConvert.SerializeObject(selectedEdgeList);
                        }
                        foreach (var edgeEntity in SelectedEdgeEntityList)
                        {
                            if (!EntityManager.TryGetComponent<EdgeGeometry>(edgeEntity, out var edgeGeometry))
                            {
                                continue;
                            }
                            var worldPosition = math.lerp(edgeGeometry.m_Bounds.min, edgeGeometry.m_Bounds.max, 0.5f);
                            var screenPoint = camera.WorldToScreenPoint(new Vector3(worldPosition.x, worldPosition.y, worldPosition.z));
                            selectedEdgeList.Add(
                                new
                                {
                                    edgeEntity = new { index = edgeEntity.Index, version = edgeEntity.Version },
                                    position = new
                                    {
                                        x = worldPosition.x,
                                        y = worldPosition.y,
                                        z = worldPosition.z,
                                    },
                                    screenPoint = new { top = Screen.height - screenPoint.y, left = screenPoint.x },
                                }
                            );
                        }
                        return JsonConvert.SerializeObject(selectedEdgeList);
                    }
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
                            }
                        }
                        else
                        {
                            return "";
                        }

                        m_GetLanesBinding.Update();
                        m_ObsoleteMarkerSystem.UpdateAll();
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
                        AddSelectedLaneIndex(inputValue);
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
                        RemoveSelectedLaneIndex(inputValue);
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
