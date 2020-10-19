// <copyright file="SuggestionSelectedOperation.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using Curfew;

namespace Craft.ViewDispatchers.MainWindow
{
    public class SuggestionSelectedOperation : IImmutableStateObject
    {
        public SemanticEmoji Emoji { get; }

        public SuggestionSelectedOperation(SemanticEmoji emoji)
        {
            Emoji = emoji;
        }
    }
}
