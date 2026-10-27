using App.Code.Domain.Analysis;
using UnityEngine;

namespace App.Code.Presentation.Analysis
{
    [CreateAssetMenu(menuName = "Lesson Constructor/Analysis/Attempt Score Analyzer", fileName = "AttemptScoreAnalyzer")]
    public sealed class AttemptScoreAnalyzerAsset : AnalyzerAsset
    {
        [SerializeField] private int firstAttemptPoints = 10;
        [SerializeField] private int secondAttemptPoints = 5;
        [SerializeField] private int laterAttemptPoints;

        public override IResultAnalyzer Create() => new AttemptScoreAnalyzer(firstAttemptPoints, secondAttemptPoints, laterAttemptPoints);
    }
}
