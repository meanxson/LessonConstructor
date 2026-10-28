using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;
using App.Code.Presentation.UI.Hud.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Code.Presentation.UI.Hud.Views
{
    public sealed class ResultWindow : BaseWindow<ResultViewModel, ResultState>
    {
        [SerializeField] private HudTexts texts;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform analyzerButtons;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Button loadFileButton;
        [SerializeField] private Button hideButton;

        private IReadOnlyList<string> _shownAnalyzers = new string[0];

        protected override void OnEnable()
        {
            base.OnEnable();
            loadFileButton.onClick.AddListener(OnLoadFile);
            hideButton.onClick.AddListener(OnHide);
        }

        protected override void OnDisable()
        {
            loadFileButton.onClick.RemoveListener(OnLoadFile);
            hideButton.onClick.RemoveListener(OnHide);
            base.OnDisable();
        }

        protected override void OnStateChanged(ResultState state)
        {
            RebuildAnalyzerButtons(state.AnalyzerNames);
            panel.SetActive(state.IsVisible);
            if (!state.IsVisible) return;

            var builder = new StringBuilder();
            builder.Append("<b>").Append(texts.Result).Append(": ").Append(state.AnalyzerName).Append("</b>");
            if (state.Mode.Length > 0) builder.Append(" (").Append(state.Mode).Append(')');
            builder.AppendLine();
            builder.Append("<size=130%><b>").Append(texts.Score).Append(": ").Append(HudFormat.Number(state.Score)).Append(" / ")
                .Append(HudFormat.Number(state.MaxScore)).AppendLine("</b></size>");
            builder.Append(texts.JournalSource).Append(": ").AppendLine(state.Source).AppendLine();

            foreach (var line in state.Lines)
            {
                builder.Append("<color=").Append(HudFormat.StatusColor(line.Status)).Append('>').Append(texts.Status(line.Status))
                    .Append("</color>  ").Append(line.StepId);
                if (line.Title.Length > 0) builder.Append(" <color=#9aa5b1>").Append(HudFormat.Shorten(line.Title, 26)).Append("</color>");
                if (line.Attempts > 0) builder.Append("  ").Append(texts.Attempt).Append(' ').Append(line.Attempts);
                if (line.Violations > 0) builder.Append(", ").Append(texts.Violations).Append(' ').Append(line.Violations);
                builder.Append("  <b>").Append(line.Points > 0 ? "+" : string.Empty).Append(HudFormat.Number(line.Points)).AppendLine("</b>");
            }

            if (state.Warnings.Count > 0)
            {
                builder.AppendLine().Append("<color=#ffd166><b>").Append(texts.Warnings).AppendLine(":</b>");
                foreach (var warning in state.Warnings) builder.Append("• ").AppendLine(warning);
                builder.Append("</color>");
            }

            label.text = builder.ToString();
        }

        private void RebuildAnalyzerButtons(IReadOnlyList<string> names)
        {
            if (_shownAnalyzers.SequenceEqual(names)) return;

            _shownAnalyzers = names;
            HudFormat.Clear(analyzerButtons);
            for (var index = 0; index < names.Count; index++)
            {
                var analyzerIndex = index;
                HudFormat.CreateButton(buttonTemplate, analyzerButtons, $"{texts.AnalyzeWith}: {names[index]}", () => ViewModel.Analyze(analyzerIndex));
            }
        }

        private void OnLoadFile() => ViewModel.LoadLatestJournal();

        private void OnHide() => ViewModel.Hide();
    }
}
