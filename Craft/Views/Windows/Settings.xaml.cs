// <copyright file="Settings.xaml.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using Craft.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Craft.Views.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private readonly string gitConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitconfig");

        public Settings()
        {
            InitializeComponent();

            gitConfigFilePath.Text = gitConfigPath;
            gitConfigFilePathLink.Click += GitConfigFilePathLink_Click;

            configurationExample.Text = $@"[core]
	editor = {Process.GetCurrentProcess().MainModule.FileName}
";
        }

        private void GitConfigFilePathLink_Click(object sender, RoutedEventArgs e)
        {
            ProcessUtils.SafeOpen(gitConfigPath);
            e.Handled = true;
        }
    }
}
