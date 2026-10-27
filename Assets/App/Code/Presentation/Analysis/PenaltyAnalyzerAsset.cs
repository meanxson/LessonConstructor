using App.Code.Domain.Analysis;
using UnityEngine;

namespace App.Code.Presentation.Analysis
{
    [CreateAssetMenu(menuName = "Lesson Constructor/Analysis/Penalty Analyzer", fileName = "PenaltyAnalyzer")]
    public sealed class PenaltyAnalyzerAsset : AnalyzerAsset
    {
        [SerializeField] private int baseScore = 100;
        [SerializeField] private int failedPenalty = 10;
        [SerializeField] private int skippedPenalty = 5;

        public override IResultAnalyzer Create() => new PenaltyAnalyzer(baseScore, failedPenalty, skippedPenalty);
    }
}
