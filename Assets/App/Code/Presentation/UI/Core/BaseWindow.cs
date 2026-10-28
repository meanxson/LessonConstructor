using UnityEngine;

namespace App.Code.Presentation.UI.Core
{
    public abstract class BaseWindow : MonoBehaviour
    {
        public void Open() => gameObject.SetActive(true);

        public void Close() => gameObject.SetActive(false);
    }

    public abstract class BaseWindow<TViewModel, TState> : BaseWindow
        where TViewModel : BaseWindowViewModel<TState>
        where TState : class, new()
    {
        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        private bool _isInitialized;

        protected TViewModel ViewModel { get; private set; }

        public void Construct(TViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        protected virtual void Start()
        {
            if (ViewModel == null)
            {
                Debug.LogError($"[{GetType().Name}] ViewModel is not constructed. Check HudCompositionRoot.", this);
                return;
            }

            _isInitialized = true;
            ViewModel.Initialize();
            Bind();
            ViewModel.OnViewOpened();
        }

        protected virtual void OnEnable()
        {
            if (_isInitialized) ViewModel.OnViewOpened();
        }

        protected virtual void OnDisable()
        {
            if (_isInitialized) ViewModel.OnViewClosed();
        }

        protected virtual void OnDestroy()
        {
            Disposables.Dispose();
            ViewModel?.Dispose();
        }

        protected virtual void Bind() => Disposables.Add(ViewModel.StateObservable.Subscribe(OnStateChanged));

        protected abstract void OnStateChanged(TState state);
    }
}
