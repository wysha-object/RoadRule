using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Input;
using UnityEngine.InputSystem;

namespace RoadRule.Systems.UI
{
    public partial class UISystem
    {
        private ProxyAction m_MainPanelToggleKeyboardBinding;

        private void SetupKeyBindings()
        {
            m_MainPanelToggleKeyboardBinding = Mod.m_Settings.GetAction(Settings.kKeyboardBindingMainPanelToggle);
            m_MainPanelToggleKeyboardBinding.shouldBeEnabled = true;
            m_MainPanelToggleKeyboardBinding.onInteraction += MainPanelToggle;
        }

        private void MainPanelToggle(ProxyAction action, InputActionPhase phase)
        {
            if (Enabled && phase == InputActionPhase.Performed)
            {
                if (GetToolState() != ToolState.Disabled)
                {
                    SetToolState(ToolState.Disabled);
                }
                else
                {
                    SetToolState(ToolState.Choosing);
                }
            }
        }
    }
}
