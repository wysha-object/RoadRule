using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.UI.Tooltip;

namespace RoadRule.Systems.UI
{
    public partial class TooltipSystem : TooltipSystemBase
    {
        public List<StringTooltip> m_TooltipList;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TooltipList = [];
        }

        protected override void OnUpdate()
        {
            foreach (var tooltip in m_TooltipList)
            {
                AddMouseTooltip(tooltip);
            }
        }
    }
}
