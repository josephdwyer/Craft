// <copyright file="App.xaml.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Windows;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Craft
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] commandLineArgs)
        {
            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value;

            if (SingleInstance.InitializeAsFirstInstance(appGuid, commandLineArgs))
            {
                SplashScreen splashScreen;
                splashScreen = new SplashScreen("Resources/icon.png");
                splashScreen.Show(true);

                App app = new App();
                app.InitializeComponent();
                try
                {
                    app.Run();
                }
                finally
                {
                    SingleInstance.Cleanup();
                }
            }
        }
    }
}
