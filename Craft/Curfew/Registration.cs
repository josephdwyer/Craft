// <copyright file="Registration.cs">
//     Copyright (c) 2018 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Curfew
{
    public class Registration : IObserver<IImmutableStateObject>
    {
        private IDisposable firehoseSubscription;
        private Queue<IImmutableStateObject> messageQueue;
        private Action<IImmutableStateObject[]> process;
        private bool emit = true;
        private static object @lock = new object();

        public Registration(IObservable<IImmutableStateObject> firehose, Func<IImmutableStateObject, bool> predicate, Action<IImmutableStateObject[]> process)
        {
            this.process = process;
            firehoseSubscription = firehose.Where(predicate).Subscribe(this);
            messageQueue = new Queue<IImmutableStateObject>();
        }

        public void Resume()
        {
            lock (@lock)
            {
                emit = true;
                Emit();
            }
        }

        public void Pause()
        {
            // the idea here is that when a view is not visible / backgrounded it can
            // stop processing messages as a way of keeping the MainThread clear
            // e.g. no reason to render updated values while a window is minimized
            // Also, since the ViewState is represented by a single immutable object,
            // it should be easy to implement something like the Android lifecycle where
            // we destroy a view and recreate it with the last state. This could be used in
            // conjuction with pausing for a seamless experience.
            // AKA: Pause the subscriptions, destroy the view...
            // ViewDispatcher should only have one property which is the ViewState
            // When we need to recreate the view, we just need to run the constructor
            // send the last state to the view and resume the subscriptions to be notified of updates from
            // other places in the application.
            lock (@lock)
            {
                emit = false;
            }
        }

        public void Unregister()
        {
            firehoseSubscription.Dispose();
        }

        private void Emit()
        {
            if (emit)
            {
                IImmutableStateObject[] messages = messageQueue.ToArray();
                messageQueue.Clear();
                process(messages);
            }
        }

        void IObserver<IImmutableStateObject>.OnNext(IImmutableStateObject value)
        {
            lock (@lock)
            {
                messageQueue.Enqueue(value);
                Emit();
            }
        }

        void IObserver<IImmutableStateObject>.OnError(Exception error)
        {
        }

        void IObserver<IImmutableStateObject>.OnCompleted()
        {
            firehoseSubscription.Dispose();
        }
    }
}
