using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RoadRule.Components
{
    public struct PathfindNeedObsoleteFlag : IComponentData, IEmptySerializable { }
}
