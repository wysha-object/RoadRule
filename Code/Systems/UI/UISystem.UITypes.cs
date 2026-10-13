using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.UI.Binding;
using Game.Net;
using Game.Vehicles;
using RoadRule.Components;
using UnityEngine;
using UnityEngine.Playables;
using static RoadRule.Settings;
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

        private struct RuleOptionsValue : IJsonReadable, IJsonWritable
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

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(this.noFlag));
                reader.Read(out int noFlag);
                this.noFlag = (RuleValue)noFlag;
                reader.ReadProperty(nameof(this.hasFlag));
                reader.Read(out int hasFlag);
                this.hasFlag = (RuleValue)hasFlag;
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(noFlag));
                writer.Write((int)noFlag);
                writer.PropertyName(nameof(hasFlag));
                writer.Write((int)hasFlag);
                writer.TypeEnd();
            }
        }

        public enum FieldState
        {
            Applied = 0,
            PartiallyApplied = 1,
        }

        public struct FieldValue<T> : IJsonReadable, IJsonWritable
        {
            public FieldState state;
            public T value;

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(this.state));
                reader.Read(out int state);
                reader.ReadProperty(nameof(this.value));
                this.state = (FieldState)state;
                if (value is IJsonReadable readableValue)
                {
                    readableValue.Read(reader);
                    value = (T)readableValue;
                }
                else if (value is float)
                {
                    reader.Read(out float floatValue);
                    value = (T)(object)floatValue;
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported UI field value type: {typeof(T).FullName}");
                }
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(state));
                writer.Write((int)state);
                writer.PropertyName(nameof(value));
                if (value is IJsonWritable writableValue)
                {
                    writer.Write(writableValue);
                }
                else if (value is float floatValue)
                {
                    writer.Write(floatValue);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported UI field value type: {typeof(T).FullName}");
                }
                writer.TypeEnd();
            }
        }

        private struct VehicleTypeRulesValue : IJsonReadable, IJsonWritable
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

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(ambulance));
                ambulance.Read(reader);
                reader.ReadProperty(nameof(deliveryTruck));
                deliveryTruck.Read(reader);
                reader.ReadProperty(nameof(fireEngine));
                fireEngine.Read(reader);
                reader.ReadProperty(nameof(garbageTruck));
                garbageTruck.Read(reader);
                reader.ReadProperty(nameof(hearse));
                hearse.Read(reader);
                reader.ReadProperty(nameof(maintenanceVehicle));
                maintenanceVehicle.Read(reader);
                reader.ReadProperty(nameof(personalCar));
                personalCar.Read(reader);
                reader.ReadProperty(nameof(policeCar));
                policeCar.Read(reader);
                reader.ReadProperty(nameof(postVan));
                postVan.Read(reader);
                reader.ReadProperty(nameof(publicTransport));
                publicTransport.Read(reader);
                reader.ReadProperty(nameof(taxi));
                taxi.Read(reader);
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(ambulance));
                writer.Write(ambulance);
                writer.PropertyName(nameof(deliveryTruck));
                writer.Write(deliveryTruck);
                writer.PropertyName(nameof(fireEngine));
                writer.Write(fireEngine);
                writer.PropertyName(nameof(garbageTruck));
                writer.Write(garbageTruck);
                writer.PropertyName(nameof(hearse));
                writer.Write(hearse);
                writer.PropertyName(nameof(maintenanceVehicle));
                writer.Write(maintenanceVehicle);
                writer.PropertyName(nameof(personalCar));
                writer.Write(personalCar);
                writer.PropertyName(nameof(policeCar));
                writer.Write(policeCar);
                writer.PropertyName(nameof(postVan));
                writer.Write(postVan);
                writer.PropertyName(nameof(publicTransport));
                writer.Write(publicTransport);
                writer.PropertyName(nameof(taxi));
                writer.Write(taxi);
                writer.TypeEnd();
            }
        }

        private struct LaneRulesValue : IJsonReadable, IJsonWritable
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

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(vehicleTypeRules));
                vehicleTypeRules.Read(reader);
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(vehicleTypeRules));
                writer.Write(vehicleTypeRules);
                writer.TypeEnd();
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

        private struct CarLaneValue : IJsonReadable, IJsonWritable
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

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(speedLimit));
                speedLimit.Read(reader);
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(speedLimit));
                writer.Write(speedLimit);
                writer.TypeEnd();
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

        private struct LaneValue : IJsonWritable
        {
            public int laneIndex;
            public PositionValue position;
            public LaneRulesValue laneRules;
            public CarLaneValue carLane;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(laneIndex));
                writer.Write(laneIndex);
                writer.PropertyName(nameof(position));
                writer.Write(position);
                writer.PropertyName(nameof(laneRules));
                writer.Write(laneRules);
                writer.PropertyName(nameof(carLane));
                writer.Write(carLane);
                writer.TypeEnd();
            }
        }

        private struct LaneGroupValue : IJsonWritable
        {
            public LaneValue masterLane;
            public LaneValue[] lanes;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(masterLane));
                writer.Write(masterLane);
                writer.PropertyName(nameof(lanes));
                writer.ArrayBegin(lanes.Length);
                foreach (var lane in lanes)
                {
                    writer.Write(lane);
                }
                writer.ArrayEnd();
                writer.TypeEnd();
            }
        }

        private struct EntityValue : IJsonWritable
        {
            public int index;
            public int version;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(index));
                writer.Write(index);
                writer.PropertyName(nameof(version));
                writer.Write(version);
                writer.TypeEnd();
            }
        }

        private struct EdgeValue : IJsonWritable
        {
            public EntityValue edgeEntity;
            public PositionValue position;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(edgeEntity));
                writer.Write(edgeEntity);
                writer.PropertyName(nameof(position));
                writer.Write(position);
                writer.TypeEnd();
            }
        }

        private struct CompositionValue : IJsonWritable
        {
            public float speedLimit;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(speedLimit));
                writer.Write(speedLimit);
                writer.TypeEnd();
            }
        }

        private struct PositionValue : IJsonReadable, IJsonWritable
        {
            public float x;
            public float y;
            public float z;

            public void Read(IJsonReader reader)
            {
                reader.ReadMapBegin();
                reader.ReadProperty(nameof(x));
                reader.Read(out x);
                reader.ReadProperty(nameof(y));
                reader.Read(out y);
                reader.ReadProperty(nameof(z));
                reader.Read(out z);
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(x));
                writer.Write(x);
                writer.PropertyName(nameof(y));
                writer.Write(y);
                writer.PropertyName(nameof(z));
                writer.Write(z);
                writer.TypeEnd();
            }
        }

        private struct PixelRectValue : IJsonWritable
        {
            public float x;
            public float y;
            public float width;
            public float height;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(x));
                writer.Write(x);
                writer.PropertyName(nameof(y));
                writer.Write(y);
                writer.PropertyName(nameof(width));
                writer.Write(width);
                writer.PropertyName(nameof(height));
                writer.Write(height);
                writer.TypeEnd();
            }
        }

        private struct CameraValue : IJsonWritable
        {
            public float[] worldToCameraMatrix;
            public float[] projectionMatrix;
            public float nearClipPlane;
            public int screenWidth;
            public int screenHeight;
            public PixelRectValue pixelRect;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(worldToCameraMatrix));
                writer.ArrayBegin(worldToCameraMatrix.Length);
                foreach (var value in worldToCameraMatrix)
                {
                    writer.Write(value);
                }
                writer.ArrayEnd();
                writer.PropertyName(nameof(projectionMatrix));
                writer.ArrayBegin(projectionMatrix.Length);
                foreach (var value in projectionMatrix)
                {
                    writer.Write(value);
                }
                writer.ArrayEnd();
                writer.PropertyName(nameof(nearClipPlane));
                writer.Write(nearClipPlane);
                writer.PropertyName(nameof(screenWidth));
                writer.Write(screenWidth);
                writer.PropertyName(nameof(screenHeight));
                writer.Write(screenHeight);
                writer.PropertyName(nameof(pixelRect));
                writer.Write(pixelRect);
                writer.TypeEnd();
            }

            public static CameraValue FromCamera(Camera camera)
            {
                return new CameraValue
                {
                    worldToCameraMatrix = ToRowMajorArray(camera.worldToCameraMatrix),
                    projectionMatrix = ToRowMajorArray(camera.projectionMatrix),
                    nearClipPlane = camera.nearClipPlane,
                    screenWidth = Screen.width,
                    screenHeight = Screen.height,
                    pixelRect = new PixelRectValue
                    {
                        x = camera.pixelRect.x,
                        y = camera.pixelRect.y,
                        width = camera.pixelRect.width,
                        height = camera.pixelRect.height,
                    },
                };
            }

            private static float[] ToRowMajorArray(Matrix4x4 matrix)
            {
                return new[]
                {
                    matrix.m00,
                    matrix.m01,
                    matrix.m02,
                    matrix.m03,
                    matrix.m10,
                    matrix.m11,
                    matrix.m12,
                    matrix.m13,
                    matrix.m20,
                    matrix.m21,
                    matrix.m22,
                    matrix.m23,
                    matrix.m30,
                    matrix.m31,
                    matrix.m32,
                    matrix.m33,
                };
            }
        }

        private struct GetLaneResultItemValue : IJsonWritable
        {
            public LaneValue lane;
            public float defaultSpeedLimit;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(GetType().FullName);
                writer.PropertyName(nameof(lane));
                writer.Write(lane);
                writer.PropertyName(nameof(defaultSpeedLimit));
                writer.Write(defaultSpeedLimit);
                writer.TypeEnd();
            }
        }
    }
}
