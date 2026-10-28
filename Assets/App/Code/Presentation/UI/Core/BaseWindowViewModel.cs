using System;

namespace App.Code.Presentation.UI.Core
{
    public abstract class BaseWindowViewModel<TState> : IDisposable
        where TState : class, new()
    {
        private readonly ObservableValue<TState> _state = new ObservableValue<TState>(new TState());
        private bool _isInitialized;
        private bool _isDisposed;

        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        public TState State => _state.Value;

        public IReadOnlyObservableValue<TState> StateObservable => _state;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            OnInitialize();
        }

        public void OnViewOpened()
        {
            if (!_isInitialized) Initialize();
            OnViewOpenedInternal();
        }

        public void OnViewClosed() => OnViewClosedInternal();

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            OnDispose();
            Disposables.Dispose();
            _state.Dispose();
        }

        protected void UpdateState(Func<TState, TState> updater)
        {
            if (updater == null || _isDisposed) return;
            _state.Value = updater(_state.Value);
        }

        protected void SetState(TState state)
        {
            if (state == null || _isDisposed) return;
            _state.Value = state;
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnViewOpenedInternal()
        {
        }

        protected virtual void OnViewClosedInternal()
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}
