using System.Linq;
using System.Text;
using App.Code.Presentation.Logging;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;
using App.Code.Presentation.UI.Hud.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Code.Presentation.UI.Hud.Views
{
    public sealed class JournalWindow : BaseWindow<JournalViewModel, JournalState>
    {
        private const int LineLimit = 400;

        [SerializeField] private HudTexts texts;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private ScrollRect scroll;

        protected override void OnStateChanged(JournalState state)
        {
            panel.SetActive(state.IsVisible);
            if (!state.IsVisible) return;

            var records = state.Records;
            var builder = new StringBuilder();
            builder.Append("<b>").Append(texts.Journal).Append("</b> (").Append(texts.JournalSource).Append(": ")
                .Append(state.Source).Append(", ").Append(records.Count).AppendLine(")");
            foreach (var record in records.Skip(Mathf.Max(0, records.Count - LineLimit))) builder.AppendLine(JournalFormatter.Line(record));

            label.text = builder.ToString();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }
}
