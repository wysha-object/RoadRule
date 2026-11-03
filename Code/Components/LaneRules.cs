using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace RoadRule.Components
{
    public struct LaneRules : IComponentData, ISerializable
    {
        private enum Rule : byte
        {
            None = 0,

            /// 有 Flag 记作 Prefer
            NoneOrPrefer = 32,

            /// 有 Flag 记作 Forbidden
            NoneOrForbidden = 64,

            /// 没 Flag 记作 Forbidden, 有 Flag 记作 Prefer
            ForbiddenOrPrefer = 80,

            /// 没 Flag 记作 Prefer, 有 Flag 记作 Forbidden
            PreferOrForbidden = 96,
        }

        private static RuleOptions migrateRule(int ruleValue)
        {
            Rule rule = (Rule)ruleValue;
            switch (rule)
            {
                case Rule.None:
                    return RuleOptions.None;
                case Rule.NoneOrPrefer:
                    return RuleOptions.Prefer;
                case Rule.NoneOrForbidden:
                    return RuleOptions.Forbidden;
                case Rule.ForbiddenOrPrefer:
                    return RuleOptions.Prefer;
                case Rule.PreferOrForbidden:
                    return RuleOptions.Forbidden;
                default:
                    return RuleOptions.None;
            }
        }

        public enum RuleOptions : byte
        {
            None = 0,

            ModeMask = 0xf,

            Prefer = 1,

            Forbidden = 2,

            Disallow = 3,

            FixedCost = 4,
        }

        private struct CarFlagsRules : ISerializable
        {
            public RuleOptions m_Emergency;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                if (schemaVersion <= 1)
                {
                    reader.Read(out int emergency);
                    m_Emergency = migrateRule(emergency);
                }

                if (schemaVersion >= 2)
                {
                    reader.Read(out byte emergency);
                    m_Emergency = (RuleOptions)emergency;
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                throw new System.NotImplementedException();
            }
        }

        private struct SizeClassRules : ISerializable
        {
            public RuleOptions m_Small;
            public RuleOptions m_Medium;
            public RuleOptions m_Large;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                if (schemaVersion <= 1)
                {
                    reader.Read(out int small);
                    m_Small = migrateRule(small);

                    reader.Read(out int medium);
                    m_Medium = migrateRule(medium);

                    reader.Read(out int large);
                    m_Large = migrateRule(large);

                    reader.Read(out int _);
                }

                if (schemaVersion >= 2)
                {
                    reader.Read(out byte small);
                    m_Small = (RuleOptions)small;

                    reader.Read(out byte medium);
                    m_Medium = (RuleOptions)medium;

                    reader.Read(out byte large);
                    m_Large = (RuleOptions)large;

                    if (schemaVersion == 2)
                    {
                        reader.Read(out byte _);
                    }
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                throw new System.NotImplementedException();
            }
        }

        private struct EnergyTypesRules : ISerializable
        {
            public RuleOptions m_Fuel;
            public RuleOptions m_Electricity;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                if (schemaVersion <= 1)
                {
                    reader.Read(out int fuel);
                    m_Fuel = migrateRule(fuel);

                    reader.Read(out int electricity);
                    m_Electricity = migrateRule(electricity);

                    reader.Read(out int _);

                    reader.Read(out int _);
                }

                if (schemaVersion >= 2)
                {
                    reader.Read(out byte fuel);
                    m_Fuel = (RuleOptions)fuel;

                    reader.Read(out byte electricity);
                    m_Electricity = (RuleOptions)electricity;

                    if (schemaVersion == 2)
                    {
                        reader.Read(out byte _);
                        reader.Read(out byte _);
                    }
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                throw new System.NotImplementedException();
            }
        }

        public struct VehicleRule : ISerializable
        {
            public RuleOptions m_Options;
            public ushort m_FixedCost;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out byte options);
                m_Options = (RuleOptions)options;
                reader.Read(out m_FixedCost);
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                writer.Write((byte)m_Options);
                writer.Write(m_FixedCost);
            }
        }

        public struct VehicleTypeRules : ISerializable
        {
            public VehicleRule m_Ambulance;
            public VehicleRule m_DeliveryTruck;
            public VehicleRule m_FireEngine;
            public VehicleRule m_GarbageTruck;
            public VehicleRule m_Hearse;
            public VehicleRule m_MaintenanceVehicle;
            public VehicleRule m_PersonalCar;
            public VehicleRule m_PoliceCar;
            public VehicleRule m_PostVan;
            public VehicleRule m_PublicTransport;
            public VehicleRule m_Taxi;
            public VehicleRule m_Bicycle;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                this = default;

                if (schemaVersion >= 5)
                {
                    reader.Read(out m_Ambulance);
                    reader.Read(out m_DeliveryTruck);
                    reader.Read(out m_FireEngine);
                    reader.Read(out m_GarbageTruck);
                    reader.Read(out m_Hearse);
                    reader.Read(out m_MaintenanceVehicle);
                    reader.Read(out m_PersonalCar);
                    reader.Read(out m_PoliceCar);
                    reader.Read(out m_PostVan);
                    reader.Read(out m_PublicTransport);
                    reader.Read(out m_Taxi);
                    reader.Read(out m_Bicycle);
                    return;
                }

                if (schemaVersion <= 2)
                {
                    reader.Read(out int ambulance);
                    m_Ambulance.m_Options = migrateRule(ambulance);
                    reader.Read(out int deliveryTruck);
                    m_DeliveryTruck.m_Options = migrateRule(deliveryTruck);
                    reader.Read(out int fireEngine);
                    m_FireEngine.m_Options = migrateRule(fireEngine);
                    reader.Read(out int garbageTruck);
                    m_GarbageTruck.m_Options = migrateRule(garbageTruck);
                    reader.Read(out int hearse);
                    m_Hearse.m_Options = migrateRule(hearse);
                    reader.Read(out int maintenanceVehicle);
                    m_MaintenanceVehicle.m_Options = migrateRule(maintenanceVehicle);
                    reader.Read(out int personalCar);
                    m_PersonalCar.m_Options = migrateRule(personalCar);
                    reader.Read(out int policeCar);
                    m_PoliceCar.m_Options = migrateRule(policeCar);
                    reader.Read(out int postVan);
                    m_PostVan.m_Options = migrateRule(postVan);
                    reader.Read(out int publicTransport);
                    m_PublicTransport.m_Options = migrateRule(publicTransport);

                    if (schemaVersion == 2)
                    {
                        reader.Read(out int taxi);
                        m_Taxi.m_Options = migrateRule(taxi);
                    }
                }

                if (schemaVersion >= 3)
                {
                    reader.Read(out byte ambulance);
                    m_Ambulance.m_Options = (RuleOptions)ambulance;
                    reader.Read(out byte deliveryTruck);
                    m_DeliveryTruck.m_Options = (RuleOptions)deliveryTruck;
                    reader.Read(out byte fireEngine);
                    m_FireEngine.m_Options = (RuleOptions)fireEngine;
                    reader.Read(out byte garbageTruck);
                    m_GarbageTruck.m_Options = (RuleOptions)garbageTruck;
                    reader.Read(out byte hearse);
                    m_Hearse.m_Options = (RuleOptions)hearse;
                    reader.Read(out byte maintenanceVehicle);
                    m_MaintenanceVehicle.m_Options = (RuleOptions)maintenanceVehicle;
                    reader.Read(out byte personalCar);
                    m_PersonalCar.m_Options = (RuleOptions)personalCar;
                    reader.Read(out byte policeCar);
                    m_PoliceCar.m_Options = (RuleOptions)policeCar;
                    reader.Read(out byte postVan);
                    m_PostVan.m_Options = (RuleOptions)postVan;
                    reader.Read(out byte publicTransport);
                    m_PublicTransport.m_Options = (RuleOptions)publicTransport;
                    reader.Read(out byte taxi);
                    m_Taxi.m_Options = (RuleOptions)taxi;

                    if (schemaVersion <= 3)
                    {
                        m_Ambulance.m_Options &= RuleOptions.ModeMask;
                        m_DeliveryTruck.m_Options &= RuleOptions.ModeMask;
                        m_FireEngine.m_Options &= RuleOptions.ModeMask;
                        m_GarbageTruck.m_Options &= RuleOptions.ModeMask;
                        m_Hearse.m_Options &= RuleOptions.ModeMask;
                        m_MaintenanceVehicle.m_Options &= RuleOptions.ModeMask;
                        m_PersonalCar.m_Options &= RuleOptions.ModeMask;
                        m_PoliceCar.m_Options &= RuleOptions.ModeMask;
                        m_PostVan.m_Options &= RuleOptions.ModeMask;
                        m_PublicTransport.m_Options &= RuleOptions.ModeMask;
                        m_Taxi.m_Options &= RuleOptions.ModeMask;
                    }
                }

                if (schemaVersion >= 4)
                {
                    reader.Read(out byte bicycle);
                    m_Bicycle.m_Options = (RuleOptions)bicycle;
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 5;
                writer.Write(schemaVersion);

                writer.Write(m_Ambulance);
                writer.Write(m_DeliveryTruck);
                writer.Write(m_FireEngine);
                writer.Write(m_GarbageTruck);
                writer.Write(m_Hearse);
                writer.Write(m_MaintenanceVehicle);
                writer.Write(m_PersonalCar);
                writer.Write(m_PoliceCar);
                writer.Write(m_PostVan);
                writer.Write(m_PublicTransport);
                writer.Write(m_Taxi);
                writer.Write(m_Bicycle);
            }
        }

        public VehicleTypeRules m_VehicleType;

        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out ushort schemaVersion);

            if (schemaVersion <= 2)
            {
                reader.Read(out CarFlagsRules _);
                reader.Read(out SizeClassRules _);
                reader.Read(out EnergyTypesRules _);
            }

            if (schemaVersion >= 2)
            {
                reader.Read(out m_VehicleType);
            }
        }

        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            ushort schemaVersion = 3;
            writer.Write(schemaVersion);

            writer.Write(m_VehicleType);
        }
    }
}
