using System.Collections.Generic;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;

namespace RoadRule
{
    [FileLocation($"ModsSettings/{nameof(RoadRule)}/{nameof(RoadRule)}")]
    [SettingsUITabOrder(kTabGeneral, kTabKeyBindings)]
    [SettingsUIGroupOrder(kGroupGeneral, kGroupDefault, kGroupDisplay, kGroupMainPanel, kGroupKeyBindingReset)]
    [SettingsUIShowGroupName]
    public class Settings : ModSetting
    {
        public const string kTabGeneral = "TabGeneral";
        public const string kGroupGeneral = "GroupGeneral";
        public const string kGroupDefault = "GroupDefault";
        public const string kGroupDisplay = "GroupDisplay";
        public const string kGroupVersion = "GroupVersion";

        public const string kTabKeyBindings = "TabKeyBindings";
        public const string kGroupMainPanel = "GroupMainPanel";
        public const string kKeyboardBindingMainPanelToggle = "KeyboardBindingMainPanelToggle";
        public const string kGroupKeyBindingReset = "GroupKeyBindingReset";

        public struct Values { }

        [SettingsUIHidden]
        public Dictionary<string, string> m_Storage;

        [SettingsUISection(kTabGeneral, kGroupVersion)]
        public string ReleaseChannel => Mod.ReleaseChannel();

        [SettingsUISection(kTabGeneral, kGroupVersion)]
        public string Version => Mod.m_InformationalVersion;

        [SettingsUIKeyboardBinding(BindingKeyboard.None, kKeyboardBindingMainPanelToggle)]
        [SettingsUISection(kTabKeyBindings, kGroupMainPanel)]
        public ProxyBinding MainPanelToggleKeyboardBinding { get; set; }

        [SettingsUISection(kTabKeyBindings, kGroupKeyBindingReset)]
        [SettingsUIButton]
        [SettingsUIConfirmation(null, null)]
        public bool ResetBindings
        {
            set { ResetKeyBindings(); }
        }

        public Settings(IMod mod)
            : base(mod)
        {
            SetDefaults();
            AssetDatabase.global.LoadSettings(nameof(RoadRule), this);
            RegisterInOptionsUI();
            RegisterKeyBindings();
        }

        public override void SetDefaults()
        {
            m_Storage = new Dictionary<string, string>();
        }

        public override void Apply()
        {
            base.Apply();
            RegisterInOptionsUI();
            RegisterKeyBindings();
        }
    }
}
