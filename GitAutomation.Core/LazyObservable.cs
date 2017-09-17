using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace GitAutomation
{
    class LazyObservable<T> : IObservable<T>, IDisposable
    {
        private readonly Lazy<(IObservable<T> observable, IDisposable subscription)> lazy;

        public LazyObservable(IConnectableObservable<T> target)
        {
            lazy = new Lazy<(IObservable<T> observable, IDisposable subscription)>(() =>
            {
                var subscription = target.Connect();
                return (observable: target, subscription);
            });
        }

        public void Dispose()
        {
            if (lazy.IsValueCreated)
            {
                lazy.Value.subscription.Dispose();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return lazy.Value.observable.Subscribe(observer);
        }
    }
}
