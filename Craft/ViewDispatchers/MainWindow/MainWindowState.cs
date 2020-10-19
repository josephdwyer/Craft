// <copyright file="MainWindowState.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using Curfew;
using System.Collections.Immutable;

namespace Craft.ViewDispatchers.MainWindow
{
    public class MainWindowState : IImmutableStateObject
    {
        public string Message { get; }
        public bool HasUserEdited { get; }
        public int SelectionStart { get; }
        public bool Focus { get; }
        public bool Close { get; }
        public (int index, string term) SearchTerm { get; }
        public IRxCommand Save { get; }
        public IRxCommand Help { get; }
        public IRxCommand Settings { get; }
        public ImmutableList<SemanticEmoji> Suggestions { get; }


        public MainWindowState()
        {
        }

        public MainWindowState(
            string message = null,
            bool? hasUserEdited = null,
            int? selectionStart = null,
            bool? focus = null,
            bool? close = null,
            IRxCommand save = null,
            IRxCommand help = null,
            IRxCommand settings = null,
            ImmutableList<SemanticEmoji> suggestions = null,
            (int, string)? searchTerm = null
        )
            : this(null, message, hasUserEdited, selectionStart, focus, close, save, help, settings, suggestions, searchTerm)
        {
        }

        public MainWindowState(
            MainWindowState defaults,
            string message = null,
            bool? hasUserEdited = null,
            int? selectionStart = null,
            bool? focus = null,
            bool? close = null,
            IRxCommand save = null,
            IRxCommand help = null,
            IRxCommand settings = null,
            ImmutableList<SemanticEmoji> suggestions = null,
            (int, string)? searchTerm = null)
        {
            Message = message ?? defaults?.Message;
            HasUserEdited = hasUserEdited ?? defaults?.HasUserEdited ?? false;
            SelectionStart = selectionStart ?? defaults?.SelectionStart ?? 0;
            Focus = focus ?? defaults?.Focus ?? false;
            Save = save ?? defaults?.Save;
            Help = help ?? defaults?.Help;
            Settings = settings ?? defaults?.Settings;
            Close = close ?? defaults?.Close ?? false;
            Suggestions = suggestions ?? defaults?.Suggestions;
            SearchTerm = searchTerm ?? defaults?.SearchTerm ?? (0, null);
        }

        public MainWindowState WithMessage(string message) => new MainWindowState(this, message: message);
        public MainWindowState WithHasUserEdited(bool hasUserEdited) => new MainWindowState(this, hasUserEdited: hasUserEdited);
        public MainWindowState WithSelectionStart(int selectionStart) => new MainWindowState(this, selectionStart: selectionStart);
        public MainWindowState WithFocus(bool focus) => new MainWindowState(this, focus: focus);
        public MainWindowState WithClose(bool close) => new MainWindowState(this, close: close);
        public MainWindowState WithSave(IRxCommand save) => new MainWindowState(this, save: save);
        public MainWindowState WithHelp(IRxCommand help) => new MainWindowState(this, help: help);
        public MainWindowState WithSettings(IRxCommand settings) => new MainWindowState(this, settings: settings);
        public MainWindowState WithSuggestions(ImmutableList<SemanticEmoji> suggestions) => new MainWindowState(this, suggestions: suggestions);
        public MainWindowState WithSearchTerm(int index, string searchTerm) => new MainWindowState(this, searchTerm: (index, searchTerm));
    }
}
