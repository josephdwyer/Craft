// <copyright file="CommitMessageEditOperation.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using Curfew;

namespace Craft.ViewDispatchers.MainWindow
{
    public class CommitMessageEditOperation : IImmutableStateObject
    {
        public int SelectionStartIndex { get; }
        public string Message { get; }
        public int SearchIndex { get; }
        public string SearchTerm { get; }


        public CommitMessageEditOperation(string message, int selectionStart, int searchIndex, string searchTerm)
        {
            Message = message;
            SelectionStartIndex = selectionStart;
            SearchIndex = searchIndex;
            SearchTerm = searchTerm;
        }
    }
}
