﻿using Vernuntii.PluginSystem.Reactive;

namespace Vernuntii.Plugins.Events
{
    /// <summary>
    /// Events for <see cref="ILoggingPlugin"/>.
    /// </summary>
    public static class LoggingEvents
    {
        /// <summary>
        /// Event when logging infrastructure is getting enabled.
        /// </summary>
        public static readonly EventDiscriminator EnableLoggingInfrastructure = EventDiscriminator.New();

        /// <summary>
        /// Event when logging infrastructure is enabled.
        /// </summary>
        public static readonly EventDiscriminator<ILoggingPlugin> OnEnabledLoggingInfrastructure = EventDiscriminator.New<ILoggingPlugin>();
    }
}
