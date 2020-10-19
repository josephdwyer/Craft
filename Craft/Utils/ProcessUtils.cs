// <copyright file="ProcessUtils.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Diagnostics;

namespace Craft.Utils
{
    public static class ProcessUtils
    {
        /// <summary>
        /// Have windows open the given file. (UseShellExecute = true)
        /// </summary>
        public static void SafeOpen(string path)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
