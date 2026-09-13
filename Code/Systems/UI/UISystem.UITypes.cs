using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Net;
using Game.Vehicles;
using RoadRule.Components;
using UnityEngine.Playables;
using static RoadRule.Systems.UI.UISystem;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        private enum RuleValue
        {
            None = 0,
            Prefer = 1,
            Forbidden = 2,
            Disallow = 3,
        }

        private struct RuleOptionsValue
        {
            public RuleValue noFlag;
            public RuleValue hasFlag;

            public override bool Equals(object obj)
            {
                return obj is RuleOptionsValue value && noFlag == value.noFlag && hasFlag == value.hasFlag;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(noFlag, hasFlag);
            }
        }

        public enum FieldState
        {
            Applied = 0,
            PartiallyApplied = 1,
        }

        public struct FieldValue<T>
        {
            public FieldState state;
            public T value;
        }

        private struct VehicleTypeRulesValue
        {
            public FieldValue<RuleOptionsValue> ambulance;
            public FieldValue<RuleOptionsValue> deliveryTruck;
            public FieldValue<RuleOptionsValue> fireEngine;
            public FieldValue<RuleOptionsValue> garbageTruck;
            public FieldValue<RuleOptionsValue> hearse;
            public FieldValue<RuleOptionsValue> maintenanceVehicle;
            public FieldValue<RuleOptionsValue> personalCar;
            public FieldValue<RuleOptionsValue> policeCar;
            public FieldValue<RuleOptionsValue> postVan;
            public FieldValue<RuleOptionsValue> publicTransport;
            public FieldValue<RuleOptionsValue> taxi;

            public static VehicleTypeRulesValue FromVehicleTypeRules(LaneRules.VehicleTypeRules vehicleTypeRules)
            {
                return new VehicleTypeRulesValue
                {
                    ambulance = FromRule(vehicleTypeRules.m_Ambulance),
                    deliveryTruck = FromRule(vehicleTypeRules.m_DeliveryTruck),
                    fireEngine = FromRule(vehicleTypeRules.m_FireEngine),
                    garbageTruck = FromRule(vehicleTypeRules.m_GarbageTruck),
                    hearse = FromRule(vehicleTypeRules.m_Hearse),
                    maintenanceVehicle = FromRule(vehicleTypeRules.m_MaintenanceVehicle),
                    personalCar = FromRule(vehicleTypeRules.m_PersonalCar),
                    policeCar = FromRule(vehicleTypeRules.m_PoliceCar),
                    postVan = FromRule(vehicleTypeRules.m_PostVan),
                    publicTransport = FromRule(vehicleTypeRules.m_PublicTransport),
                    taxi = FromRule(vehicleTypeRules.m_Taxi),
                };
            }

            public static LaneRules.VehicleTypeRules ApplyVehicleTypeRulesValue(LaneRules.VehicleTypeRules vehicleTypeRules, VehicleTypeRulesValue vehicleTypeRulesValue)
            {
                return new LaneRules.VehicleTypeRules
                {
                    m_Ambulance = ApplyRuleOptionsValue(vehicleTypeRules.m_Ambulance, vehicleTypeRulesValue.ambulance),
                    m_DeliveryTruck = ApplyRuleOptionsValue(vehicleTypeRules.m_DeliveryTruck, vehicleTypeRulesValue.deliveryTruck),
                    m_FireEngine = ApplyRuleOptionsValue(vehicleTypeRules.m_FireEngine, vehicleTypeRulesValue.fireEngine),
                    m_GarbageTruck = ApplyRuleOptionsValue(vehicleTypeRules.m_GarbageTruck, vehicleTypeRulesValue.garbageTruck),
                    m_Hearse = ApplyRuleOptionsValue(vehicleTypeRules.m_Hearse, vehicleTypeRulesValue.hearse),
                    m_MaintenanceVehicle = ApplyRuleOptionsValue(vehicleTypeRules.m_MaintenanceVehicle, vehicleTypeRulesValue.maintenanceVehicle),
                    m_PersonalCar = ApplyRuleOptionsValue(vehicleTypeRules.m_PersonalCar, vehicleTypeRulesValue.personalCar),
                    m_PoliceCar = ApplyRuleOptionsValue(vehicleTypeRules.m_PoliceCar, vehicleTypeRulesValue.policeCar),
                    m_PostVan = ApplyRuleOptionsValue(vehicleTypeRules.m_PostVan, vehicleTypeRulesValue.postVan),
                    m_PublicTransport = ApplyRuleOptionsValue(vehicleTypeRules.m_PublicTransport, vehicleTypeRulesValue.publicTransport),
                    m_Taxi = ApplyRuleOptionsValue(vehicleTypeRules.m_Taxi, vehicleTypeRulesValue.taxi),
                };
            }

            public static VehicleTypeRulesValue MergeVehicleTypeRules(VehicleTypeRulesValue a, VehicleTypeRulesValue b)
            {
                return new VehicleTypeRulesValue
                {
                    ambulance = MergeRuleValues(a.ambulance, b.ambulance),
                    deliveryTruck = MergeRuleValues(a.deliveryTruck, b.deliveryTruck),
                    fireEngine = MergeRuleValues(a.fireEngine, b.fireEngine),
                    garbageTruck = MergeRuleValues(a.garbageTruck, b.garbageTruck),
                    hearse = MergeRuleValues(a.hearse, b.hearse),
                    maintenanceVehicle = MergeRuleValues(a.maintenanceVehicle, b.maintenanceVehicle),
                    personalCar = MergeRuleValues(a.personalCar, b.personalCar),
                    policeCar = MergeRuleValues(a.policeCar, b.policeCar),
                    postVan = MergeRuleValues(a.postVan, b.postVan),
                    publicTransport = MergeRuleValues(a.publicTransport, b.publicTransport),
                    taxi = MergeRuleValues(a.taxi, b.taxi),
                };
            }
        }

        private struct LaneRulesValue
        {
            public VehicleTypeRulesValue vehicleTypeRules;

            public static LaneRulesValue FromRules(LaneRules rules)
            {
                return new LaneRulesValue { vehicleTypeRules = VehicleTypeRulesValue.FromVehicleTypeRules(rules.m_VehicleType) };
            }

            public static LaneRules ApplyRulesValue(LaneRules laneRules, LaneRulesValue rulesValue)
            {
                return new LaneRules { m_VehicleType = VehicleTypeRulesValue.ApplyVehicleTypeRulesValue(laneRules.m_VehicleType, rulesValue.vehicleTypeRules) };
            }

            public static LaneRulesValue MergeRulesValues(LaneRulesValue a, LaneRulesValue b)
            {
                return new LaneRulesValue { vehicleTypeRules = VehicleTypeRulesValue.MergeVehicleTypeRules(a.vehicleTypeRules, b.vehicleTypeRules) };
            }
        }

        private static FieldValue<RuleOptionsValue> FromRule(LaneRules.RuleOptions rule)
        {
            var noFlag = RuleValue.None;
            switch (rule & LaneRules.RuleOptions.NoFlagRuleMask)
            {
                case LaneRules.RuleOptions.None:
                    noFlag = RuleValue.None;
                    break;
                case LaneRules.RuleOptions.NoFlagPrefer:
                    noFlag = RuleValue.Prefer;
                    break;
                case LaneRules.RuleOptions.NoFlagForbidden:
                    noFlag = RuleValue.Forbidden;
                    break;
                case LaneRules.RuleOptions.NoFlagDisallow:
                    noFlag = RuleValue.Disallow;
                    break;
                default:
                    noFlag = RuleValue.None;
                    break;
            }

            var hasFlag = RuleValue.None;
            switch (rule & LaneRules.RuleOptions.HasFlagRuleMask)
            {
                case LaneRules.RuleOptions.None:
                    hasFlag = RuleValue.None;
                    break;
                case LaneRules.RuleOptions.HasFlagPrefer:
                    hasFlag = RuleValue.Prefer;
                    break;
                case LaneRules.RuleOptions.HasFlagForbidden:
                    hasFlag = RuleValue.Forbidden;
                    break;
                case LaneRules.RuleOptions.HasFlagDisallow:
                    hasFlag = RuleValue.Disallow;
                    break;
                default:
                    hasFlag = RuleValue.None;
                    break;
            }

            return new FieldValue<RuleOptionsValue>
            {
                state = FieldState.Applied,
                value = new RuleOptionsValue { noFlag = noFlag, hasFlag = hasFlag },
            };
        }

        private static LaneRules.RuleOptions ToRuleOptions(RuleOptionsValue ruleOptionsValue)
        {
            LaneRules.RuleOptions rule = LaneRules.RuleOptions.None;
            switch (ruleOptionsValue.noFlag)
            {
                case RuleValue.Prefer:
                    rule |= LaneRules.RuleOptions.NoFlagPrefer;
                    break;
                case RuleValue.Forbidden:
                    rule |= LaneRules.RuleOptions.NoFlagForbidden;
                    break;
                case RuleValue.Disallow:
                    rule |= LaneRules.RuleOptions.NoFlagDisallow;
                    break;
            }
            switch (ruleOptionsValue.hasFlag)
            {
                case RuleValue.Prefer:
                    rule |= LaneRules.RuleOptions.HasFlagPrefer;
                    break;
                case RuleValue.Forbidden:
                    rule |= LaneRules.RuleOptions.HasFlagForbidden;
                    break;
                case RuleValue.Disallow:
                    rule |= LaneRules.RuleOptions.HasFlagDisallow;
                    break;
            }
            return rule;
        }

        private static LaneRules.RuleOptions ApplyRuleOptionsValue(LaneRules.RuleOptions ruleOptions, FieldValue<RuleOptionsValue> ruleOptionsValue)
        {
            if (ruleOptionsValue.state == FieldState.Applied)
            {
                return ToRuleOptions(ruleOptionsValue.value);
            }
            else
            {
                return ruleOptions;
            }
        }

        private static FieldValue<RuleOptionsValue> MergeRuleValues(FieldValue<RuleOptionsValue> a, FieldValue<RuleOptionsValue> b)
        {
            if (a.value.Equals(b.value) && a.state == b.state && a.state == FieldState.Applied)
            {
                return new FieldValue<RuleOptionsValue> { state = FieldState.Applied, value = a.value };
            }
            else
            {
                var noFlag = (RuleValue)Math.Max((int)a.value.noFlag, (int)b.value.noFlag);
                var hasFlag = (RuleValue)Math.Max((int)a.value.hasFlag, (int)b.value.hasFlag);
                var mergedRule = new RuleOptionsValue { noFlag = noFlag, hasFlag = hasFlag };
                return new FieldValue<RuleOptionsValue> { state = FieldState.PartiallyApplied, value = mergedRule };
            }
        }

        private struct CarLaneValue
        {
            /// Game lane speed uses 2x m/s; km/h converts by / 1.8.
            public FieldValue<float> speedLimit;

            public static CarLaneValue FromCarLane(CarLane carLane)
            {
                return new CarLaneValue { speedLimit = FromSpeedLimit(carLane.m_DefaultSpeedLimit) };
            }

            public static CarLane ApplyLanePropertyValue(CarLane carLane, CarLaneValue lanePropertyValue)
            {
                carLane.m_SpeedLimit = lanePropertyValue.speedLimit.state == FieldState.Applied ? lanePropertyValue.speedLimit.value / 1.8f : carLane.m_SpeedLimit;
                carLane.m_DefaultSpeedLimit = lanePropertyValue.speedLimit.state == FieldState.Applied ? lanePropertyValue.speedLimit.value / 1.8f : carLane.m_DefaultSpeedLimit;
                return carLane;
            }

            public static CarLaneValue MergeLanePropertyValues(CarLaneValue a, CarLaneValue b)
            {
                return new CarLaneValue { speedLimit = MergeSpeedLimitValues(a.speedLimit, b.speedLimit) };
            }
        }

        private static FieldValue<float> FromSpeedLimit(float speedLimit)
        {
            return new FieldValue<float> { state = FieldState.Applied, value = speedLimit * 1.8f };
        }

        private static FieldValue<float> MergeSpeedLimitValues(FieldValue<float> a, FieldValue<float> b)
        {
            if (a.value == b.value && a.state == b.state && a.state == FieldState.Applied)
            {
                return new FieldValue<float> { state = FieldState.Applied, value = a.value };
            }
            else
            {
                var mergedSpeedLimit = Math.Min(a.value, b.value);
                return new FieldValue<float> { state = FieldState.PartiallyApplied, value = mergedSpeedLimit };
            }
        }

        private struct LaneValue
        {
            public int laneIndex;
            public PositionValue position;
            public ScreenPointValue screenPoint;
            public LaneRulesValue laneRules;
            public CarLaneValue carLane;
        }

        private struct PositionValue
        {
            public float x;
            public float y;
            public float z;
        }

        private struct ScreenPointValue
        {
            public float top;
            public float left;
        }
    }
}
