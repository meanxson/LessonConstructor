using System.Text;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;
using App.Code.Presentation.UI.Hud.ViewModels;
using TMPro;
using UnityEngine;

namespace App.Code.Presentation.UI.Hud.Views
{
    public sealed class TasksWindow : BaseWindow<TasksViewModel, TasksState>
    {
        [SerializeField] private HudTexts texts;
        [SerializeField] private TMP_Text label;

        protected override void OnStateChanged(TasksState state)
        {
            var builder = new StringBuilder();
            if (state.Error.Length > 0) builder.Append("<color=#ff6b6b>").Append(state.Error).AppendLine("</color>");

            if (state.Total == 0)
            {
                label.text = builder.Append(texts.NotStarted).ToString();
                return;
            }

            builder.Append("<b>").Append(texts.Mode).Append(": ").Append(state.ModeName).AppendLine("</b>");
            builder.Append(texts.Progress).Append(": ").Append(state.Done).Append('/').Append(state.Total).AppendLine();

            if (state.IsFinished)
            {
                label.text = builder.AppendLine().Append("<color=#7bed9f>").Append(texts.Finished).Append("</color>").ToString();
                return;
            }

            if (state.ShowGroup)
            {
                builder.Append(texts.Group).Append(' ').Append(state.GroupNumber).Append('/').Append(state.GroupCount);
                if (state.GroupName.Length > 0) builder.Append(" — ").Append(state.GroupName);
                builder.AppendLine();
                builder.Append(texts.GroupViolations).Append(": ").Append(state.GroupViolations);
                if (state.ViolationLimit > 0) builder.Append('/').Append(state.ViolationLimit);
                builder.AppendLine();
            }

            builder.AppendLine().Append("<b>").Append(texts.ActiveTasks).AppendLine(":</b>");
            foreach (var task in state.Tasks)
            {
                builder.Append("• ").Append(task.Title);
                if (task.Attempt > 1) builder.Append(" <color=#ffd166>(").Append(texts.Attempt).Append(' ').Append(task.Attempt).Append(")</color>");
                builder.AppendLine();
            }

            label.text = builder.ToString();
        }
    }
}
