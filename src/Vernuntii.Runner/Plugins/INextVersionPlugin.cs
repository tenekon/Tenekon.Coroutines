﻿using Vernuntii.Plugins.CommandLine;
using Vernuntii.PluginSystem;

namespace Vernuntii.Plugins
{
    /// <summary>
    /// Plugin that produces the next version and writes it to console.
    /// </summary>
    public interface INextVersionPlugin : IPlugin
    {
        /// <summary>
        /// The command that calculates the next version on invocation.
        /// </summary>
        ICommandSeat Command { get; }
    }
}
