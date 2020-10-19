// <copyright file="ViewDispatcher.cs">
//     Copyright (c) 2018 Joseph Dwyer All rights reserved.
// </copyright>

using System.Linq;
using System.Reactive.Linq;

namespace Curfew
{
    /// <summary>
    /// Fills a similar role as a ViewModel, but:
    ///   * should not contain individual state properties instead use LastState
    ///   * should register to anything interesting from the the global dispatcher <see cref="Curfew.Dispatcher"/>
    ///   * should Dispatch new <see cref="TViewState"/> states to be consumed by the view
    /// </summary>
    public class ViewDispatcher<TViewState> : GlobalDispatcher where TViewState : IImmutableStateObject
    {
        public TViewState LastState { get; private set; }

        public ViewDispatcher()
        {
            RegisterLast<TViewState>(UpdateLastState);
        }

        /// <summary>
        /// Add as the listener to update <see cref="LastState"/>
        /// </summary>
        private void UpdateLastState(TViewState lastState)
        {
            LastState = lastState;
        }
    }
}
