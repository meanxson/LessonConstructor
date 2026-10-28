using App.Code.Presentation.Runner;
using App.Code.Presentation.UI.Hud.ViewModels;
using App.Code.Presentation.UI.Hud.Views;
using UnityEngine;

namespace App.Code.Presentation.UI.Hud
{
    [DefaultExecutionOrder(-100)]
    public sealed class HudCompositionRoot : MonoBehaviour
    {
        [SerializeField] private ScenarioRunner runner;
        [SerializeField] private TasksWindow tasksWindow;
        [SerializeField] private MessageWindow messageWindow;
        [SerializeField] private ControlPanelWindow controlPanelWindow;
        [SerializeField] private ResultWindow resultWindow;
        [SerializeField] private JournalWindow journalWindow;

        private void Awake()
        {
            if (runner == null)
            {
                Debug.LogError("[HudCompositionRoot] Runner is not assigned", this);
                return;
            }

            var messages = new MessageViewModel(runner);
            var journal = new JournalViewModel(runner);

            tasksWindow.Construct(new TasksViewModel(runner));
            messageWindow.Construct(messages);
            journalWindow.Construct(journal);
            controlPanelWindow.Construct(new ControlPanelViewModel(runner, journal));
            resultWindow.Construct(new ResultViewModel(runner, messages));
        }
    }
}
