// <copyright file="MainWindow.xaml.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Curfew;
using Craft.ViewDispatchers.MainWindow;
using Craft.Utils;

namespace Craft.Views.Windows
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindowDependencies
    {
        public MainWindowViewDispatcher ViewDispatcher { get; }
        private readonly IRxCommand escape;

        public MainWindow()
        {
            escape = RxCommand.Create(EscapeImpl);
            ViewDispatcher = new MainWindowViewDispatcher(this);

            Title = "Craft";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            InitializeComponent();

            ViewDispatcher.RegisterLast<MainWindowState>(SetState);
            // trigger ^ with anything set up already
            ViewDispatcher.Dispatch(ViewDispatcher.LastState);
        }

        private void SetState(MainWindowState state)
        {
            message.TextChanged -= Message_TextChanged;
            message.Text = state.Message;
            message.SelectionStart = state.SelectionStart;
            message.SelectionLength = 0;
            message.TextChanged += Message_TextChanged;

            submit.Command = state.Save;
            emoji.Command = state.Help;
            settings.Command = state.Settings;

            suggestionList.ItemConfirmed -= OnSuggestionConfirmed;
            suggestionList.ItemsSource = state.Suggestions;
            suggestionList.ItemConfirmed += OnSuggestionConfirmed;

            if (state.Focus)
            {
                message.Focus();
            }
            if (state.Close)
            {
                Close();
            }

            InputBindings.Clear();
            InputBindings.Add(new KeyBinding(state.Save, new KeyGesture(Key.S, ModifierKeys.Control)));

            if (!state.HasUserEdited)
            {
                InputBindings.Add(new KeyBinding(escape, new KeyGesture(Key.Escape)));
            }
        }

        private void OnSuggestionConfirmed(object sender, ListViewItem e)
        {
            if (e.DataContext is SemanticEmoji emoji)
            {
                ViewDispatcher.Dispatch(new SuggestionSelectedOperation(emoji));
            }
        }

        //private void Message_KeyUp(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Tab
        //        && !string.IsNullOrWhiteSpace(ViewDispatcher.LastState.SearchTerm.term)
        //        && !ViewDispatcher.LastState.Suggestions.IsEmpty)
        //    {
        //        ViewDispatcher.Dispatch(new SuggestionSelectedOperation(ViewDispatcher.LastState.Suggestions.First()));
        //        e.Handled = true;
        //    }
        //}

        private void Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                ViewDispatcher.Dispatch(new CommitMessageEditOperation("", 0, 0, null));
                return;
            }

            string newText = message.Text;

            int currentIndex = message.SelectionStart;
            string searchTerm = null;

            foreach (var item in e.Changes)
            {
                currentIndex = item.Offset + item.AddedLength - 1;

                if (item.AddedLength == 0 && item.RemovedLength > 0)
                {
                    currentIndex = item.Offset - 1;
                }
                if (item.Offset == 0 && currentIndex == -1)
                {
                    // removing the first character, no need to match emojis
                    // also, we want to keep currentIndex at -1 to cancel out the +1 we need
                    // for normal typing
                    break;
                }
                currentIndex = Math.Max(0, currentIndex);

                bool checkedForWholeMatch = false;
                int index = newText.LastIndexOf(':', currentIndex);
                checkEmoji:
                if (index >= 0)
                {
                    string possibleEmoji = searchTerm = newText.Substring(index, currentIndex - index + 1);

                    if (possibleEmoji.EndsWith("\t")
                        && !string.IsNullOrWhiteSpace(ViewDispatcher.LastState.SearchTerm.term)
                        && !ViewDispatcher.LastState.Suggestions.IsEmpty)
                    {
                        ViewDispatcher.Dispatch(new SuggestionSelectedOperation(ViewDispatcher.LastState.Suggestions.First()));
                        return;
                    }

                    if (possibleEmoji == ":" && index > 0 && !checkedForWholeMatch)
                    {
                        index = newText.LastIndexOf(':', index - 1);
                        checkedForWholeMatch = true;
                        goto checkEmoji;
                    }
                    string emoji = MainWindowViewDispatcher.ShortCodeToEmoji(possibleEmoji);
                    if (emoji != possibleEmoji)
                    {
                        searchTerm = null;
                        newText = newText.Substring(0, index) + emoji + newText.Substring(currentIndex + 1);
                        currentIndex = index;
                    }
                }
            }

            // TODO: would like to be able to continue to use tab to move to Done
            //if (newText.Contains('\t'))
            //{
            //    // the user pressed tab, while they were not performing a search ... move to the next button
            //    // and don't make that change
            //    ViewDispatcher.Dispatch(ViewDispatcher.LastState);
            //    submit.Focus();
            //    Keyboard.Focus(submit);
            //    return;
            //}

            ViewDispatcher.Dispatch(new CommitMessageEditOperation(newText, Math.Max(0, currentIndex + 1), currentIndex, searchTerm));
        }

        public void OpenHelp()
        {
            ProcessUtils.SafeOpen("https://github.com/josephdwyer/Craft");
        }

        public void OpenSettings()
        {
            var settingsWindow = new Settings();
            settingsWindow.ShowDialog();
        }

        private void EscapeImpl()
        {
            Close();
        }
    }
}
