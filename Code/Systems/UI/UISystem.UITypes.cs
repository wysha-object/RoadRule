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

        private struct CarFlagsRulesValue
        {
            public FieldValue<LaneRules.Rule> emergency;

            public static CarFlagsRulesValue FromCarFlagsRules(LaneRules.CarFlagsRules carFlagsRules)
            {
                return new CarFlagsRulesValue { emergency = FromRule(carFlagsRules.m_Emergency) };
            }

            public static LaneRules.CarFlagsRules ApplyCarFlagsRulesValue(LaneRules.CarFlagsRules carFlagsRules, CarFlagsRulesValue carFlagsRulesValue)
            {
                return new LaneRules.CarFlagsRules
                {
                    m_Emergency = carFlagsRulesValue.emergency.state == FieldState.Applied ? carFlagsRulesValue.emergency.value : carFlagsRules.m_Emergency,
                };
            }

            public static CarFlagsRulesValue MergeCarFlagsRules(CarFlagsRulesValue a, CarFlagsRulesValue b)
            {
                return new CarFlagsRulesValue { emergency = MergeRuleValues(a.emergency, b.emergency) };
            }
        }

        private struct SizeClassRulesValue
        {
            public FieldValue<LaneRules.Rule> small;
            public FieldValue<LaneRules.Rule> medium;
            public FieldValue<LaneRules.Rule> large;
            public FieldValue<LaneRules.Rule> undefined;

            public static SizeClassRulesValue FromSizeClassRules(LaneRules.SizeClassRules sizeClassRules)
            {
                return new SizeClassRulesValue
                {
                    small = FromRule(sizeClassRules.m_Small),
                    medium = FromRule(sizeClassRules.m_Medium),
                    large = FromRule(sizeClassRules.m_Large),
                    undefined = FromRule(sizeClassRules.m_Undefined),
                };
            }

            public static LaneRules.SizeClassRules ApplySizeClassRulesValue(LaneRules.SizeClassRules sizeClassRules, SizeClassRulesValue sizeClassFlagsRulesValue)
            {
                return new LaneRules.SizeClassRules
                {
                    m_Small = sizeClassFlagsRulesValue.small.state == FieldState.Applied ? sizeClassFlagsRulesValue.small.value : sizeClassRules.m_Small,
                    m_Medium = sizeClassFlagsRulesValue.medium.state == FieldState.Applied ? sizeClassFlagsRulesValue.medium.value : sizeClassRules.m_Medium,
                    m_Large = sizeClassFlagsRulesValue.large.state == FieldState.Applied ? sizeClassFlagsRulesValue.large.value : sizeClassRules.m_Large,
                    m_Undefined = sizeClassFlagsRulesValue.undefined.state == FieldState.Applied ? sizeClassFlagsRulesValue.undefined.value : sizeClassRules.m_Undefined,
                };
            }

            public static SizeClassRulesValue MergeSizeClassRules(SizeClassRulesValue a, SizeClassRulesValue b)
            {
                return new SizeClassRulesValue
                {
                    small = MergeRuleValues(a.small, b.small),
                    medium = MergeRuleValues(a.medium, b.medium),
                    large = MergeRuleValues(a.large, b.large),
                    undefined = MergeRuleValues(a.undefined, b.undefined),
                };
            }
        }

        private struct EnergyTypesRulesValue
        {
            public FieldValue<LaneRules.Rule> fuel;
            public FieldValue<LaneRules.Rule> electricity;
            public FieldValue<LaneRules.Rule> fuelAndElectricity;
            public FieldValue<LaneRules.Rule> none;

            public static EnergyTypesRulesValue FromEnergyTypesRules(LaneRules.EnergyTypesRules energyTypesRules)
            {
                return new EnergyTypesRulesValue
                {
                    fuel = FromRule(energyTypesRules.m_Fuel),
                    electricity = FromRule(energyTypesRules.m_Electricity),
                    fuelAndElectricity = FromRule(energyTypesRules.m_FuelAndElectricity),
                    none = FromRule(energyTypesRules.m_None),
                };
            }

            public static LaneRules.EnergyTypesRules ApplyEnergyTypesRulesValue(LaneRules.EnergyTypesRules energyTypesRules, EnergyTypesRulesValue energyTypesFlagsRulesValue)
            {
                return new LaneRules.EnergyTypesRules
                {
                    m_Fuel = energyTypesFlagsRulesValue.fuel.state == FieldState.Applied ? energyTypesFlagsRulesValue.fuel.value : energyTypesRules.m_Fuel,
                    m_Electricity =
                        energyTypesFlagsRulesValue.electricity.state == FieldState.Applied ? energyTypesFlagsRulesValue.electricity.value : energyTypesRules.m_Electricity,
                    m_FuelAndElectricity =
                        energyTypesFlagsRulesValue.fuelAndElectricity.state == FieldState.Applied
                            ? energyTypesFlagsRulesValue.fuelAndElectricity.value
                            : energyTypesRules.m_FuelAndElectricity,
                    m_None = energyTypesFlagsRulesValue.none.state == FieldState.Applied ? energyTypesFlagsRulesValue.none.value : energyTypesRules.m_None,
                };
            }

            public static EnergyTypesRulesValue MergeEnergyTypesRules(EnergyTypesRulesValue a, EnergyTypesRulesValue b)
            {
                return new EnergyTypesRulesValue
                {
                    fuel = MergeRuleValues(a.fuel, b.fuel),
                    electricity = MergeRuleValues(a.electricity, b.electricity),
                    fuelAndElectricity = MergeRuleValues(a.fuelAndElectricity, b.fuelAndElectricity),
                    none = MergeRuleValues(a.none, b.none),
                };
            }
        }

        private struct VehicleTypeRulesValue
        {
            public FieldValue<LaneRules.Rule> ambulance;
            public FieldValue<LaneRules.Rule> deliveryTruck;
            public FieldValue<LaneRules.Rule> fireEngine;
            public FieldValue<LaneRules.Rule> garbageTruck;
            public FieldValue<LaneRules.Rule> hearse;
            public FieldValue<LaneRules.Rule> maintenanceVehicle;
            public FieldValue<LaneRules.Rule> personalCar;
            public FieldValue<LaneRules.Rule> policeCar;
            public FieldValue<LaneRules.Rule> postVan;
            public FieldValue<LaneRules.Rule> publicTransport;
            public FieldValue<LaneRules.Rule> taxi;

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
                    m_Ambulance = vehicleTypeRulesValue.ambulance.state == FieldState.Applied ? vehicleTypeRulesValue.ambulance.value : vehicleTypeRules.m_Ambulance,
                    m_DeliveryTruck =
                        vehicleTypeRulesValue.deliveryTruck.state == FieldState.Applied ? vehicleTypeRulesValue.deliveryTruck.value : vehicleTypeRules.m_DeliveryTruck,
                    m_FireEngine = vehicleTypeRulesValue.fireEngine.state == FieldState.Applied ? vehicleTypeRulesValue.fireEngine.value : vehicleTypeRules.m_FireEngine,
                    m_GarbageTruck = vehicleTypeRulesValue.garbageTruck.state == FieldState.Applied ? vehicleTypeRulesValue.garbageTruck.value : vehicleTypeRules.m_GarbageTruck,
                    m_Hearse = vehicleTypeRulesValue.hearse.state == FieldState.Applied ? vehicleTypeRulesValue.hearse.value : vehicleTypeRules.m_Hearse,
                    m_MaintenanceVehicle =
                        vehicleTypeRulesValue.maintenanceVehicle.state == FieldState.Applied
                            ? vehicleTypeRulesValue.maintenanceVehicle.value
                            : vehicleTypeRules.m_MaintenanceVehicle,
                    m_PersonalCar = vehicleTypeRulesValue.personalCar.state == FieldState.Applied ? vehicleTypeRulesValue.personalCar.value : vehicleTypeRules.m_PersonalCar,
                    m_PoliceCar = vehicleTypeRulesValue.policeCar.state == FieldState.Applied ? vehicleTypeRulesValue.policeCar.value : vehicleTypeRules.m_PoliceCar,
                    m_PostVan = vehicleTypeRulesValue.postVan.state == FieldState.Applied ? vehicleTypeRulesValue.postVan.value : vehicleTypeRules.m_PostVan,
                    m_PublicTransport =
                        vehicleTypeRulesValue.publicTransport.state == FieldState.Applied ? vehicleTypeRulesValue.publicTransport.value : vehicleTypeRules.m_PublicTransport,
                    m_Taxi = vehicleTypeRulesValue.taxi.state == FieldState.Applied ? vehicleTypeRulesValue.taxi.value : vehicleTypeRules.m_Taxi,
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
            public CarFlagsRulesValue carFlagsRules;
            public SizeClassRulesValue sizeClassRules;
            public EnergyTypesRulesValue energyTypesRules;
            public VehicleTypeRulesValue vehicleTypeRules;

            public static LaneRulesValue FromRules(LaneRules rules)
            {
                return new LaneRulesValue
                {
                    carFlagsRules = CarFlagsRulesValue.FromCarFlagsRules(rules.m_CarFlagsRules),
                    sizeClassRules = SizeClassRulesValue.FromSizeClassRules(rules.m_SizeClassRules),
                    energyTypesRules = EnergyTypesRulesValue.FromEnergyTypesRules(rules.m_EnergyTypesRules),
                    vehicleTypeRules = VehicleTypeRulesValue.FromVehicleTypeRules(rules.m_VehicleType),
                };
            }

            public static LaneRules ApplyRulesValue(LaneRules laneRules, LaneRulesValue rulesValue)
            {
                return new LaneRules
                {
                    m_CarFlagsRules = CarFlagsRulesValue.ApplyCarFlagsRulesValue(laneRules.m_CarFlagsRules, rulesValue.carFlagsRules),
                    m_SizeClassRules = SizeClassRulesValue.ApplySizeClassRulesValue(laneRules.m_SizeClassRules, rulesValue.sizeClassRules),
                    m_EnergyTypesRules = EnergyTypesRulesValue.ApplyEnergyTypesRulesValue(laneRules.m_EnergyTypesRules, rulesValue.energyTypesRules),
                    m_VehicleType = VehicleTypeRulesValue.ApplyVehicleTypeRulesValue(laneRules.m_VehicleType, rulesValue.vehicleTypeRules),
                };
            }

            public static LaneRulesValue MergeRulesValues(LaneRulesValue a, LaneRulesValue b)
            {
                return new LaneRulesValue
                {
                    carFlagsRules = CarFlagsRulesValue.MergeCarFlagsRules(a.carFlagsRules, b.carFlagsRules),
                    sizeClassRules = SizeClassRulesValue.MergeSizeClassRules(a.sizeClassRules, b.sizeClassRules),
                    energyTypesRules = EnergyTypesRulesValue.MergeEnergyTypesRules(a.energyTypesRules, b.energyTypesRules),
                    vehicleTypeRules = VehicleTypeRulesValue.MergeVehicleTypeRules(a.vehicleTypeRules, b.vehicleTypeRules),
                };
            }
        }

        private static FieldValue<LaneRules.Rule> FromRule(LaneRules.Rule rule)
        {
            return new FieldValue<LaneRules.Rule> { state = FieldState.Applied, value = rule };
        }

        private static FieldValue<LaneRules.Rule> MergeRuleValues(FieldValue<LaneRules.Rule> a, FieldValue<LaneRules.Rule> b)
        {
            if (a.value == b.value && a.state == b.state && a.state == FieldState.Applied)
            {
                return new FieldValue<LaneRules.Rule> { state = FieldState.Applied, value = a.value };
            }
            else
            {
                var mergedRule = (LaneRules.Rule)Math.Max((int)a.value, (int)b.value);
                return new FieldValue<LaneRules.Rule> { state = FieldState.PartiallyApplied, value = mergedRule };
            }
        }

        private struct CarLaneValue
        {
            public FieldValue<float> speedLimit;
            public FieldValue<float> defaultSpeedLimit;

            public static CarLaneValue FromCarLane(CarLane carLane)
            {
                return new CarLaneValue { speedLimit = FromSpeedLimit(carLane.m_SpeedLimit), defaultSpeedLimit = FromSpeedLimit(carLane.m_DefaultSpeedLimit) };
            }

            public static CarLane ApplyLanePropertyValue(CarLane carLane, CarLaneValue lanePropertyValue)
            {
                carLane.m_SpeedLimit = lanePropertyValue.speedLimit.state == FieldState.Applied ? lanePropertyValue.speedLimit.value : carLane.m_SpeedLimit;
                return carLane;
            }

            public static CarLaneValue MergeLanePropertyValues(CarLaneValue a, CarLaneValue b)
            {
                return new CarLaneValue
                {
                    speedLimit = MergeSpeedLimitValues(a.speedLimit, b.speedLimit),
                    defaultSpeedLimit = MergeSpeedLimitValues(a.defaultSpeedLimit, b.defaultSpeedLimit),
                };
            }
        }

        private static FieldValue<float> FromSpeedLimit(float speedLimit)
        {
            return new FieldValue<float> { state = FieldState.Applied, value = speedLimit };
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
