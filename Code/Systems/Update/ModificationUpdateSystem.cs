using Game;
using Game.Common;
using Unity.Entities;

namespace RoadRule.Systems.Update;

public partial class ModificationUpdateSystem : GameSystemBase
{
    private RoadRule.Systems.UI.UISystem m_UISystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_UISystem = World.GetOrCreateSystemManaged<RoadRule.Systems.UI.UISystem>();
    }

    protected override void OnUpdate()
    {
        m_UISystem.ModificationUpdate();
    }
}
