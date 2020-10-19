// <copyright file="LoadFileOperation.cs">
//     Copyright (c) 2018 Joseph Dwyer All rights reserved.
// </copyright>

using Curfew;

namespace Craft.GlobalOperations
{
    public class LoadFileOperation : IImmutableStateObject
    {
        public string FilePath { get; }

        public LoadFileOperation(string path)
        {
            FilePath = path;
        }
    }
}
