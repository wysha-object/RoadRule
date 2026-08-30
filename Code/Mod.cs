using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.Pathfind;
using Game.SceneFlow;
using Game.Serialization;
using HarmonyLib;
using RoadRule.Systems.Pathfind;
using RoadRule.Systems.Update;

namespace RoadRule
{
    public class Mod : IMod
    {
        public static readonly string m_InformationalVersion = (
            (AssemblyInformationalVersionAttribute)System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(Mod)), typeof(AssemblyInformationalVersionAttribute))
        ).InformationalVersion;

        public static ILog m_Log = LogManager.GetLogger(nameof(RoadRule)).SetShowsErrorsInUI(false);

        public static Settings m_Settings;

        public void OnLoad(Game.UpdateSystem updateSystem)
        {
            m_Log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                m_Log.Info($"Current mod asset at {asset.path}");

            LoadHarmonyPatches();

            m_Settings = new Settings(this);

            SystemSetup(updateSystem);
        }

        public void OnDispose()
        {
            m_Log.Info(nameof(OnDispose));
        }

        public void LoadHarmonyPatches()
        {
            var harmony = new Harmony("RoadRule");
            harmony.PatchAll();
        }

        public void SystemSetup(Game.UpdateSystem updateSystem)
        {
            var carNavigationSystem = updateSystem.World.GetOrCreateSystemManaged<Game.Simulation.CarNavigationSystem>();

            carNavigationSystem.Enabled = false;

            updateSystem.UpdateBefore<Systems.Simulation.PatchedCarNavigationSystem, Game.Simulation.CarNavigationSystem.Actions>(Game.SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateBefore<Systems.Simulation.PatchedCarNavigationSystem, Game.Simulation.CarNavigationSystem.Actions>(Game.SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ObsoleteCheckSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ObsoleteMarkerSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ModificationUpdateSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<Systems.Tool.ToolSystem>(Game.SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<Systems.UI.UISystem>(Game.SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<Systems.UI.TooltipSystem>(Game.SystemUpdatePhase.UITooltip);
        }

        public static string ReleaseChannel()
        {
#if STABLE
            return "Stable";
#elif BETA
            return "Beta";
#else
            return "UNKNOWN";
#endif
        }
    }
}
