﻿using System.Diagnostics.CodeAnalysis;

namespace Vernuntii.SemVer.Parser
{
    /// <summary>
    /// A parser to parse a SemVer-compatible version.
    /// </summary>
    public interface ISemanticVersionParser
    {
        /// <summary>
        /// Tries to parse the value.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="prefix">The prefix part of the version. Cannot be empty.</param>
        /// <param name="major">The major part of the version.</param>
        /// <param name="minor">The minor part of the version.</param>
        /// <param name="patch">The patch part of the version.</param>
        /// <param name="preReleaseIdentifiers">The pre-release identifiers. Cannot be empty.</param>
        /// <param name="buildIdentifiers">The build identifiers. Cannot be empty.</param>
        /// <returns>True if successful.</returns>
        bool TryParse(
            string? value,
            out string? prefix,
            [NotNullWhen(true)] out uint? major,
            [NotNullWhen(true)] out uint? minor,
            [NotNullWhen(true)] out uint? patch,
            out IEnumerable<string>? preReleaseIdentifiers,
            out IEnumerable<string>? buildIdentifiers);
    }
}
