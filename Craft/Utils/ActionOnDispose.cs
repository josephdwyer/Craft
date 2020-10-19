// <copyright file="ActionOnDispose.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System;

namespace Craft.Utils
{
    /// <summary>
    /// Helper that runs the given action when disposed, useful for deferring some code execution.
    /// </summary>
    public class ActionOnDispose : IDisposable
    {
        private Action action;
        private bool isDisposed;

        public ActionOnDispose(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                action();
                isDisposed = true;
            }
        }
    }
}
