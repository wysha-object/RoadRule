using System;
using Game.Prefabs;
using Game.Vehicles;
using RoadRule.Components;
using Unity.Entities;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace RoadRule.Utils
{
    public static class LaneRulesUtils
    {
        [Flags]
        public enum VehicleTypeFlags : ushort
        {
            None = 0,
            Ambulance = 1 << 0,
            DeliveryTruck = 1 << 1,
            FireEngine = 1 << 2,
            GarbageTruck = 1 << 3,
            Hearse = 1 << 4,
            MaintenanceVehicle = 1 << 5,
            PersonalCar = 1 << 6,
            PoliceCar = 1 << 7,
            PostVan = 1 << 8,
            PublicTransport = 1 << 9,
        }

        public static bool CheckLaneRules(
            LaneRules laneRules,
            Entity carEntity,
            ComponentLookup<Car> carLookup,
            ComponentLookup<PrefabRef> prefabRefLookup,
            ComponentLookup<CarData> carDataLookup,
            ComponentLookup<Game.Vehicles.Ambulance> ambulanceLookup,
            ComponentLookup<Game.Vehicles.DeliveryTruck> deliveryTruckLookup,
            ComponentLookup<Game.Vehicles.FireEngine> fireEngineLookup,
            ComponentLookup<Game.Vehicles.GarbageTruck> garbageTruckLookup,
            ComponentLookup<Game.Vehicles.Hearse> hearseLookup,
            ComponentLookup<Game.Vehicles.MaintenanceVehicle> maintenanceVehicleLookup,
            ComponentLookup<Game.Vehicles.PersonalCar> personalCarLookup,
            ComponentLookup<Game.Vehicles.PoliceCar> policeCarLookup,
            ComponentLookup<Game.Vehicles.PostVan> postVanLookup,
            ComponentLookup<Game.Vehicles.PublicTransport> publicTransportLookup,
            out bool isPrefer,
            out bool isForbidden
        )
        {
            if (
                !carLookup.TryGetComponent(carEntity, out var car)
                || !prefabRefLookup.TryGetComponent(carEntity, out var prefabRef)
                || !carDataLookup.TryGetComponent(prefabRef.m_Prefab, out var carData)
            )
            {
                isPrefer = false;
                isForbidden = false;
                return false;
            }

            var carFlags = car.m_Flags;

            var sizeClass = carData.m_SizeClass;
            var energyTypes = carData.m_EnergyType;

            var vehicleTypeFlags = VehicleTypeFlags.None;
            if (ambulanceLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.Ambulance;
            if (deliveryTruckLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.DeliveryTruck;
            if (fireEngineLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.FireEngine;
            if (garbageTruckLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.GarbageTruck;
            if (hearseLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.Hearse;
            if (maintenanceVehicleLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.MaintenanceVehicle;
            if (personalCarLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.PersonalCar;
            if (policeCarLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.PoliceCar;
            if (postVanLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.PostVan;
            if (publicTransportLookup.HasComponent(carEntity))
                vehicleTypeFlags |= VehicleTypeFlags.PublicTransport;

            isPrefer = IsPrefer(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags);
            isForbidden = IsForbidden(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags);
            return true;
        }

        public static bool IsPrefer(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes, VehicleTypeFlags vehicleTypeFlags)
        {
            if (IsForbidden(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags))
            {
                return false;
            }

            bool isPreferCarFlags = IsPrefer((int)CarFlags.Emergency, laneRules.m_CarFlagsRules.m_Emergency, (int)carFlags);

            bool isPreferSizeClass =
                IsPrefer(laneRules.m_SizeClassRules.m_Small, sizeClass == SizeClass.Small)
                || IsPrefer(laneRules.m_SizeClassRules.m_Medium, sizeClass == SizeClass.Medium)
                || IsPrefer(laneRules.m_SizeClassRules.m_Large, sizeClass == SizeClass.Large)
                || IsPrefer(laneRules.m_SizeClassRules.m_Undefined, sizeClass == SizeClass.Undefined);

            bool isPreferEnergyTypes =
                IsPrefer(laneRules.m_EnergyTypesRules.m_Fuel, energyTypes == EnergyTypes.Fuel)
                || IsPrefer(laneRules.m_EnergyTypesRules.m_Electricity, energyTypes == EnergyTypes.Electricity)
                || IsPrefer(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, energyTypes == EnergyTypes.FuelAndElectricity)
                || IsPrefer(laneRules.m_EnergyTypesRules.m_None, energyTypes == EnergyTypes.None);

            bool isPreferVehicleType =
                IsPrefer((int)VehicleTypeFlags.Ambulance, laneRules.m_VehicleType.m_Ambulance, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.DeliveryTruck, laneRules.m_VehicleType.m_DeliveryTruck, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.FireEngine, laneRules.m_VehicleType.m_FireEngine, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.GarbageTruck, laneRules.m_VehicleType.m_GarbageTruck, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.Hearse, laneRules.m_VehicleType.m_Hearse, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.MaintenanceVehicle, laneRules.m_VehicleType.m_MaintenanceVehicle, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.PersonalCar, laneRules.m_VehicleType.m_PersonalCar, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.PoliceCar, laneRules.m_VehicleType.m_PoliceCar, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.PostVan, laneRules.m_VehicleType.m_PostVan, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.PublicTransport, laneRules.m_VehicleType.m_PublicTransport, (int)vehicleTypeFlags);

            return isPreferCarFlags || isPreferSizeClass || isPreferEnergyTypes || isPreferVehicleType;
        }

        public static bool IsForbidden(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes, VehicleTypeFlags vehicleTypeFlags)
        {
            bool isForbiddenCarFlags = IsForbidden((int)CarFlags.Emergency, laneRules.m_CarFlagsRules.m_Emergency, (int)carFlags);

            bool isForbiddenSizeClass =
                IsForbidden(laneRules.m_SizeClassRules.m_Small, sizeClass == SizeClass.Small)
                || IsForbidden(laneRules.m_SizeClassRules.m_Medium, sizeClass == SizeClass.Medium)
                || IsForbidden(laneRules.m_SizeClassRules.m_Large, sizeClass == SizeClass.Large)
                || IsForbidden(laneRules.m_SizeClassRules.m_Undefined, sizeClass == SizeClass.Undefined);

            bool isForbiddenEnergyTypes =
                IsForbidden(laneRules.m_EnergyTypesRules.m_Fuel, energyTypes == EnergyTypes.Fuel)
                || IsForbidden(laneRules.m_EnergyTypesRules.m_Electricity, energyTypes == EnergyTypes.Electricity)
                || IsForbidden(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, energyTypes == EnergyTypes.FuelAndElectricity)
                || IsForbidden(laneRules.m_EnergyTypesRules.m_None, energyTypes == EnergyTypes.None);

            bool isForbiddenVehicleType =
                IsForbidden((int)VehicleTypeFlags.Ambulance, laneRules.m_VehicleType.m_Ambulance, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.DeliveryTruck, laneRules.m_VehicleType.m_DeliveryTruck, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.FireEngine, laneRules.m_VehicleType.m_FireEngine, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.GarbageTruck, laneRules.m_VehicleType.m_GarbageTruck, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.Hearse, laneRules.m_VehicleType.m_Hearse, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.MaintenanceVehicle, laneRules.m_VehicleType.m_MaintenanceVehicle, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.PersonalCar, laneRules.m_VehicleType.m_PersonalCar, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.PoliceCar, laneRules.m_VehicleType.m_PoliceCar, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.PostVan, laneRules.m_VehicleType.m_PostVan, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.PublicTransport, laneRules.m_VehicleType.m_PublicTransport, (int)vehicleTypeFlags);

            return isForbiddenCarFlags || isForbiddenSizeClass || isForbiddenEnergyTypes || isForbiddenVehicleType;
        }

        public static bool IsPrefer(int flag, LaneRules.Rule rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsPrefer(rule, value);
        }

        public static bool IsForbidden(int flag, LaneRules.Rule rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsForbidden(rule, value);
        }

        public static bool IsPrefer(LaneRules.Rule rule, bool value)
        {
            if (IsForbidden(rule, value))
            {
                return false;
            }
            switch (rule)
            {
                case LaneRules.Rule.None:
                    return false;
                case LaneRules.Rule.PreferOrNone:
                    return value == false;
                case LaneRules.Rule.NoneOrPrefer:
                    return value == true;
                case LaneRules.Rule.ForbiddenOrNone:
                    return false;
                case LaneRules.Rule.NoneOrForbidden:
                    return false;
                case LaneRules.Rule.ForbiddenOrPrefer:
                    return value == true;
                case LaneRules.Rule.PreferOrForbidden:
                    return value == false;
                default:
                    return false;
            }
        }

        public static bool IsForbidden(LaneRules.Rule rule, bool value)
        {
            switch (rule)
            {
                case LaneRules.Rule.None:
                    return false;
                case LaneRules.Rule.PreferOrNone:
                    return false;
                case LaneRules.Rule.NoneOrPrefer:
                    return false;
                case LaneRules.Rule.ForbiddenOrNone:
                    return value == false;
                case LaneRules.Rule.NoneOrForbidden:
                    return value == true;
                case LaneRules.Rule.ForbiddenOrPrefer:
                    return value == false;
                case LaneRules.Rule.PreferOrForbidden:
                    return value == true;
                default:
                    return false;
            }
        }
    }
}
