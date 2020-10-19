// <copyright file="SemanticEmoji.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>


namespace Craft.ViewDispatchers.MainWindow
{
    public class SemanticEmoji
    {
        public string Emoji { get; }
        public string ShortCode { get; }
        public string Description { get; }

        public string DisplayString => $"{Emoji} {Description}";

        public SemanticEmoji(string shortCode, string description)
        {
            Emoji = MainWindowViewDispatcher.ShortCodeToEmoji(shortCode);
            ShortCode = shortCode;
            Description = description;
        }
    }
}
