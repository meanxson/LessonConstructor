using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using App.Code.Domain.Journal;

namespace App.Code.Domain.Execution
{
    public readonly struct ViolationNotice
    {
        public ViolationNotice(string stepId, string reason, bool outOfTurn, ViolationResponse response)
        {
            StepId = stepId;
            Reason = reason;
            OutOfTurn = outOfTurn;
            Response = response;
        }

        public string StepId { get; }

        public string Reason { get; }

        public bool OutOfTurn { get; }

        public ViolationResponse Response { get; }
    }

    public sealed class ScenarioSession
    {
        private sealed class StepState
        {
            public StepState(string id, int groupIndex, int batchIndex)
            {
                Id = id;
                GroupIndex = groupIndex;
                BatchIndex = batchIndex;
            }

            public string Id { get; }

            public int GroupIndex { get; }

            public int BatchIndex { get; }

            public StepStatus Status { get; set; } = StepStatus.Pending;

            public int Attempt { get; set; } = 1;

            public int Violations { get; set; }
        }

        private readonly ScenarioPlan _plan;
        private readonly IExecutionPolicy _policy;
        private readonly OutOfTurnBehaviour _outOfTurn;
        private readonly IScenarioLogger _logger;
        private readonly Func<DateTime> _clock;
        private readonly Dictionary<string, StepState> _steps = new Dictionary<string, StepState>();
        private readonly List<StepState> _orderedSteps = new List<StepState>();

        private DateTime _startedAt;
        private int _sequence;
        private int _groupIndex = -1;
        private int _batchIndex = -1;

        public ScenarioSession(
            ScenarioPlan plan,
            IExecutionPolicy policy,
            OutOfTurnBehaviour outOfTurn,
            IScenarioLogger logger,
            Func<DateTime> clock = null)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _outOfTurn = outOfTurn;
            _clock = clock ?? (() => DateTime.UtcNow);

            for (var groupIndex = 0; groupIndex < plan.Groups.Count; groupIndex++)
            {
                var group = plan.Groups[groupIndex];
                for (var batchIndex = 0; batchIndex < group.Batches.Count; batchIndex++)
                {
                    foreach (var id in group.Batches[batchIndex].StepIds)
                    {
                        var state = new StepState(id, groupIndex, batchIndex);
                        _steps.Add(id, state);
                        _orderedSteps.Add(state);
                    }
                }
            }
        }

        public event Action<string, StepStatus> StepStatusChanged;

        public event Action<ViolationNotice> ViolationRecorded;

        public event Action GroupChanged;

        public event Action Finished;

        public ScenarioPlan Plan => _plan;

        public IExecutionPolicy Policy => _policy;

        public bool IsRunning => _groupIndex >= 0 && !IsFinished;

        public bool IsFinished { get; private set; }

        public int CurrentGroupIndex => _groupIndex;

        public PlanGroup CurrentGroup => _groupIndex >= 0 && _groupIndex < _plan.Groups.Count ? _plan.Groups[_groupIndex] : null;

        public int CurrentGroupViolations { get; private set; }

        public IReadOnlyList<string> ActiveStepIds =>
            _orderedSteps.Where(step => step.Status == StepStatus.Active).Select(step => step.Id).ToArray();

        public bool Contains(string stepId) => stepId != null && _steps.ContainsKey(stepId);

        public StepStatus GetStatus(string stepId) => Find(stepId).Status;

        public int GetAttempt(string stepId) => Find(stepId).Attempt;

        public int GetViolations(string stepId) => Find(stepId).Violations;

        public void Start()
        {
            if (_groupIndex >= 0) throw new InvalidOperationException("Session has already been started");

            _startedAt = _clock();
            Write(new LogRecord(LogRecordTypes.SessionStarted, data: new Dictionary<string, string>
            {
                [LogDataKeys.Mode] = _plan.Name,
                [LogDataKeys.Policy] = _policy.Name,
                [LogDataKeys.OutOfTurn] = _outOfTurn.ToString()
            }));

            foreach (var step in _orderedSteps)
            {
                Write(new LogRecord(LogRecordTypes.StepRegistered, step.Id, group: step.GroupIndex, data: new Dictionary<string, string>
                {
                    [LogDataKeys.GroupName] = _plan.Groups[step.GroupIndex].Name,
                    [LogDataKeys.Batch] = step.BatchIndex.ToString(CultureInfo.InvariantCulture)
                }));
            }

            StartGroup(0);
        }

        public bool Complete(string stepId)
        {
            if (!IsRunning || !_steps.TryGetValue(stepId ?? string.Empty, out var step) || step.Status != StepStatus.Active) return false;

            SetStatus(step, StepStatus.Completed);
            Write(new LogRecord(LogRecordTypes.StepCompleted, step.Id, LogOutcomes.Completed, step.Attempt, step.GroupIndex));
            AdvanceWhileBatchDone();
            return true;
        }

        public bool Violate(string stepId, string reason)
        {
            if (!IsRunning || !_steps.TryGetValue(stepId ?? string.Empty, out var step) || step.Status != StepStatus.Active) return false;

            step.Violations++;
            CurrentGroupViolations++;
            var response = _policy.OnViolation(step.Attempt);
            Write(new LogRecord(LogRecordTypes.StepViolation, step.Id,
                response == ViolationResponse.Retry ? LogOutcomes.Retry : LogOutcomes.Failed,
                step.Attempt, _groupIndex, reason, GroupViolationData()));

            if (response == ViolationResponse.Retry)
            {
                step.Attempt++;
            }
            else
            {
                SetStatus(step, StepStatus.Failed);
                Write(new LogRecord(LogRecordTypes.StepFailed, step.Id, LogOutcomes.Failed, step.Attempt, step.GroupIndex, reason));
            }

            ViolationRecorded?.Invoke(new ViolationNotice(step.Id, reason, false, response));
            AfterViolation();
            return true;
        }

        public bool ReportOutOfTurn(string stepId)
        {
            if (_outOfTurn == OutOfTurnBehaviour.Ignore) return false;
            if (!IsRunning || !_steps.TryGetValue(stepId ?? string.Empty, out var step) || step.Status != StepStatus.Pending) return false;

            step.Violations++;
            CurrentGroupViolations++;
            var data = GroupViolationData();
            data[LogDataKeys.StepGroup] = step.GroupIndex.ToString(CultureInfo.InvariantCulture);
            Write(new LogRecord(LogRecordTypes.StepViolation, step.Id, LogOutcomes.Violation, LogRecord.NoValue, _groupIndex,
                ViolationReasons.OutOfTurn, data));

            ViolationRecorded?.Invoke(new ViolationNotice(step.Id, ViolationReasons.OutOfTurn, true, ViolationResponse.Retry));
            AfterViolation();
            return true;
        }

        public bool ReportProgress(string stepId, string detail)
        {
            if (!IsRunning || !_steps.TryGetValue(stepId ?? string.Empty, out var step) || step.Status != StepStatus.Active) return false;

            Write(new LogRecord(LogRecordTypes.StepProgress, step.Id, attempt: step.Attempt, group: step.GroupIndex,
                data: new Dictionary<string, string> { [LogDataKeys.Detail] = detail ?? string.Empty }));
            return true;
        }

        private StepState Find(string stepId)
        {
            if (stepId == null || !_steps.TryGetValue(stepId, out var step))
                throw new ArgumentException($"Step '{stepId}' is not part of the plan", nameof(stepId));
            return step;
        }

        private Dictionary<string, string> GroupViolationData() => new Dictionary<string, string>
        {
            [LogDataKeys.GroupViolations] = CurrentGroupViolations.ToString(CultureInfo.InvariantCulture)
        };

        private void AfterViolation()
        {
            if (!IsRunning) return;

            if (_policy.IsGroupExhausted(CurrentGroupViolations))
            {
                AbortGroup();
                return;
            }

            AdvanceWhileBatchDone();
        }

        private void StartGroup(int groupIndex)
        {
            if (groupIndex >= _plan.Groups.Count)
            {
                FinishSession();
                return;
            }

            _groupIndex = groupIndex;
            _batchIndex = -1;
            CurrentGroupViolations = 0;
            Write(new LogRecord(LogRecordTypes.GroupStarted, group: groupIndex, data: new Dictionary<string, string>
            {
                [LogDataKeys.GroupName] = _plan.Groups[groupIndex].Name
            }));
            GroupChanged?.Invoke();
            ActivateBatch(0);
            AdvanceWhileBatchDone();
        }

        private void ActivateBatch(int batchIndex)
        {
            _batchIndex = batchIndex;
            foreach (var id in CurrentGroup.Batches[batchIndex].StepIds)
            {
                var step = _steps[id];
                if (step.Status != StepStatus.Pending) continue;

                SetStatus(step, StepStatus.Active);
                Write(new LogRecord(LogRecordTypes.StepActivated, step.Id, attempt: step.Attempt, group: step.GroupIndex,
                    data: new Dictionary<string, string> { [LogDataKeys.Batch] = batchIndex.ToString(CultureInfo.InvariantCulture) }));
            }
        }

        private void AdvanceWhileBatchDone()
        {
            while (IsRunning && CurrentGroup.Batches[_batchIndex].StepIds.All(id => _steps[id].Status.IsTerminal()))
            {
                if (_batchIndex + 1 < CurrentGroup.Batches.Count)
                {
                    ActivateBatch(_batchIndex + 1);
                    continue;
                }

                FinishGroup(LogOutcomes.Completed);
                StartGroup(_groupIndex + 1);
            }
        }

        private void AbortGroup()
        {
            foreach (var step in _orderedSteps.Where(step => step.GroupIndex == _groupIndex && !step.Status.IsTerminal()))
            {
                SetStatus(step, StepStatus.Skipped);
                Write(new LogRecord(LogRecordTypes.StepSkipped, step.Id, LogOutcomes.Skipped, step.Attempt, step.GroupIndex,
                    "group_violation_limit"));
            }

            FinishGroup(LogOutcomes.Aborted);
            StartGroup(_groupIndex + 1);
        }

        private void FinishGroup(string outcome)
        {
            Write(new LogRecord(LogRecordTypes.GroupFinished, outcome: outcome, group: _groupIndex, data: GroupViolationData()));
        }

        private void FinishSession()
        {
            IsFinished = true;
            Write(new LogRecord(LogRecordTypes.SessionFinished, data: new Dictionary<string, string>
            {
                [LogDataKeys.Mode] = _plan.Name
            }));
            GroupChanged?.Invoke();
            Finished?.Invoke();
        }

        private void SetStatus(StepState step, StepStatus status)
        {
            step.Status = status;
            StepStatusChanged?.Invoke(step.Id, status);
        }

        private void Write(LogRecord record)
        {
            var now = _clock();
            _sequence++;
            _logger.Write(record.Stamp(_sequence, (now - _startedAt).TotalSeconds, now.ToString("o", CultureInfo.InvariantCulture)));
        }
    }
}
