// <copyright file="MainWindowViewDispatcher.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.IO;
using Curfew;
using System.Collections.Immutable;
using Craft.GlobalOperations;
using CsvHelper;
using System.Globalization;

namespace Craft.ViewDispatchers.MainWindow
{
    public class CsvConfigurationRecord
    {
        public string EmojiName { get; set; }
        public string Description { get; set; }
    }

    public class MainWindowViewDispatcher : ViewDispatcher<MainWindowState>
    {
        private string filePath;
        private ImmutableList<SemanticEmoji> BlessedEmoji { get; }

        public MainWindowViewDispatcher(IMainWindowDependencies viewDependencies) : base()
        {
            BlessedEmoji = ParseEmoji();

            // initialize starting state
            Dispatch(new MainWindowState(
                save: RxCommand.Create(SaveImpl),
                help: RxCommand.Create(viewDependencies.OpenHelp),
                settings: RxCommand.Create(viewDependencies.OpenSettings),
                suggestions: BlessedEmoji
            ));

            RegisterLast<CommitMessageEditOperation>(UpdateCommitMessage);
            RegisterLast<SuggestionSelectedOperation>(UseSuggestion);
            Curfew.Curfew.Dispatcher.RegisterLast<LoadFileOperation>(Load);
        }

        private void UpdateCommitMessage(CommitMessageEditOperation operation)
        {
            string searchTerm = operation.SearchTerm?.Trim(':');
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Dispatch(LastState
                    .WithSuggestions(BlessedEmoji)
                    .WithSearchTerm(0, null)
                    .WithMessage(operation.Message)
                    .WithHasUserEdited(true)
                    .WithSelectionStart(operation.SelectionStartIndex)
                );
                return;
            }
            searchTerm = searchTerm.ToLower().Trim();
            var suggestions = BlessedEmoji
                .Where(x => x.ShortCode.ToLower().Contains(searchTerm) || x.Description.ToLower().Contains(searchTerm))
                .ToImmutableList();

            // TODO: would need to fix the tab auto complete for a situation like `WIN-100: :pill: Fix the thing\t`
            // where we try to auto complete for the `:` after WIN-100 to the end
            //if (suggestions.IsEmpty)
            //{
            //    suggestions = BlessedEmoji;
            //}

            Dispatch(LastState
                .WithSuggestions(suggestions)
                .WithSearchTerm(operation.SearchIndex, operation.SearchTerm)
                .WithMessage(operation.Message)
                .WithHasUserEdited(true)
                .WithSelectionStart(operation.SelectionStartIndex));
        }

        private void UseSuggestion(SuggestionSelectedOperation operation)
        {
            if (!string.IsNullOrWhiteSpace(LastState.SearchTerm.term))
            {
                string newMessage = LastState.Message.Substring(0, LastState.SearchTerm.index + 1 - LastState.SearchTerm.term.Length)
                    + operation.Emoji.Emoji +
                    LastState.Message.Substring(LastState.SearchTerm.index + 1);

                Dispatch(LastState
                    .WithMessage(newMessage)
                    .WithHasUserEdited(true)
                    .WithSelectionStart(LastState.SearchTerm.index + 1 - LastState.SearchTerm.term.Length + operation.Emoji.Emoji.Length)
                    .WithSearchTerm(0, null));
            }
            else
            {
                string newMessage = LastState.Message ?? "";
                if (newMessage.Length > 0 && !newMessage.EndsWith(" "))
                {
                    newMessage += " ";
                }
                newMessage += operation.Emoji.Emoji;

                Dispatch(LastState
                    .WithMessage(newMessage)
                    .WithHasUserEdited(true)
                    .WithSelectionStart(newMessage.Length)
                    .WithSearchTerm(0, null));
            }
        }

        private void Load(LoadFileOperation operation)
        {
            /*
                 filePath: C:/code/plangrid-windows/.git/COMMIT_EDITMSG
                  HEAD => contains branch name!


            rebase
                C:/code/plangrid-windows/.git/rebase-merge/git-rebase-todo
                C:/code/plangrid-windows/.git/COMMIT_EDITMSG
            */
            FileInfo fileInfo = new FileInfo(operation.FilePath);
            if (fileInfo.Exists)
            {
                this.filePath = operation.FilePath;
                string commitText = File.ReadAllText(operation.FilePath);

                string branch = GetBranchName(fileInfo.Directory.FullName);
                string jiraTicket = "";

                if (branch?.Contains("/") ?? false)
                {
                    jiraTicket = branch.Substring(branch.LastIndexOf("/") + 1).ToUpper();
                }

                var lines = commitText.Split('\n');
                bool hasContent = lines.Any(x => !string.IsNullOrEmpty(x) && !x.StartsWith("#"));

                // TODO: what happens with ammend
                if (!hasContent)
                {
                    // if we are starting with a standard commit template (i.e. no user edits)
                    // then we will start the message with a prefix of the jira ticket from the branch name
                    switch (branch)
                    {
                        case "dev":
                        case "test":
                        case "master":
                        case null:
                            break;
                        default:
                            if (!commitText.StartsWith(jiraTicket))
                            {
                                commitText = jiraTicket + ": " + commitText;
                            }
                            break;
                    }
                }

                Dispatch(LastState
                    .WithMessage(commitText)
                    .WithHasUserEdited(false)
                    .WithSelectionStart(Math.Max(0, commitText.IndexOf("\n")))
                    .WithFocus(true));
            }

            // TODO: Rebase UI
            // TODO: Add button to set as git editor
            // TODO: show a row of emoji/help text along the top that is filtered by text from the last ':' -or- default to common picks
        }

        public static string ShortCodeToEmoji(string possibleEmoji)
        {
            switch(possibleEmoji)
            {
                case ":bug:":
                    return EmojiOne.EmojiOne.ShortnameToUnicode(":pill:");
                case ":bug1:":
                    return EmojiOne.EmojiOne.ShortnameToUnicode(":pill:");
                case ":bug2:":
                    return EmojiOne.EmojiOne.ShortnameToUnicode(":syringe:");
                case ":bug3:":
                    return EmojiOne.EmojiOne.ShortnameToUnicode(":ambulance:");
                case ":refactor:":
                    return EmojiOne.EmojiOne.ShortnameToUnicode(":construction_site:");
                case ":sync:":
                    return EmojiOne.EmojiOne.ShortnameToUnicode(":cloud:");
            }
            return EmojiOne.EmojiOne.ShortnameToUnicode(possibleEmoji);
        }

        private string GetBranchName(string gitDirectory)
        {
            string headFilePath = System.IO.Path.Combine(gitDirectory, "HEAD");
            if (File.Exists(headFilePath))
            {
                string headContents = File.ReadAllText(headFilePath);

                if (headContents.StartsWith("ref:"))
                {
                    return headContents.Replace("ref: refs/heads/", "").Trim();
                }
            }
            return null;
        }

        private void SaveImpl()
        {
            if (filePath != null)
            {
                File.WriteAllText(filePath, LastState.Message);
            }
            Dispatch(LastState.WithClose(true));
        }

        private ImmutableList<SemanticEmoji> ParseEmoji()
        {
            // improvement: allow setting for a URL to download from so a team can easily share a definition with a team

            // look for craft configuration file at ~/.craft
            try
            {
                var csvFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".craft"));
                if (csvFile.Exists)
                {
                    using var reader = new StreamReader(csvFile.FullName);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    CsvConfigurationRecord[] records = csv.GetRecords<CsvConfigurationRecord>().ToArray();

                    return records
                        .Select(x => new SemanticEmoji(x.EmojiName, x.Description))
                        .ToImmutableList();
                }
            }
            catch
            {
                // let the user know that something went wrong?
            }

            return ImmutableList<SemanticEmoji>.Empty;
        }
    }
}
