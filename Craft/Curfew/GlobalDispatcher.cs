// <copyright file="GlobalDispatcher.cs">
//     Copyright (c) 2018 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Subjects;

namespace Curfew
{
    public class GlobalDispatcher
    {
        private Subject<IImmutableStateObject> firehose;

        public GlobalDispatcher()
        {
            firehose = new Subject<IImmutableStateObject>();
        }

        public void Dispatch(IImmutableStateObject state)
        {
            firehose.OnNext(state);
        }

        private Registration Register(Action<IImmutableStateObject[]> process)
        {
            return new Registration(firehose, _ => true, process);
        }

        private Registration Register(Func<IImmutableStateObject, bool> predicate, Action<IImmutableStateObject[]> process)
        {
            return new Registration(firehose, predicate, process);
        }

        public Registration RegisterLast<T>(Action<T> process)
          where T : IImmutableStateObject
        {
            return Register(x => x is T, x => process((T)x.Last()));
        }

        public Registration RegisterLast<T>(Func<T, bool> predicate, Action<T> process)
          where T : IImmutableStateObject
        {
            return Register(x => x is T && predicate((T)x), x => process((T)x.Last()));
        }
    }
}
