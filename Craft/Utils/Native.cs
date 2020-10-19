// <copyright file="Native.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System.Runtime.InteropServices;

namespace Craft.Utils
{
    public static class Native
    {
        [DllImport("user32.dll")]
        public static extern uint GetDoubleClickTime();
    }
}
