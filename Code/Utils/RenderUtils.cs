using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.Mathematics;
using Game.Net;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RoadRule.Utils
{
    public static class RenderUtils
    {
        public static void DrawEdgeOutline(
            Segment startEdgeSegment,
            Segment endEdgeSegment,
            ref OverlayRenderSystem.Buffer overlayBuffer,
            Color color,
            float lineWidth,
            bool isDashed = false
        )
        {
            //start edge line
            overlayBuffer.DrawLine(color, color, 0, 0, new Line3.Segment(startEdgeSegment.m_Left.a, startEdgeSegment.m_Right.a), lineWidth, float2.zero);
            if (!isDashed)
            {
                //left edge line
                overlayBuffer.DrawCurve(color, color, 0, 0, startEdgeSegment.m_Left, lineWidth, 1);
                overlayBuffer.DrawCurve(color, color, 0, 0, endEdgeSegment.m_Left, lineWidth, 1);
                //right edge line
                overlayBuffer.DrawCurve(color, color, 0, 0, startEdgeSegment.m_Right, lineWidth, 1);
                overlayBuffer.DrawCurve(color, color, 0, 0, endEdgeSegment.m_Right, lineWidth, 1);
            }
            else
            {
                //left edge line
                overlayBuffer.DrawDashedCurve(color, color, 0, 0, startEdgeSegment.m_Left, lineWidth, 2, 0.4f);
                overlayBuffer.DrawDashedCurve(color, color, 0, 0, endEdgeSegment.m_Left, lineWidth, 2, 0.4f);
                //right edge line
                overlayBuffer.DrawDashedCurve(color, color, 0, 0, startEdgeSegment.m_Right, lineWidth, 2, 0.4f);
                overlayBuffer.DrawDashedCurve(color, color, 0, 0, endEdgeSegment.m_Right, lineWidth, 2, 0.4f);
            }
            //end cut line
            overlayBuffer.DrawLine(color, color, 0, 0, new Line3.Segment(endEdgeSegment.m_Left.d, endEdgeSegment.m_Right.d), lineWidth, float2.zero);
        }
    }
}
