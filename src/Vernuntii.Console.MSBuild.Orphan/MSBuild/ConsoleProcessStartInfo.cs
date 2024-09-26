﻿using System;
using Kenet.SimpleProcess;

namespace Vernuntii.Console.MSBuild
{
    internal record ConsoleProcessStartInfo : SimpleProcessStartInfo
    {
        private static bool IsDynamicLinkLibrary(string fileNameOrPath) =>
            fileNameOrPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

        internal static string GetFileNameOrPath(string fileNameOrPath)
        {
            if (IsDynamicLinkLibrary(fileNameOrPath)) {
                return "dotnet";
            }

            return fileNameOrPath;
        }

        private static string? GetArguments(string fileNameOrPath, string? args)
        {
            if (IsDynamicLinkLibrary(fileNameOrPath)) {
                return $"{fileNameOrPath} {args}";
            }

            return args;
        }

        public ConsoleProcessStartInfo(string fileNameOrPath, string? args = null, string? workingDirectory = null)
            : base(GetFileNameOrPath(fileNameOrPath))
        {
            Arguments = GetArguments(fileNameOrPath, args);
            WorkingDirectory = workingDirectory;
        }
    }
}
