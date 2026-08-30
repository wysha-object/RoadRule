using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Vehicles;
using RoadRule.Components;
using UnityEngine.Playables;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        public enum RuleState
        {
            Applied = 0,
            PartiallyApplied = 1,
        }

        public struct RuleValue
        {
            public RuleState state;
            public LaneRules.Rule rule;
        }

        private struct CarFlagsRulesValue
        {
            public RuleValue emergency;

            public static CarFlagsRulesValue FromCarFlagsRules(LaneRules.CarFlagsRules carFlagsRules)
            {
                return new CarFlagsRulesValue { emergency = FromRule(carFlagsRules.m_Emergency) };
            }

            public static LaneRules.CarFlagsRules ApplyCarFlagsRulesValue(LaneRules.CarFlagsRules carFlagsRules, CarFlagsRulesValue carFlagsRulesValue)
            {
                return new LaneRules.CarFlagsRules
                {
                    m_Emergency = carFlagsRulesValue.emergency.state == RuleState.Applied ? carFlagsRulesValue.emergency.rule : carFlagsRules.m_Emergency,
                };
            }

            public static CarFlagsRulesValue MergeCarFlagsRules(CarFlagsRulesValue a, CarFlagsRulesValue b)
            {
                return new CarFlagsRulesValue { emergency = MergeRuleValues(a.emergency, b.emergency) };
            }
        }

        private struct SizeClassRulesValue
        {
            public RuleValue small;
            public RuleValue medium;
            public RuleValue large;
            public RuleValue undefined;

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
                    m_Small = sizeClassFlagsRulesValue.small.state == RuleState.Applied ? sizeClassFlagsRulesValue.small.rule : sizeClassRules.m_Small,
                    m_Medium = sizeClassFlagsRulesValue.medium.state == RuleState.Applied ? sizeClassFlagsRulesValue.medium.rule : sizeClassRules.m_Medium,
                    m_Large = sizeClassFlagsRulesValue.large.state == RuleState.Applied ? sizeClassFlagsRulesValue.large.rule : sizeClassRules.m_Large,
                    m_Undefined = sizeClassFlagsRulesValue.undefined.state == RuleState.Applied ? sizeClassFlagsRulesValue.undefined.rule : sizeClassRules.m_Undefined,
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
            public RuleValue fuel;
            public RuleValue electricity;
            public RuleValue fuelAndElectricity;
            public RuleValue none;

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
                    m_Fuel = energyTypesFlagsRulesValue.fuel.state == RuleState.Applied ? energyTypesFlagsRulesValue.fuel.rule : energyTypesRules.m_Fuel,
                    m_Electricity =
                        energyTypesFlagsRulesValue.electricity.state == RuleState.Applied ? energyTypesFlagsRulesValue.electricity.rule : energyTypesRules.m_Electricity,
                    m_FuelAndElectricity =
                        energyTypesFlagsRulesValue.fuelAndElectricity.state == RuleState.Applied
                            ? energyTypesFlagsRulesValue.fuelAndElectricity.rule
                            : energyTypesRules.m_FuelAndElectricity,
                    m_None = energyTypesFlagsRulesValue.none.state == RuleState.Applied ? energyTypesFlagsRulesValue.none.rule : energyTypesRules.m_None,
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
            public RuleValue ambulance;
            public RuleValue deliveryTruck;
            public RuleValue fireEngine;
            public RuleValue garbageTruck;
            public RuleValue hearse;
            public RuleValue maintenanceVehicle;
            public RuleValue personalCar;
            public RuleValue policeCar;
            public RuleValue postVan;
            public RuleValue publicTransport;

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
                };
            }

            public static LaneRules.VehicleTypeRules ApplyVehicleTypeRulesValue(LaneRules.VehicleTypeRules vehicleTypeRules, VehicleTypeRulesValue vehicleTypeRulesValue)
            {
                return new LaneRules.VehicleTypeRules
                {
                    m_Ambulance = vehicleTypeRulesValue.ambulance.state == RuleState.Applied ? vehicleTypeRulesValue.ambulance.rule : vehicleTypeRules.m_Ambulance,
                    m_DeliveryTruck = vehicleTypeRulesValue.deliveryTruck.state == RuleState.Applied ? vehicleTypeRulesValue.deliveryTruck.rule : vehicleTypeRules.m_DeliveryTruck,
                    m_FireEngine = vehicleTypeRulesValue.fireEngine.state == RuleState.Applied ? vehicleTypeRulesValue.fireEngine.rule : vehicleTypeRules.m_FireEngine,
                    m_GarbageTruck = vehicleTypeRulesValue.garbageTruck.state == RuleState.Applied ? vehicleTypeRulesValue.garbageTruck.rule : vehicleTypeRules.m_GarbageTruck,
                    m_Hearse = vehicleTypeRulesValue.hearse.state == RuleState.Applied ? vehicleTypeRulesValue.hearse.rule : vehicleTypeRules.m_Hearse,
                    m_MaintenanceVehicle =
                        vehicleTypeRulesValue.maintenanceVehicle.state == RuleState.Applied ? vehicleTypeRulesValue.maintenanceVehicle.rule : vehicleTypeRules.m_MaintenanceVehicle,
                    m_PersonalCar = vehicleTypeRulesValue.personalCar.state == RuleState.Applied ? vehicleTypeRulesValue.personalCar.rule : vehicleTypeRules.m_PersonalCar,
                    m_PoliceCar = vehicleTypeRulesValue.policeCar.state == RuleState.Applied ? vehicleTypeRulesValue.policeCar.rule : vehicleTypeRules.m_PoliceCar,
                    m_PostVan = vehicleTypeRulesValue.postVan.state == RuleState.Applied ? vehicleTypeRulesValue.postVan.rule : vehicleTypeRules.m_PostVan,
                    m_PublicTransport =
                        vehicleTypeRulesValue.publicTransport.state == RuleState.Applied ? vehicleTypeRulesValue.publicTransport.rule : vehicleTypeRules.m_PublicTransport,
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

        private static RuleValue FromRule(LaneRules.Rule rule)
        {
            return new RuleValue { state = RuleState.Applied, rule = rule };
        }

        private static RuleValue MergeRuleValues(RuleValue a, RuleValue b)
        {
            if (a.rule == b.rule && a.state == b.state && a.state == RuleState.Applied)
            {
                return new RuleValue { state = RuleState.Applied, rule = a.rule };
            }
            else
            {
                var mergedRule = (LaneRules.Rule)Math.Max((int)a.rule, (int)b.rule);
                return new RuleValue { state = RuleState.PartiallyApplied, rule = mergedRule };
            }
        }

        private struct LaneValue
        {
            public int laneIndex;
            public PositionValue position;
            public ScreenPointValue screenPoint;
            public LaneRulesValue laneRules;
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
