using App.Code.Domain.Execution;
using UnityEngine;

namespace App.Code.Presentation.UI.Hud
{
    [CreateAssetMenu(menuName = "Lesson Constructor/UI/HUD Texts", fileName = "HudTexts")]
    public sealed class HudTexts : ScriptableObject
    {
        [SerializeField] private string activeTasks = "Active tasks";
        [SerializeField] private string mode = "Mode";
        [SerializeField] private string group = "Group";
        [SerializeField] private string groupViolations = "Violations in group";
        [SerializeField] private string progress = "Done";
        [SerializeField] private string finished = "Scenario finished";
        [SerializeField] private string notStarted = "Choose a mode to start";
        [SerializeField] private string outOfTurn = "Action is out of turn";
        [SerializeField] private string retry = "try the step again";
        [SerializeField] private string failed = "step failed";
        [SerializeField] private string result = "Result";
        [SerializeField] private string score = "Score";
        [SerializeField] private string attempt = "attempt";
        [SerializeField] private string violations = "violations";
        [SerializeField] private string warnings = "Warnings";
        [SerializeField] private string journal = "Journal";
        [SerializeField] private string journalSource = "Source";
        [SerializeField] private string analyzeWith = "Score with";
        [SerializeField] private string statusPending = "Pending";
        [SerializeField] private string statusActive = "Active";
        [SerializeField] private string statusCompleted = "Completed";
        [SerializeField] private string statusFailed = "Failed";
        [SerializeField] private string statusSkipped = "Skipped";

        public string ActiveTasks => activeTasks;
        public string Mode => mode;
        public string Group => group;
        public string GroupViolations => groupViolations;
        public string Progress => progress;
        public string Finished => finished;
        public string NotStarted => notStarted;
        public string OutOfTurn => outOfTurn;
        public string Retry => retry;
        public string Failed => failed;
        public string Result => result;
        public string Score => score;
        public string Attempt => attempt;
        public string Violations => violations;
        public string Warnings => warnings;
        public string Journal => journal;
        public string JournalSource => journalSource;
        public string AnalyzeWith => analyzeWith;

        public string Status(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Active: return statusActive;
                case StepStatus.Completed: return statusCompleted;
                case StepStatus.Failed: return statusFailed;
                case StepStatus.Skipped: return statusSkipped;
                default: return statusPending;
            }
        }
    }
}
