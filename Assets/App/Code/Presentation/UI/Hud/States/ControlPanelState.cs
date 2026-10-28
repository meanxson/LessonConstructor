using System.Collections.Generic;

namespace App.Code.Presentation.UI.Hud.States
{
    public sealed class ControlPanelState
    {
        public ControlPanelState()
        {
        }

        public ControlPanelState(IReadOnlyList<string> modeNames, string currentModeName)
        {
            ModeNames = modeNames ?? new string[0];
            CurrentModeName = currentModeName ?? string.Empty;
        }

        public IReadOnlyList<string> ModeNames { get; } = new string[0];

        public string CurrentModeName { get; } = string.Empty;
    }
}
