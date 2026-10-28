using System.Collections.Generic;
using System.Linq;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;
using App.Code.Presentation.UI.Hud.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace App.Code.Presentation.UI.Hud.Views
{
    public sealed class ControlPanelWindow : BaseWindow<ControlPanelViewModel, ControlPanelState>
    {
        [SerializeField] private RectTransform modeButtons;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button journalButton;

        private IReadOnlyList<string> _shownModes = new string[0];

        protected override void OnEnable()
        {
            base.OnEnable();
            restartButton.onClick.AddListener(OnRestart);
            journalButton.onClick.AddListener(OnJournal);
        }

        protected override void OnDisable()
        {
            restartButton.onClick.RemoveListener(OnRestart);
            journalButton.onClick.RemoveListener(OnJournal);
            base.OnDisable();
        }

        protected override void OnStateChanged(ControlPanelState state)
        {
            if (_shownModes.SequenceEqual(state.ModeNames)) return;

            _shownModes = state.ModeNames;
            HudFormat.Clear(modeButtons);
            for (var index = 0; index < state.ModeNames.Count; index++)
            {
                var modeIndex = index;
                HudFormat.CreateButton(buttonTemplate, modeButtons, state.ModeNames[index], () => ViewModel.StartMode(modeIndex));
            }
        }

        private void OnRestart() => ViewModel.Restart();

        private void OnJournal() => ViewModel.ToggleJournal();
    }
}
