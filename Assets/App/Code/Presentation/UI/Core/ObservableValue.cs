using System;
using System.Collections.Generic;

namespace App.Code.Presentation.UI.Core
{
    public interface IReadOnlyObservableValue<out T>
    {
        T Value { get; }

        IDisposable Subscribe(Action<T> observer);
    }

    public sealed class ObservableValue<T> : IReadOnlyObservableValue<T>, IDisposable
    {
        private readonly List<Action<T>> _observers = new List<Action<T>>();
        private T _value;
        private bool _isDisposed;

        public ObservableValue(T initial)
        {
            _value = initial;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (_isDisposed) return;
                _value = value;
                foreach (var observer in _observers.ToArray()) observer(value);
            }
        }

        public IDisposable Subscribe(Action<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (_isDisposed) return Subscription.Empty;

            _observers.Add(observer);
            observer(_value);
            return new Subscription(() => _observers.Remove(observer));
        }

        public void Dispose()
        {
            _isDisposed = true;
            _observers.Clear();
        }
    }

    public sealed class Subscription : IDisposable
    {
        public static readonly IDisposable Empty = new Subscription(null);

        private Action _dispose;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }

    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _items = new List<IDisposable>();

        public void Add(IDisposable item)
        {
            if (item != null) _items.Add(item);
        }

        public void Dispose()
        {
            foreach (var item in _items) item.Dispose();
            _items.Clear();
        }
    }
}
