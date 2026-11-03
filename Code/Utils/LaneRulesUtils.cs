using System;
using Colossal.Entities;
using Game.Citizens;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using RoadRule.Components;
using Unity.Entities;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace RoadRule.Utils
{
    public static class LaneRulesUtils
    {
        public enum VehicleType : ushort
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
            Bicycle = 1 << 11,
        }

        public struct CarParameters
        {
            public bool m_IsValid;
            public VehicleType m_VehicleTypeFlags;

            public CarParameters(VehicleType vehicleTypeFlags)
            {
                this.m_IsValid = false;
                this.m_VehicleTypeFlags = vehicleTypeFlags;
            }

            public CarParameters()
            {
                this.m_IsValid = false;
                this.m_VehicleTypeFlags = VehicleType.None;
            }
        }

        public static readonly CarParameters TAXI_CAR_PARAMETERS = new CarParameters(VehicleType.Taxi) { m_IsValid = true };

        public static readonly CarParameters BICYCLE_CAR_PARAMETERS = new CarParameters(VehicleType.Bicycle) { m_IsValid = true };

        public static bool GetCarParameters(
            Entity carEntity,
            ComponentLookup<Car> carLookup,
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
            if (carLookup.HasComponent(carEntity))
            {
                var vehicleType = VehicleType.None;
                if (ambulanceLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.Ambulance;
                }
                else if (deliveryTruckLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.DeliveryTruck;
                }
                else if (fireEngineLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.FireEngine;
                }
                else if (garbageTruckLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.GarbageTruck;
                }
                else if (hearseLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.Hearse;
                }
                else if (maintenanceVehicleLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.MaintenanceVehicle;
                }
                else if (personalCarLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.PersonalCar;
                }
                else if (policeCarLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.PoliceCar;
                }
                else if (postVanLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.PostVan;
                }
                else if (publicTransportLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.PublicTransport;
                }
                else if (taxiLookup.HasComponent(carEntity))
                {
                    vehicleType = VehicleType.Taxi;
                }

                carParameters = new CarParameters(vehicleType) { m_IsValid = true };
                return true;
            }

            carParameters = new CarParameters();
            return false;
        }

        public static bool FindCarEntity(
            Entity requestOwner,
            ComponentLookup<Car> carLookup,
            ComponentLookup<Citizen> citizenLookup,
            ComponentLookup<CarKeeper> carKeeperLookup,
            ComponentLookup<Game.Creatures.Resident> residentLookup,
            out Entity carEntity
        )
        {
            if (carLookup.HasComponent(requestOwner))
            {
                carEntity = requestOwner;
                return true;
            }

            if (citizenLookup.HasComponent(requestOwner))
            {
                if (carKeeperLookup.TryGetEnabledComponent(requestOwner, out var carKeeper))
                {
                    carEntity = carKeeper.m_Car;
                    return true;
                }
                else
                {
                    carEntity = Entity.Null;
                    return true;
                }
            }

            if (residentLookup.TryGetComponent(requestOwner, out var resident) && carKeeperLookup.TryGetEnabledComponent(resident.m_Citizen, out var residentCitizenCarKeeper))
            {
                carEntity = residentCitizenCarKeeper.m_Car;
                return true;
            }

            carEntity = Entity.Null;
            return false;
        }

        public static bool GetCarParameters(
            PathMethod pathMethod,
            SetupTargetType originType,
            SetupTargetType destinationType,
            Entity requestOwner,
            ComponentLookup<Car> carLookup,
            ComponentLookup<Citizen> citizenLookup,
            ComponentLookup<CarKeeper> carKeeperLookup,
            ComponentLookup<Game.Creatures.Resident> residentLookup,
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
            ComponentLookup<RandomTrafficRequest> randomTrafficRequestLookup,
            ComponentLookup<RouteInfo> routeInfoLookup,
            out CarParameters carParameters
        )
        {
            carParameters = new CarParameters();

            if ((pathMethod & PathMethod.Bicycle) != 0)
            {
                carParameters = LaneRulesUtils.BICYCLE_CAR_PARAMETERS;
                return true;
            }
            else if ((pathMethod & (PathMethod.Road | PathMethod.MediumRoad)) != 0)
            {
                if (FindCarEntity(requestOwner, carLookup, citizenLookup, carKeeperLookup, residentLookup, out var carEntity))
                {
                    if (carEntity == Entity.Null)
                    {
                        return false;
                    }

                    GetCarParameters(
                        carEntity,
                        carLookup,
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

                var vehicleTypeFlags = VehicleType.None;
                if (Any(originType, destinationType, SetupTargetType.ResourceSeller))
                {
                    vehicleTypeFlags = VehicleType.DeliveryTruck;
                }
                else if (Any(originType, destinationType, SetupTargetType.TransportVehicle))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else if (Any(originType, destinationType, SetupTargetType.GarbageCollector))
                {
                    vehicleTypeFlags = VehicleType.GarbageTruck;
                }
                else if (Any(originType, destinationType, SetupTargetType.RandomTraffic))
                {
                    if (randomTrafficRequestLookup.TryGetComponent(requestOwner, out var randomTrafficRequest))
                    {
                        if ((randomTrafficRequest.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) != 0)
                        {
                            vehicleTypeFlags = VehicleType.DeliveryTruck;
                        }
                        else if ((randomTrafficRequest.m_Flags & RandomTrafficRequestFlags.TransportVehicle) != 0)
                        {
                            vehicleTypeFlags = VehicleType.PublicTransport;
                        }
                        else
                        {
                            vehicleTypeFlags = VehicleType.PersonalCar;
                        }
                    }
                }
                else if (Any(originType, destinationType, SetupTargetType.JobSeekerTo))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.SchoolSeekerTo))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.FireEngine))
                {
                    vehicleTypeFlags = VehicleType.FireEngine;
                }
                else if (Any(originType, destinationType, SetupTargetType.PolicePatrol))
                {
                    vehicleTypeFlags = VehicleType.PoliceCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.Leisure))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.Taxi))
                {
                    vehicleTypeFlags = VehicleType.Taxi;
                }
                else if (Any(originType, destinationType, SetupTargetType.ResourceExport))
                {
                    vehicleTypeFlags = VehicleType.DeliveryTruck;
                }
                else if (Any(originType, destinationType, SetupTargetType.Ambulance))
                {
                    vehicleTypeFlags = VehicleType.Ambulance;
                }
                else if (Any(originType, destinationType, SetupTargetType.StorageTransfer))
                {
                    vehicleTypeFlags = VehicleType.DeliveryTruck;
                }
                else if (Any(originType, destinationType, SetupTargetType.Maintenance))
                {
                    vehicleTypeFlags = VehicleType.MaintenanceVehicle;
                }
                else if (Any(originType, destinationType, SetupTargetType.PostVan))
                {
                    vehicleTypeFlags = VehicleType.PostVan;
                }
                else if (Any(originType, destinationType, SetupTargetType.MailTransfer))
                {
                    vehicleTypeFlags = VehicleType.PostVan;
                }
                else if (Any(originType, destinationType, SetupTargetType.MailBox))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.OutsideConnection))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.AccidentLocation))
                {
                    vehicleTypeFlags = VehicleType.PoliceCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.Hospital))
                {
                    vehicleTypeFlags = VehicleType.Ambulance;
                }
                else if (Any(originType, destinationType, SetupTargetType.Safety))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.EmergencyShelter))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.EvacuationTransport))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else if (Any(originType, destinationType, SetupTargetType.Hearse))
                {
                    vehicleTypeFlags = VehicleType.Hearse;
                }
                else if (Any(originType, destinationType, SetupTargetType.CrimeProducer))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.PrisonerTransport))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else if (Any(originType, destinationType, SetupTargetType.Sightseeing))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.Attraction))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.GarbageTransfer))
                {
                    vehicleTypeFlags = VehicleType.GarbageTruck;
                }
                else if (Any(originType, destinationType, SetupTargetType.FindHome))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.TransportVehicleRequest))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else if (Any(originType, destinationType, SetupTargetType.TaxiRequest))
                {
                    vehicleTypeFlags = VehicleType.Taxi;
                }
                else if (Any(originType, destinationType, SetupTargetType.PrisonerTransportRequest))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else if (Any(originType, destinationType, SetupTargetType.EvacuationRequest))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else if (Any(originType, destinationType, SetupTargetType.GarbageCollectorRequest))
                {
                    vehicleTypeFlags = VehicleType.GarbageTruck;
                }
                else if (Any(originType, destinationType, SetupTargetType.PoliceRequest))
                {
                    vehicleTypeFlags = VehicleType.PoliceCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.FireRescueRequest))
                {
                    vehicleTypeFlags = VehicleType.FireEngine;
                }
                else if (Any(originType, destinationType, SetupTargetType.PostVanRequest))
                {
                    vehicleTypeFlags = VehicleType.PostVan;
                }
                else if (Any(originType, destinationType, SetupTargetType.MaintenanceRequest))
                {
                    vehicleTypeFlags = VehicleType.MaintenanceVehicle;
                }
                else if (Any(originType, destinationType, SetupTargetType.HealthcareRequest))
                {
                    vehicleTypeFlags = VehicleType.Ambulance;
                }
                else if (Any(originType, destinationType, SetupTargetType.TouristFindTarget))
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }
                else if (Any(originType, destinationType, SetupTargetType.GoodsDelivery))
                {
                    vehicleTypeFlags = VehicleType.DeliveryTruck;
                }
                else if (routeInfoLookup.TryGetComponent(requestOwner, out var routeInfo))
                {
                    vehicleTypeFlags = VehicleType.PublicTransport;
                }
                else
                {
                    vehicleTypeFlags = VehicleType.PersonalCar;
                }

                carParameters.m_VehicleTypeFlags = vehicleTypeFlags;
                carParameters.m_IsValid = true;
                return true;
            }

            return false;
        }

        private static bool Any(SetupTargetType originType, SetupTargetType destinationType, SetupTargetType type)
        {
            return originType == type || destinationType == type;
        }

        public static void CheckLaneRules(LaneRules laneRules, CarParameters carParameters, out LaneRules.RuleOptions options, out float fixedCost)
        {
            options = LaneRules.RuleOptions.None;
            fixedCost = 0f;
            if (!carParameters.m_IsValid)
            {
                return;
            }

            var rules = laneRules.m_VehicleType;
            var rule = carParameters.m_VehicleTypeFlags switch
            {
                VehicleType.Ambulance => rules.m_Ambulance,
                VehicleType.DeliveryTruck => rules.m_DeliveryTruck,
                VehicleType.FireEngine => rules.m_FireEngine,
                VehicleType.GarbageTruck => rules.m_GarbageTruck,
                VehicleType.Hearse => rules.m_Hearse,
                VehicleType.MaintenanceVehicle => rules.m_MaintenanceVehicle,
                VehicleType.PersonalCar => rules.m_PersonalCar,
                VehicleType.PoliceCar => rules.m_PoliceCar,
                VehicleType.PostVan => rules.m_PostVan,
                VehicleType.PublicTransport => rules.m_PublicTransport,
                VehicleType.Taxi => rules.m_Taxi,
                VehicleType.Bicycle => rules.m_Bicycle,
                _ => default,
            };
            options = rule.m_Options;
            fixedCost = rule.m_FixedCost;
        }
    }
}
