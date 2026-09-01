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

            /// 没 Flag 记作 Prefer
            PreferOrNone = 16,

            /// 有 Flag 记作 Prefer
            NoneOrPrefer = 32,

            /// 没 Flag 记作 Forbidden
            ForbiddenOrNone = 48,

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
                case Rule.PreferOrNone:
                    return RuleOptions.NoFlagPrefer;
                case Rule.NoneOrPrefer:
                    return RuleOptions.HasFlagPrefer;
                case Rule.ForbiddenOrNone:
                    return RuleOptions.NoFlagForbidden;
                case Rule.NoneOrForbidden:
                    return RuleOptions.HasFlagForbidden;
                case Rule.ForbiddenOrPrefer:
                    return RuleOptions.NoFlagForbidden | RuleOptions.HasFlagPrefer;
                case Rule.PreferOrForbidden:
                    return RuleOptions.NoFlagPrefer | RuleOptions.HasFlagForbidden;
                default:
                    return RuleOptions.None;
            }
        }

        public enum RuleOptions : byte
        {
            None = 0,

            NoFlagRuleMask = 0xf << 4,

            NoFlagPrefer = 1 << 4,

            NoFlagForbidden = 2 << 4,

            HasFlagRuleMask = 0xf,

            HasFlagPrefer = 1,

            HasFlagForbidden = 2,
        }

        public struct CarFlagsRules : ISerializable
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
                ushort schemaVersion = 2;
                writer.Write(schemaVersion);

                writer.Write((byte)m_Emergency);
            }
        }

        public struct SizeClassRules : ISerializable
        {
            public RuleOptions m_Small;
            public RuleOptions m_Medium;
            public RuleOptions m_Large;
            public RuleOptions m_Undefined;

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

                    reader.Read(out int undefined);
                    m_Undefined = migrateRule(undefined);
                }

                if (schemaVersion >= 2)
                {
                    reader.Read(out byte small);
                    m_Small = (RuleOptions)small;

                    reader.Read(out byte medium);
                    m_Medium = (RuleOptions)medium;

                    reader.Read(out byte large);
                    m_Large = (RuleOptions)large;

                    reader.Read(out byte undefined);
                    m_Undefined = (RuleOptions)undefined;
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 2;
                writer.Write(schemaVersion);

                writer.Write((byte)m_Small);
                writer.Write((byte)m_Medium);
                writer.Write((byte)m_Large);
                writer.Write((byte)m_Undefined);
            }
        }

        public struct EnergyTypesRules : ISerializable
        {
            public RuleOptions m_Fuel;
            public RuleOptions m_Electricity;
            public RuleOptions m_FuelAndElectricity;
            public RuleOptions m_None;

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

                    reader.Read(out int fuelAndElectricity);
                    m_FuelAndElectricity = migrateRule(fuelAndElectricity);

                    reader.Read(out int none);
                    m_None = migrateRule(none);
                }

                if (schemaVersion >= 2)
                {
                    reader.Read(out byte fuel);
                    m_Fuel = (RuleOptions)fuel;

                    reader.Read(out byte electricity);
                    m_Electricity = (RuleOptions)electricity;

                    reader.Read(out byte fuelAndElectricity);
                    m_FuelAndElectricity = (RuleOptions)fuelAndElectricity;

                    reader.Read(out byte none);
                    m_None = (RuleOptions)none;
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 2;
                writer.Write(schemaVersion);

                writer.Write((byte)m_Fuel);
                writer.Write((byte)m_Electricity);
                writer.Write((byte)m_FuelAndElectricity);
                writer.Write((byte)m_None);
            }
        }

        public struct VehicleTypeRules : ISerializable
        {
            public RuleOptions m_Ambulance;
            public RuleOptions m_DeliveryTruck;
            public RuleOptions m_FireEngine;
            public RuleOptions m_GarbageTruck;
            public RuleOptions m_Hearse;
            public RuleOptions m_MaintenanceVehicle;
            public RuleOptions m_PersonalCar;
            public RuleOptions m_PoliceCar;
            public RuleOptions m_PostVan;
            public RuleOptions m_PublicTransport;
            public RuleOptions m_Taxi;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                if (schemaVersion <= 2)
                {
                    reader.Read(out int ambulance);
                    m_Ambulance = migrateRule(ambulance);
                    reader.Read(out int deliveryTruck);
                    m_DeliveryTruck = migrateRule(deliveryTruck);
                    reader.Read(out int fireEngine);
                    m_FireEngine = migrateRule(fireEngine);
                    reader.Read(out int garbageTruck);
                    m_GarbageTruck = migrateRule(garbageTruck);
                    reader.Read(out int hearse);
                    m_Hearse = migrateRule(hearse);
                    reader.Read(out int maintenanceVehicle);
                    m_MaintenanceVehicle = migrateRule(maintenanceVehicle);
                    reader.Read(out int personalCar);
                    m_PersonalCar = migrateRule(personalCar);
                    reader.Read(out int policeCar);
                    m_PoliceCar = migrateRule(policeCar);
                    reader.Read(out int postVan);
                    m_PostVan = migrateRule(postVan);
                    reader.Read(out int publicTransport);
                    m_PublicTransport = migrateRule(publicTransport);

                    if (schemaVersion == 2)
                    {
                        reader.Read(out int taxi);
                        m_Taxi = migrateRule(taxi);
                    }
                }

                if (schemaVersion >= 3)
                {
                    reader.Read(out byte ambulance);
                    m_Ambulance = (RuleOptions)ambulance;
                    reader.Read(out byte deliveryTruck);
                    m_DeliveryTruck = (RuleOptions)deliveryTruck;
                    reader.Read(out byte fireEngine);
                    m_FireEngine = (RuleOptions)fireEngine;
                    reader.Read(out byte garbageTruck);
                    m_GarbageTruck = (RuleOptions)garbageTruck;
                    reader.Read(out byte hearse);
                    m_Hearse = (RuleOptions)hearse;
                    reader.Read(out byte maintenanceVehicle);
                    m_MaintenanceVehicle = (RuleOptions)maintenanceVehicle;
                    reader.Read(out byte personalCar);
                    m_PersonalCar = (RuleOptions)personalCar;
                    reader.Read(out byte policeCar);
                    m_PoliceCar = (RuleOptions)policeCar;
                    reader.Read(out byte postVan);
                    m_PostVan = (RuleOptions)postVan;
                    reader.Read(out byte publicTransport);
                    m_PublicTransport = (RuleOptions)publicTransport;
                    reader.Read(out byte taxi);
                    m_Taxi = (RuleOptions)taxi;
                }
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 3;
                writer.Write(schemaVersion);

                writer.Write((byte)m_Ambulance);
                writer.Write((byte)m_DeliveryTruck);
                writer.Write((byte)m_FireEngine);
                writer.Write((byte)m_GarbageTruck);
                writer.Write((byte)m_Hearse);
                writer.Write((byte)m_MaintenanceVehicle);
                writer.Write((byte)m_PersonalCar);
                writer.Write((byte)m_PoliceCar);
                writer.Write((byte)m_PostVan);
                writer.Write((byte)m_PublicTransport);
                writer.Write((byte)m_Taxi);
            }
        }

        public CarFlagsRules m_CarFlagsRules;
        public SizeClassRules m_SizeClassRules;
        public EnergyTypesRules m_EnergyTypesRules;
        public VehicleTypeRules m_VehicleType;

        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out ushort schemaVersion);

            reader.Read(out m_CarFlagsRules);
            reader.Read(out m_SizeClassRules);
            reader.Read(out m_EnergyTypesRules);

            if (schemaVersion >= 2)
            {
                reader.Read(out m_VehicleType);
            }
        }

        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            ushort schemaVersion = 2;
            writer.Write(schemaVersion);

            writer.Write(m_CarFlagsRules);
            writer.Write(m_SizeClassRules);
            writer.Write(m_EnergyTypesRules);
            writer.Write(m_VehicleType);
        }
    }
}
