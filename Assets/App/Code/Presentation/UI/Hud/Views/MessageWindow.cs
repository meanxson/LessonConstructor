using System.Text;
using App.Code.Domain.Execution;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;
using App.Code.Presentation.UI.Hud.ViewModels;
using TMPro;
using UnityEngine;

namespace App.Code.Presentation.UI.Hud.Views
{
    public sealed class MessageWindow : BaseWindow<MessageViewModel, MessageState>
    {
        [SerializeField] private HudTexts texts;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.5f)] private float visibleSeconds = 3.5f;

        private float _hideAt;

        protected override void Start()
        {
            panel.SetActive(false);
            base.Start();
        }

        private void Update()
        {
            if (panel.activeSelf && Time.unscaledTime >= _hideAt) panel.SetActive(false);
        }

        protected override void OnStateChanged(MessageState state)
        {
            if (state.Kind == MessageKind.None) return;

            var builder = new StringBuilder();
            if (state.Kind == MessageKind.Info)
            {
                builder.Append(state.Text);
            }
            else
            {
                var reason = state.Kind == MessageKind.OutOfTurn ? texts.OutOfTurn : state.Text;
                builder.Append("<b>").Append(reason).AppendLine("</b>").Append(state.Title);
                if (state.Kind == MessageKind.Violation)
                    builder.Append(" — ").Append(state.Response == ViolationResponse.Retry ? texts.Retry : texts.Failed);
            }

            label.text = builder.ToString();
            panel.SetActive(true);
            _hideAt = Time.unscaledTime + visibleSeconds;
        }
    }
}
