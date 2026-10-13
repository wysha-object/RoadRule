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
        private struct UpdateLaneInputValue : IJsonReadable
        {
            public enum UpdateLaneType : int
            {
                LaneRules = 0,
                CarLane = 1,
            }

            public int laneIndex;
            public UpdateLaneType key;
            public object value;

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(laneIndex));
                reader.Read(out laneIndex);
                reader.ReadProperty(nameof(this.key));
                reader.Read(out int key);
                this.key = (UpdateLaneType)key;
                reader.ReadProperty(nameof(value));
                switch (this.key)
                {
                    case UpdateLaneType.LaneRules:
                        var laneRulesValue = new LaneRulesValue();
                        laneRulesValue.Read(reader);
                        value = laneRulesValue;
                        break;
                    case UpdateLaneType.CarLane:
                        var carLaneValue = new CarLaneValue();
                        carLaneValue.Read(reader);
                        value = carLaneValue;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported UpdateLaneType: {this.key}");
                }
                reader.ReadMapEnd();
            }
        }

        private struct LookAtInputValue : IJsonReadable
        {
            public PositionValue position;
            public float distance;

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(position));
                position.Read(reader);
                reader.ReadProperty(nameof(distance));
                reader.Read(out distance);
                reader.ReadMapEnd();
            }
        }

        private ValueBinding<int> m_GetToolStateBinding;
        private ValueBinding<GetLaneResultItemValue[]> m_GetAllLaneGroupBinding;
        private ValueBinding<GetLaneResultItemValue[]> m_GetAllLaneBinding;
        private GetterValueBinding<Dictionary<int, LaneGroupValue>> m_GetLanesBinding;
        private GetterValueBinding<int[]> m_GetSelectedLaneIndexBinding;
        private GetterValueBinding<EdgeValue[]> m_GetSelectedEdgeBinding;
        private GetterValueBinding<CompositionValue> m_GetCompositionBinding;

        private void AddUIBindings()
        {
            AddBinding(m_GetToolStateBinding = new ValueBinding<int>("RoadRule", "GetToolState", (int)ToolState.Disabled));
            AddBinding(m_GetAllLaneGroupBinding = new ValueBinding<GetLaneResultItemValue[]>("RoadRule", "GetAllLaneGroup", [], new ListWriter<GetLaneResultItemValue>()));
            AddBinding(m_GetAllLaneBinding = new ValueBinding<GetLaneResultItemValue[]>("RoadRule", "GetAllLane", [], new ListWriter<GetLaneResultItemValue>()));

            AddBinding(
                m_GetLanesBinding = new GetterValueBinding<Dictionary<int, LaneGroupValue>>("RoadRule", "GetLanes", RoadRuleGetLanes, new DictionaryWriter<int, LaneGroupValue>())
            );
            AddBinding(m_GetSelectedLaneIndexBinding = new GetterValueBinding<int[]>("RoadRule", "GetSelectedLaneIndex", RoadRuleGetSelectedLaneIndex, new ListWriter<int>()));
            AddBinding(m_GetSelectedEdgeBinding = new GetterValueBinding<EdgeValue[]>("RoadRule", "GetSelectedEdge", RoadRuleGetSelectedEdge, new ListWriter<EdgeValue>()));
            AddBinding(m_GetCompositionBinding = new GetterValueBinding<CompositionValue>("RoadRule", "GetComposition", RoadRuleGetComposition));

            AddUpdateBinding(
                new GetterValueBinding<CameraValue?>("RoadRule", "GetCamera", RoadRuleGetCamera, new NullableStructWriter<CameraValue>(new ValueWriter<CameraValue>()))
            );

            AddBinding(new CallBinding<int, int>("RoadRule", "SetToolState", RoadRuleSetToolState));
            AddBinding(new CallBinding<UpdateLaneInputValue, int>("RoadRule", "UpdateLane", RoadRuleUpdateLane));
            AddBinding(new CallBinding<LookAtInputValue, string>("RoadRule", "LookAt", RoadRuleLookAt));
            AddBinding(new CallBinding<int, string>("RoadRule", "AddSelectedLaneIndex", RoadRuleAddSelectedLaneIndex));
            AddBinding(new CallBinding<int, string>("RoadRule", "RemoveSelectedLaneIndex", RoadRuleRemoveSelectedLaneIndex));
            AddBinding(new CallBinding<string, string>("RoadRule", "ClearSelectedLaneIndex", RoadRuleClearSelectedLaneIndex));
        }

        private Dictionary<int, LaneGroupValue> RoadRuleGetLanes()
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
        }

        private int[] RoadRuleGetSelectedLaneIndex()
        {
            return SelectedLaneIndexSet.Select((item) => (int)item).ToArray();
        }

        private EdgeValue[] RoadRuleGetSelectedEdge()
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
        }

        private CompositionValue RoadRuleGetComposition()
        {
            float speedLimit = 60f;
            if (m_CompositionParameters is CompositionParameters composition)
            {
                speedLimit = composition.m_SpeedLimit * 1.8f;
            }
            return new CompositionValue { speedLimit = speedLimit };
        }

        private CameraValue? RoadRuleGetCamera()
        {
            var camera = m_CameraUpdateSystem.activeCamera;
            return camera == null ? null : CameraValue.FromCamera(camera);
        }

        private int RoadRuleSetToolState(int inputValue)
        {
            SetToolState((ToolState)inputValue);
            return 0;
        }

        private int RoadRuleUpdateLane(UpdateLaneInputValue inputValue)
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

            if (inputValue.key == UpdateLaneInputValue.UpdateLaneType.LaneRules && inputValue.value is LaneRulesValue laneRulesValue)
            {
                foreach (var e in laneEntities)
                {
                    if (!EntityManager.TryGetComponent<LaneRules>(e, out var laneRules))
                    {
                        laneRules = new LaneRules();
                        EntityManager.AddComponentData(e, laneRules);
                    }

                    laneRules = LaneRulesValue.ApplyRulesValue(laneRules, laneRulesValue);
                    EntityManager.SetComponentData(e, laneRules);
                    EntityManager.AddComponentData(e, new Updated());
                }
            }
            else if (inputValue.key == UpdateLaneInputValue.UpdateLaneType.CarLane && inputValue.value is CarLaneValue carLaneValue)
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
                    carLane = CarLaneValue.ApplyLanePropertyValue(carLane, carLaneValue);
                    EntityManager.SetComponentData(e, carLane);
                    EntityManager.AddComponentData(e, new Updated());
                }
            }

            m_GetLanesBinding.Update();
            return 0;
        }

        private string RoadRuleLookAt(LookAtInputValue inputValue)
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
