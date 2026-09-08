using System;
using Colossal.Entities;
using Game.Prefabs;
using Game.Simulation;
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
            Taxi = 1 << 10,
        }

        public struct CarParameters
        {
            public bool m_IsValid;
            public CarFlags m_CarFlags;
            public SizeClass m_SizeClass;
            public EnergyTypes m_EnergyTypes;
            public VehicleTypeFlags m_VehicleTypeFlags;

            public CarParameters(CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes, VehicleTypeFlags vehicleTypeFlags)
            {
                this.m_IsValid = false;
                this.m_CarFlags = carFlags;
                this.m_SizeClass = sizeClass;
                this.m_EnergyTypes = energyTypes;
                this.m_VehicleTypeFlags = vehicleTypeFlags;
            }

            public CarParameters()
            {
                this.m_IsValid = false;
                this.m_CarFlags = 0;
                this.m_SizeClass = SizeClass.Undefined;
                this.m_EnergyTypes = EnergyTypes.None;
                this.m_VehicleTypeFlags = VehicleTypeFlags.None;
            }
        }

        /// 当处理`Road`边时, 如果之前没拿到`CarParameters`, 就使用这个默认参数.
        public static readonly CarParameters FALLBACK_CAR_PARAMETERS = new CarParameters(0, SizeClass.Undefined, EnergyTypes.None, VehicleTypeFlags.PersonalCar)
        {
            m_IsValid = true,
        };

        public static readonly CarParameters TAXI_CAR_PARAMETERS = new CarParameters(0, SizeClass.Undefined, EnergyTypes.None, VehicleTypeFlags.Taxi) { m_IsValid = true };

        public static bool GetCarParameters(
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
            ComponentLookup<Game.Vehicles.Taxi> taxiLookup,
            out CarParameters carParameters
        )
        {
            if (
                carLookup.TryGetComponent(carEntity, out var car)
                && prefabRefLookup.TryGetComponent(carEntity, out var prefabRef)
                && carDataLookup.TryGetComponent(prefabRef.m_Prefab, out var carData)
            )
            {
                var vehicleTypeFlags = VehicleTypeFlags.None;
                if (ambulanceLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.Ambulance;
                }
                if (deliveryTruckLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.DeliveryTruck;
                }
                if (fireEngineLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.FireEngine;
                }
                if (garbageTruckLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.GarbageTruck;
                }
                if (hearseLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.Hearse;
                }
                if (maintenanceVehicleLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.MaintenanceVehicle;
                }
                if (personalCarLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.PersonalCar;
                }
                if (policeCarLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.PoliceCar;
                }
                if (postVanLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.PostVan;
                }
                if (publicTransportLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.PublicTransport;
                }
                if (taxiLookup.HasComponent(carEntity))
                {
                    vehicleTypeFlags |= VehicleTypeFlags.Taxi;
                }

                carParameters = new CarParameters(car.m_Flags, carData.m_SizeClass, carData.m_EnergyType, vehicleTypeFlags) { m_IsValid = true };
                return true;
            }

            carParameters = new CarParameters();
            return false;
        }

        public static bool FindCarEntity(
            Entity requestOwner,
            ComponentLookup<Car> carLookup,
            ComponentLookup<PrefabRef> prefabRefLookup,
            ComponentLookup<CarData> carDataLookup,
            ComponentLookup<Game.Creatures.Resident> residentLookup,
            ComponentLookup<Game.Citizens.CarKeeper> carKeeperLookup,
            out Entity carEntity
        )
        {
            if (carLookup.HasComponent(requestOwner) && prefabRefLookup.HasComponent(requestOwner) && carDataLookup.HasComponent(prefabRefLookup[requestOwner].m_Prefab))
            {
                // owner 是 car
                carEntity = requestOwner;
                return true;
            }

            if (carKeeperLookup.TryGetEnabledComponent(requestOwner, out var carKeeper))
            {
                // owner 是 citizen
                carEntity = carKeeper.m_Car;
                return true;
            }

            if (residentLookup.TryGetComponent(requestOwner, out var resident) && carKeeperLookup.TryGetEnabledComponent(resident.m_Citizen, out var residentCitizenCarKeeper))
            {
                // owner 是 resident
                carEntity = residentCitizenCarKeeper.m_Car;
                return true;
            }

            carEntity = Entity.Null;
            return false;
        }

        public static bool FindCarParameters(
            Entity requestOwner,
            ComponentLookup<Car> carLookup,
            ComponentLookup<PrefabRef> prefabRefLookup,
            ComponentLookup<CarData> carDataLookup,
            ComponentLookup<Game.Creatures.Resident> residentLookup,
            ComponentLookup<Game.Citizens.CarKeeper> carKeeperLookup,
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
            ComponentLookup<Game.Vehicles.Taxi> taxiLookup,
            ComponentLookup<EvacuationRequest> evacuationRequestLookup,
            ComponentLookup<FireRescueRequest> fireRescueRequestLookup,
            ComponentLookup<GarbageCollectionRequest> garbageCollectionRequestLookup,
            ComponentLookup<GarbageTransferRequest> garbageTransferRequestLookup,
            ComponentLookup<GoodsDeliveryRequest> goodsDeliveryRequestLookup,
            ComponentLookup<HealthcareRequest> healthcareRequestLookup,
            ComponentLookup<MailTransferRequest> mailTransferRequestLookup,
            ComponentLookup<MaintenanceRequest> maintenanceRequestLookup,
            ComponentLookup<PoliceEmergencyRequest> policeEmergencyRequestLookup,
            ComponentLookup<PolicePatrolRequest> policePatrolRequestLookup,
            ComponentLookup<PostVanRequest> postVanRequestLookup,
            ComponentLookup<PrisonerTransportRequest> prisonerTransportRequestLookup,
            ComponentLookup<RandomTrafficRequest> randomTrafficRequestLookup,
            ComponentLookup<TaxiRequest> taxiRequestLookup,
            ComponentLookup<TransportVehicleRequest> transportVehicleRequestLookup,
            out CarParameters carParameters
        )
        {
            if (FindCarEntity(requestOwner, carLookup, prefabRefLookup, carDataLookup, residentLookup, carKeeperLookup, out var carEntity))
            {
                GetCarParameters(
                    carEntity,
                    carLookup,
                    prefabRefLookup,
                    carDataLookup,
                    ambulanceLookup,
                    deliveryTruckLookup,
                    fireEngineLookup,
                    garbageTruckLookup,
                    hearseLookup,
                    maintenanceVehicleLookup,
                    personalCarLookup,
                    policeCarLookup,
                    postVanLookup,
                    publicTransportLookup,
                    taxiLookup,
                    out carParameters
                );
                return carParameters.m_IsValid;
            }

            CarFlags carFlags = 0;
            var sizeClass = SizeClass.Undefined;
            var energyTypes = EnergyTypes.None;
            var vehicleTypeFlags = VehicleTypeFlags.None;
            if (evacuationRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.PublicTransport;
            }
            else if (fireRescueRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.FireEngine;
            }
            else if (garbageCollectionRequestLookup.HasComponent(requestOwner) || garbageTransferRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.GarbageTruck;
            }
            else if (goodsDeliveryRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.DeliveryTruck;
            }
            else if (healthcareRequestLookup.TryGetComponent(requestOwner, out var healthcareRequest))
            {
                if (healthcareRequest.m_Type == HealthcareRequestType.Ambulance)
                {
                    vehicleTypeFlags = VehicleTypeFlags.Ambulance;
                }
                else if (healthcareRequest.m_Type == HealthcareRequestType.Hearse)
                {
                    vehicleTypeFlags = VehicleTypeFlags.Hearse;
                }
            }
            else if (mailTransferRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.PostVan;
            }
            else if (maintenanceRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.MaintenanceVehicle;
            }
            else if (policeEmergencyRequestLookup.HasComponent(requestOwner) || policePatrolRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.PoliceCar;
            }
            else if (postVanRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.PostVan;
            }
            else if (prisonerTransportRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.PublicTransport;
            }
            else if (randomTrafficRequestLookup.TryGetComponent(requestOwner, out var randomTrafficRequest))
            {
                sizeClass = randomTrafficRequest.m_SizeClass;
                energyTypes = randomTrafficRequest.m_EnergyTypes;
                if ((randomTrafficRequest.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) != 0)
                {
                    vehicleTypeFlags = VehicleTypeFlags.DeliveryTruck;
                }
                else if ((randomTrafficRequest.m_Flags & RandomTrafficRequestFlags.TransportVehicle) != 0)
                {
                    vehicleTypeFlags = VehicleTypeFlags.PublicTransport;
                }
                else
                {
                    vehicleTypeFlags = VehicleTypeFlags.PersonalCar;
                }
            }
            else if (taxiRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.Taxi;
            }
            else if (transportVehicleRequestLookup.HasComponent(requestOwner))
            {
                vehicleTypeFlags = VehicleTypeFlags.PublicTransport;
            }
            carParameters = new CarParameters();
            if (vehicleTypeFlags != VehicleTypeFlags.None)
            {
                carParameters.m_CarFlags = carFlags;
                carParameters.m_SizeClass = sizeClass;
                carParameters.m_EnergyTypes = energyTypes;
                carParameters.m_VehicleTypeFlags = vehicleTypeFlags;
                carParameters.m_IsValid = true;
                return true;
            }

            return false;
        }

        public static void CheckLaneRules(LaneRules laneRules, CarParameters carParameters, out bool isPrefer, out bool isForbidden, out bool isDisallow)
        {
            if (!carParameters.m_IsValid)
            {
                isPrefer = false;
                isForbidden = false;
                isDisallow = false;
                return;
            }

            var carFlags = carParameters.m_CarFlags;
            var sizeClass = carParameters.m_SizeClass;
            var energyTypes = carParameters.m_EnergyTypes;
            var vehicleTypeFlags = carParameters.m_VehicleTypeFlags;

            isPrefer = IsPrefer(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags);
            isForbidden = IsForbidden(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags);
            isDisallow = IsDisallow(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags);
        }

        public static bool IsPrefer(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes, VehicleTypeFlags vehicleTypeFlags)
        {
            if (IsForbidden(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags))
            {
                return false;
            }
            if (IsDisallow(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags))
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
                || IsPrefer((int)VehicleTypeFlags.PublicTransport, laneRules.m_VehicleType.m_PublicTransport, (int)vehicleTypeFlags)
                || IsPrefer((int)VehicleTypeFlags.Taxi, laneRules.m_VehicleType.m_Taxi, (int)vehicleTypeFlags);

            return isPreferCarFlags || isPreferSizeClass || isPreferEnergyTypes || isPreferVehicleType;
        }

        public static bool IsForbidden(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes, VehicleTypeFlags vehicleTypeFlags)
        {
            if (IsDisallow(laneRules, carFlags, sizeClass, energyTypes, vehicleTypeFlags))
            {
                return false;
            }

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
                || IsForbidden((int)VehicleTypeFlags.PublicTransport, laneRules.m_VehicleType.m_PublicTransport, (int)vehicleTypeFlags)
                || IsForbidden((int)VehicleTypeFlags.Taxi, laneRules.m_VehicleType.m_Taxi, (int)vehicleTypeFlags);

            return isForbiddenCarFlags || isForbiddenSizeClass || isForbiddenEnergyTypes || isForbiddenVehicleType;
        }

        public static bool IsDisallow(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes, VehicleTypeFlags vehicleTypeFlags)
        {
            bool isDisallowCarFlags = IsDisallow((int)CarFlags.Emergency, laneRules.m_CarFlagsRules.m_Emergency, (int)carFlags);

            bool isDisallowSizeClass =
                IsDisallow(laneRules.m_SizeClassRules.m_Small, sizeClass == SizeClass.Small)
                || IsDisallow(laneRules.m_SizeClassRules.m_Medium, sizeClass == SizeClass.Medium)
                || IsDisallow(laneRules.m_SizeClassRules.m_Large, sizeClass == SizeClass.Large)
                || IsDisallow(laneRules.m_SizeClassRules.m_Undefined, sizeClass == SizeClass.Undefined);

            bool isDisallowEnergyTypes =
                IsDisallow(laneRules.m_EnergyTypesRules.m_Fuel, energyTypes == EnergyTypes.Fuel)
                || IsDisallow(laneRules.m_EnergyTypesRules.m_Electricity, energyTypes == EnergyTypes.Electricity)
                || IsDisallow(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, energyTypes == EnergyTypes.FuelAndElectricity)
                || IsDisallow(laneRules.m_EnergyTypesRules.m_None, energyTypes == EnergyTypes.None);

            bool isDisallowVehicleType =
                IsDisallow((int)VehicleTypeFlags.Ambulance, laneRules.m_VehicleType.m_Ambulance, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.DeliveryTruck, laneRules.m_VehicleType.m_DeliveryTruck, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.FireEngine, laneRules.m_VehicleType.m_FireEngine, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.GarbageTruck, laneRules.m_VehicleType.m_GarbageTruck, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.Hearse, laneRules.m_VehicleType.m_Hearse, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.MaintenanceVehicle, laneRules.m_VehicleType.m_MaintenanceVehicle, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.PersonalCar, laneRules.m_VehicleType.m_PersonalCar, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.PoliceCar, laneRules.m_VehicleType.m_PoliceCar, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.PostVan, laneRules.m_VehicleType.m_PostVan, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.PublicTransport, laneRules.m_VehicleType.m_PublicTransport, (int)vehicleTypeFlags)
                || IsDisallow((int)VehicleTypeFlags.Taxi, laneRules.m_VehicleType.m_Taxi, (int)vehicleTypeFlags);

            return isDisallowCarFlags || isDisallowSizeClass || isDisallowEnergyTypes || isDisallowVehicleType;
        }

        public static bool IsPrefer(int flag, LaneRules.RuleOptions rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsPrefer(rule, value);
        }

        public static bool IsForbidden(int flag, LaneRules.RuleOptions rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsForbidden(rule, value);
        }

        public static bool IsDisallow(int flag, LaneRules.RuleOptions rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsDisallow(rule, value);
        }

        public static bool IsPrefer(LaneRules.RuleOptions rule, bool value)
        {
            var noFlagRule = rule & LaneRules.RuleOptions.NoFlagRuleMask;
            var hasFlagRule = rule & LaneRules.RuleOptions.HasFlagRuleMask;

            if (value)
            {
                switch (hasFlagRule)
                {
                    case LaneRules.RuleOptions.HasFlagPrefer:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                switch (noFlagRule)
                {
                    case LaneRules.RuleOptions.NoFlagPrefer:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsForbidden(LaneRules.RuleOptions rule, bool value)
        {
            var noFlagRule = rule & LaneRules.RuleOptions.NoFlagRuleMask;
            var hasFlagRule = rule & LaneRules.RuleOptions.HasFlagRuleMask;

            if (value)
            {
                switch (hasFlagRule)
                {
                    case LaneRules.RuleOptions.HasFlagForbidden:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                switch (noFlagRule)
                {
                    case LaneRules.RuleOptions.NoFlagForbidden:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsDisallow(LaneRules.RuleOptions rule, bool value)
        {
            var noFlagRule = rule & LaneRules.RuleOptions.NoFlagRuleMask;
            var hasFlagRule = rule & LaneRules.RuleOptions.HasFlagRuleMask;
            if (value)
            {
                switch (hasFlagRule)
                {
                    case LaneRules.RuleOptions.HasFlagDisallow:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                switch (noFlagRule)
                {
                    case LaneRules.RuleOptions.NoFlagDisallow:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
