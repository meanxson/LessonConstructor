using App.Code.Domain.Analysis;
using UnityEngine;

namespace App.Code.Presentation.Analysis
{
    public abstract class AnalyzerAsset : ScriptableObject
    {
        [SerializeField] private string displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public abstract IResultAnalyzer Create();
    }
}
