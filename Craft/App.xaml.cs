// <copyright file="App.xaml.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System.Threading.Tasks;
using System.Windows;
using Craft.Views.Windows;
using Craft.GlobalOperations;

namespace Craft
{
    public partial class App : ISingleInstanceApp
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();

            if (e.Args != null && e.Args.Length == 1)
            {
                Curfew.Curfew.Dispatcher.Dispatch(new LoadFileOperation(e.Args[0]));
            }

            MainWindow.Show();
        }

        public Task OnSignalFromAnotherInstance(SingleInstance.Message message)
        {
            if (message?.Args == null || message.Args.Count != 1)
            {
                return Task.CompletedTask;
            }

            Curfew.Curfew.Dispatcher.Dispatch(new LoadFileOperation(message.Args[0]));
            return Task.CompletedTask;
        }
    }
}
