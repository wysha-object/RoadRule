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
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryLookup;
            public NativeArray<EdgeParameters> m_SelectedEdgeEntityArray;
            public NativeArray<byte> m_SelectedLaneIndexArray;
            public CompositionParameters m_CompositionParameters;
            public NativeHashMap<byte, UnsafeList<byte>> m_MasterLaneIndexMap;
            public OverlayRenderSystem.Buffer m_OverlayRenderSystemBuffer;

            private void DrawLane()
            {
                foreach (var selectedEdge in m_SelectedEdgeEntityArray)
                {
                    if (m_EdgeGeometryLookup.TryGetComponent(selectedEdge.m_Entity, out var edgeGeometry))
                    {
                        foreach (var kvp in m_MasterLaneIndexMap)
                        {
                            var selectedAll = m_SelectedLaneIndexArray.Contains(kvp.Key);
                            var keyArray = kvp.Value;
                            foreach (var index in keyArray)
                            {
                                var slected = selectedAll || m_SelectedLaneIndexArray.Contains(index);
                                var laneParameters = selectedEdge.m_CompositionLaneParameters[index];
                                var startLaneSegment = CalculateLaneSegment(ref edgeGeometry.m_Start, ref laneParameters, selectedEdge.m_Width);
                                var endLaneSegment = CalculateLaneSegment(ref edgeGeometry.m_End, ref laneParameters, selectedEdge.m_Width);
                                var color = slected ? new Color(0f, 0.8f, 1f, 1f) : Color.white;
                                var lineWdth = slected ? 0.2f : 0.1f;
                                RenderUtils.DrawEdgeOutline(startLaneSegment, endLaneSegment, ref m_OverlayRenderSystemBuffer, color, lineWdth, false);
                            }
                        }
                    }
                }
            }

            private Segment CalculateLaneSegment(ref Segment edgeSegment, ref LaneParameters laneParameters, float edgeWidth)
            {
                float halfLaneWidth = math.max((laneParameters.m_Width - 0.3f) / 2f, 0.5f);
                float t = (laneParameters.m_Position.x - halfLaneWidth) / math.max(1f, edgeWidth) + 0.5f;
                float t2 = (laneParameters.m_Position.x + halfLaneWidth) / math.max(1f, edgeWidth) + 0.5f;
                Segment segment = new Segment()
                {
                    m_Left = MathUtils.Lerp(edgeSegment.m_Left, edgeSegment.m_Right, t),
                    m_Right = MathUtils.Lerp(edgeSegment.m_Left, edgeSegment.m_Right, t2),
                };
                segment.m_Length = new float2(MathUtils.Length(segment.m_Left), MathUtils.Length(segment.m_Right));

                return segment;
            }

            public void Execute()
            {
                DrawLane();
            }
        }

        private JobHandle ScheduleOverlayJob(in JobHandle dependsOn)
        {
            JobHandle dependency = dependsOn;
            if (m_CompositionParameters is CompositionParameters compositionParameters)
            {
                var overlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
                var buffer = overlayRenderSystem.GetBuffer(out var overlayRenderDependencies);
                var dictionary = GetMasterLaneDictionary();

                var selectedEdgeEntityArray = new NativeArray<EdgeParameters>(SelectedEdgeList.ToArray(), Allocator.TempJob);
                var selectedLaneIndexArray = new NativeArray<byte>(SelectedLaneIndexSet.ToArray(), Allocator.TempJob);
                var masterLaneIndexMap = new NativeHashMap<byte, UnsafeList<byte>>(dictionary.Count, Allocator.TempJob);
                foreach (var kvp in dictionary)
                {
                    var keys = kvp.Value.m_LaneIndexDictionary.Keys;
                    var list = new UnsafeList<byte>(keys.Count, Allocator.TempJob);
                    foreach (var key in keys)
                    {
                        list.Add(key);
                    }
                    if (list.Length == 0)
                    {
                        list.Add(kvp.Key);
                    }
                    masterLaneIndexMap.Add(kvp.Key, list);
                }

                dependency = IJobExtensions.Schedule(
                    new OverlayJob
                    {
                        m_NetCompositionDataLookup = SystemAPI.GetComponentLookup<NetCompositionData>(true),
                        m_EdgeGeometryLookup = SystemAPI.GetComponentLookup<EdgeGeometry>(true),
                        m_SelectedEdgeEntityArray = selectedEdgeEntityArray,
                        m_SelectedLaneIndexArray = selectedLaneIndexArray,
                        m_CompositionParameters = compositionParameters,
                        m_MasterLaneIndexMap = masterLaneIndexMap,
                        m_OverlayRenderSystemBuffer = buffer,
                    },
                    JobHandle.CombineDependencies(dependsOn, overlayRenderDependencies)
                );

                foreach (var kvp in masterLaneIndexMap)
                {
                    var cloned = masterLaneIndexMap[kvp.Key];
                    dependency = cloned.Dispose(dependency);
                }
                dependency = masterLaneIndexMap.Dispose(dependency);
                dependency = selectedLaneIndexArray.Dispose(dependency);
                dependency = selectedEdgeEntityArray.Dispose(dependency);
            }
            return dependency;
        }
    }
}
