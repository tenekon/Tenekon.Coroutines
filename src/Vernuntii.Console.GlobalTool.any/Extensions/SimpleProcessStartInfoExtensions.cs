﻿using Kenet.SimpleProcess;
using Microsoft.Extensions.Logging;

namespace Vernuntii.Console.GlobalTool.Extensions
{
    internal static class SimpleProcessStartInfoExtensions
    {
        public static SimpleProcessStartInfo LogDebug(this SimpleProcessStartInfo startInfo, ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug)) {
                string message;

                if (startInfo.Arguments == null) {
                    message = "{DotNetName}";
                } else {
                    message = "{DotNetName} {DotNetArguments}";
                }

#pragma warning disable CA2254 // Template should be a static expression
                logger.LogDebug(message, startInfo.Executable, startInfo.Arguments);
#pragma warning restore CA2254 // Template should be a static expression
            }

            return startInfo;
        }
    }
}
