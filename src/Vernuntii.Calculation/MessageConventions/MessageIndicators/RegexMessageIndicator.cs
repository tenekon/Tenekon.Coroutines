﻿using System.Text.RegularExpressions;

namespace Vernuntii.MessageConventions.MessageIndicators
{
    /// <summary>
    /// Message indicator depending on RegEx patterns.
    /// </summary>
    public record class RegexMessageIndicator : RegexMessageIndicatorBase
    {
        /// <summary>
        /// The default instance.
        /// </summary>
        public static readonly RegexMessageIndicator Empty = new();

        /// <inheritdoc/>
        public override string IndicatorName => "Regex";

        /// <inheritdoc/>
        public override Regex? MajorRegex { get; init; }
        /// <inheritdoc/>
        public override Regex? MinorRegex { get; init; }
        /// <inheritdoc/>
        public override Regex? PatchRegex { get; init; }
    }
}
