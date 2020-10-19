// <copyright file="Curfew.cs">
//     Copyright (c) 2018 Joseph Dwyer All rights reserved.
// </copyright>

using System;

namespace Curfew
{
    public static class Curfew
    {
        private static Lazy<GlobalDispatcher> instance = new Lazy<GlobalDispatcher>(() => new GlobalDispatcher());

        /// <summary>
        /// Global dispatcher used to communicate changes from one area of an application to another.
        /// These messages should be view-agnostic / data model focused
        /// e.g. A to do item created in one window needs to show up on another window or summary view.
        /// </summary>
        public static GlobalDispatcher Dispatcher => instance.Value;
    }
}
