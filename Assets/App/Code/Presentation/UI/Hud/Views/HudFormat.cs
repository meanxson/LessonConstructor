using System.Globalization;
using App.Code.Domain.Execution;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.Code.Presentation.UI.Hud.Views
{
    public static class HudFormat
    {
        public static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        public static string Shorten(string value, int length) =>
            string.IsNullOrEmpty(value) || value.Length <= length ? value ?? string.Empty : value.Substring(0, length - 1) + "…";

        public static string StatusColor(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Completed: return "#7bed9f";
                case StepStatus.Failed: return "#ff6b6b";
                case StepStatus.Skipped: return "#ffd166";
                default: return "#c8d6e5";
            }
        }

        public static Button CreateButton(Button template, Transform parent, string label, UnityAction onClick)
        {
            var button = Object.Instantiate(template, parent);
            button.name = label;
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = label;
            button.onClick.AddListener(onClick);
            return button;
        }

        public static void Clear(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--) Object.Destroy(parent.GetChild(index).gameObject);
        }
    }
}
