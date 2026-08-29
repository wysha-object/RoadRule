using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RoadRule.Components
{
    public struct PathfindReprocessed : IComponentData, ISerializable
    {
        public Entity m_LastTargetEntity;

        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out ushort schemaVersion);

            reader.Read(out m_LastTargetEntity);
        }

        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            ushort schemaVersion = 1;
            writer.Write(schemaVersion);

            writer.Write(m_LastTargetEntity);
        }
    }
}
