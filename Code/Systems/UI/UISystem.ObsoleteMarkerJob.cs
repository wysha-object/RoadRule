using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Common;
using Game.Pathfind;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        [BurstCompile]
        private struct ObsoleteMarkerJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            public ComponentTypeHandle<PathOwner> m_PathOwnerType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                NativeArray<PathOwner> pathOwners = chunk.GetNativeArray(ref m_PathOwnerType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var pathOwner = pathOwners[i];
                    pathOwner.m_State |= PathFlags.Obsolete;
                    pathOwners[i] = pathOwner;
                }
            }
        }
    }
}
