using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace System.Reactive.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<T> ConnectFirst<T>(this IConnectableObservable<T> target, Action onConnect = null)
        {
            return Observable.Create<T>(observer =>
            {
                var subscription = target.Subscribe(observer);
                target.Connect();
                onConnect?.Invoke();
                return subscription;
            });
        }
    }
}
