using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace RoadRule.Components
{
    public struct LaneRules : IComponentData, ISerializable
    {
        public enum Rule : byte
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

        public struct CarFlagsRules : ISerializable
        {
            public Rule m_Emergency;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                reader.Read(out int emergency);
                m_Emergency = (Rule)emergency;
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 1;
                writer.Write(schemaVersion);

                writer.Write((int)m_Emergency);
            }
        }

        public struct SizeClassRules : ISerializable
        {
            public Rule m_Small;
            public Rule m_Medium;
            public Rule m_Large;
            public Rule m_Undefined;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                reader.Read(out int small);
                m_Small = (Rule)small;

                reader.Read(out int medium);
                m_Medium = (Rule)medium;

                reader.Read(out int large);
                m_Large = (Rule)large;

                reader.Read(out int undefined);
                m_Undefined = (Rule)undefined;
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 1;
                writer.Write(schemaVersion);

                writer.Write((int)m_Small);
                writer.Write((int)m_Medium);
                writer.Write((int)m_Large);
                writer.Write((int)m_Undefined);
            }
        }

        public struct EnergyTypesRules : ISerializable
        {
            public Rule m_Fuel;
            public Rule m_Electricity;
            public Rule m_FuelAndElectricity;
            public Rule m_None;

            public void Deserialize<TReader>(TReader reader)
                where TReader : IReader
            {
                reader.Read(out ushort schemaVersion);

                reader.Read(out int fuel);
                m_Fuel = (Rule)fuel;

                reader.Read(out int electricity);
                m_Electricity = (Rule)electricity;

                reader.Read(out int fuelAndElectricity);
                m_FuelAndElectricity = (Rule)fuelAndElectricity;

                reader.Read(out int none);
                m_None = (Rule)none;
            }

            public void Serialize<TWriter>(TWriter writer)
                where TWriter : IWriter
            {
                ushort schemaVersion = 1;
                writer.Write(schemaVersion);

                writer.Write((int)m_Fuel);
                writer.Write((int)m_Electricity);
                writer.Write((int)m_FuelAndElectricity);
                writer.Write((int)m_None);
            }
        }

        public CarFlagsRules m_CarFlagsRules;
        public SizeClassRules m_SizeClassRules;
        public EnergyTypesRules m_EnergyTypesRules;

        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out ushort schemaVersion);

            reader.Read(out CarFlagsRules carFlagsRules);
            m_CarFlagsRules = carFlagsRules;

            reader.Read(out SizeClassRules sizeClassRules);
            m_SizeClassRules = sizeClassRules;

            reader.Read(out EnergyTypesRules energyTypesRules);
            m_EnergyTypesRules = energyTypesRules;
        }

        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            ushort schemaVersion = 1;
            writer.Write(schemaVersion);

            writer.Write(m_CarFlagsRules);
            writer.Write(m_SizeClassRules);
            writer.Write(m_EnergyTypesRules);
        }
    }
}
