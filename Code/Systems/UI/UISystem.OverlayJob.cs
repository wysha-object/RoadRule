using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using RoadRule.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        [BurstCompile]
        private struct OverlayJob : IJob
        {
            [ReadOnly]
            public ComponentLookup<NetCompositionData> m_NetCompositionDataLookup;

            [ReadOnly]
            public BufferLookup<NetCompositionLane> m_NetCompositionLaneBufferLookup;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryLookup;

            [ReadOnly]
            public ComponentLookup<NetLaneData> m_NetLaneDataLookup;
            public NativeArray<Entity> m_SelectedEdgeEntityArray;
            public NativeArray<int> m_SelectedLaneIndexArray;
            public Entity m_CompositionEdgePrefabEntity;
            public NativeHashMap<int, UnsafeList<int>> m_MasterLaneIndexMap;
            public OverlayRenderSystem.Buffer m_OverlayRenderSystemBuffer;

            public void Execute()
            {
                DrawLane();
            }

            private void DrawLane()
            {
                foreach (var selectedEdgeEntity in m_SelectedEdgeEntityArray)
                {
                    if (
                        m_NetCompositionDataLookup.TryGetComponent(m_CompositionEdgePrefabEntity, out var netCompositionData)
                        && m_NetCompositionLaneBufferLookup.TryGetBuffer(m_CompositionEdgePrefabEntity, out var netCompositionLaneBuffer)
                        && m_EdgeGeometryLookup.TryGetComponent(selectedEdgeEntity, out var edgeGeometry)
                    )
                    {
                        foreach (var kvp in m_MasterLaneIndexMap)
                        {
                            var selectedAll = m_SelectedLaneIndexArray.Contains(kvp.Key);
                            var keyArray = kvp.Value;
                            foreach (var index in keyArray)
                            {
                                var slected = selectedAll || m_SelectedLaneIndexArray.Contains(index);
                                var netCompositionLane = netCompositionLaneBuffer.ElementAt(index);
                                var startLaneSegment = CalculateLaneSegment(ref edgeGeometry.m_Start, ref netCompositionLane, netCompositionData.m_Width, m_NetLaneDataLookup);
                                var endLaneSegment = CalculateLaneSegment(ref edgeGeometry.m_End, ref netCompositionLane, netCompositionData.m_Width, m_NetLaneDataLookup);
                                var color = slected ? new Color(0f, 0.8f, 1f, 1f) : Color.white;
                                var lineWdth = slected ? 0.2f : 0.1f;
                                RenderUtils.DrawEdgeOutline(startLaneSegment, endLaneSegment, ref m_OverlayRenderSystemBuffer, color, lineWdth, false);
                            }
                        }
                    }
                }
            }

            private Segment CalculateLaneSegment(ref Segment edgeSegment, ref NetCompositionLane compositionLane, float edgeWidth, ComponentLookup<NetLaneData> netLaneDataLookup)
            {
                float halfLaneWidth = math.max((netLaneDataLookup[compositionLane.m_Lane].m_Width - 0.3f) / 2f, 0.5f);
                float t = (compositionLane.m_Position.x - halfLaneWidth) / math.max(1f, edgeWidth) + 0.5f;
                float t2 = (compositionLane.m_Position.x + halfLaneWidth) / math.max(1f, edgeWidth) + 0.5f;
                Segment segment = new Segment()
                {
                    m_Left = MathUtils.Lerp(edgeSegment.m_Left, edgeSegment.m_Right, t),
                    m_Right = MathUtils.Lerp(edgeSegment.m_Left, edgeSegment.m_Right, t2),
                };
                segment.m_Length = new float2(MathUtils.Length(segment.m_Left), MathUtils.Length(segment.m_Right));

                return segment;
            }
        }
    }
}
