﻿using System.Diagnostics.CodeAnalysis;
using Vernuntii.SemVer.Parser.Extensions;
using Vernuntii.SemVer.Parser.Normalization;
using static Vernuntii.SemVer.Parser.SemanticVersionCharacters;

namespace Vernuntii.SemVer.Parser.Parsers
{
    internal class IdentifierParser
    {
        public delegate bool TryParseNonEmptyIdentifier<T>(SemanticVersionPart versionPart, string dottedIdentifier, [NotNullWhen(true)] out T? result);
        public delegate IReadOnlyList<SemanticVersionFault> SearchFaultsDelegate(ReadOnlyMemory<char> spar);

        public static readonly IdentifierParser Strict = new(SemanticVersionNormalizer.NoAction);
        public static readonly IdentifierParser Erase = new(SemanticVersionNormalizer.Erase);

        public static IdentifierParseResult<T> TryParseIdentifier<T>(
            SemanticVersionPart versionPart,
            string? identifier,
            TryParseNonEmptyIdentifier<T> tryParse,
            bool allowNull)
        {
            if (identifier == null) {
                if (allowNull) {
                    return IdentifierParseResult<T>.ValidNull;
                } else {
                    return IdentifierParseResult<T>.InvalidNull;
                }
            } else if (identifier.Length == 0) {
                return IdentifierParseResult<T>.InvalidEmpty;
            } else if (string.IsNullOrWhiteSpace(identifier)) {
                return IdentifierParseResult<T>.InvalidWhiteSpace;
            } else if (!tryParse(versionPart, identifier, out var preReleaseIdentifiers)) {
                return IdentifierParseResult.InvalidParse(preReleaseIdentifiers);
            } else {
                return IdentifierParseResult.ValidParse(preReleaseIdentifiers);
            }
        }

        private static bool IsContainedInAlphanumericIdentifierCharset(char character) =>
            character == Hyphen
            || (character >= UpperA && character <= UpperZ)
            || (character >= LowerA && character <= LowerZ)
            || char.IsDigit(character);

        public static IReadOnlyList<SemanticVersionFault> SearchFaults(
            ReadOnlySpan<char> value,
            bool lookupBackslashZero = false,
            bool lookupSingleZero = false,
            bool lookupAlphanumeric = false,
            bool lookupNumeric = false)
        {
            var valueLength = value.Length;
            var faults = new List<SemanticVersionFault>();

            if (lookupSingleZero && value.HasNumberLeadingZeros(out var zeros)) {
                IdentifierExpectation expectation;

                if (zeros == valueLength) {
                    expectation = IdentifierExpectation.SingleZero;
                } else {
                    expectation = IdentifierExpectation.Empty;
                }

                faults.Add(new SemanticVersionFault(expectation, 0..zeros));
            } else {
                for (var i = 0; i < valueLength; i++) {
                    var currentCharacter = value[i];

                    @continue:
                    ;

                    if (lookupBackslashZero && currentCharacter == '\0') {
                        var faultStartAt = i;

                        for (i++; i < valueLength; i++) {
                            currentCharacter = value[i];

                            if (currentCharacter != '\0') {
                                faults.Add(new SemanticVersionFault(IdentifierExpectation.Empty, faultStartAt..(i - 1)));
                                goto @continue;
                            }
                        }

                        faults.Add(new SemanticVersionFault(IdentifierExpectation.Empty, faultStartAt..valueLength));
                    }

                    if (lookupAlphanumeric && !IsContainedInAlphanumericIdentifierCharset(currentCharacter)) {
                        var faultStartAt = i;

                        for (i++; i < valueLength; i++) {
                            currentCharacter = value[i];

                            if (IsContainedInAlphanumericIdentifierCharset(currentCharacter)) {
                                faults.Add(new SemanticVersionFault(IdentifierExpectation.Alphanumeric, faultStartAt..(i - 1)));
                                goto @continue;
                            }
                        }

                        faults.Add(new SemanticVersionFault(IdentifierExpectation.Alphanumeric, faultStartAt..valueLength));
                    }

                    if (lookupNumeric && !char.IsDigit(currentCharacter)) {
                        var faultStartAt = i;

                        for (i++; i < valueLength; i++) {
                            currentCharacter = value[i];

                            if (char.IsDigit(currentCharacter)) {
                                faults.Add(new SemanticVersionFault(IdentifierExpectation.Numeric, faultStartAt..(i - 1)));
                                goto @continue;
                            }
                        }

                        faults.Add(new SemanticVersionFault(IdentifierExpectation.Numeric, faultStartAt..valueLength));
                    }
                }
            }

            return faults;
        }

        public ISemanticVersionNormalizer Normalizer { get; }

        public IdentifierParser(ISemanticVersionNormalizer normalizer) => Normalizer = normalizer;

        public bool TryResolveFaults(SemanticVersionPart versionPart, ReadOnlyMemory<char> valueMemory, SearchFaultsDelegate searchFaults, out ReadOnlyMemory<char> result)
        {
            recheck:
            var faults = searchFaults(valueMemory);

            if (faults.Count != 0) {
                var normalizedMemory = Normalizer.NormalizeFaults(versionPart, valueMemory, faults);

                if (valueMemory.Span.Equals(normalizedMemory.Span, StringComparison.Ordinal)) {
                    goto exit;
                }

                valueMemory = normalizedMemory;
                goto recheck;
            }

            result = valueMemory;
            return true;

            exit:
            result = null;
            return false;
        }

        public bool TryParseDottedIdentifier(
            SemanticVersionPart versionPart,
            string dottedIdentifier,
            [NotNullWhen(true)] out IEnumerable<string>? dotSplittedIdentifiers,
            bool lookupSingleZero = false)
        {
            var dotSplittedIdentifierArray = dottedIdentifier.Split(Dot);
            var dotSplittedIdentifierArrayLength = dotSplittedIdentifierArray.Length;
            var emptyIdentifiers = 0;

            for (var i = 0; i < dotSplittedIdentifierArrayLength; i++) {
                var unresolvedMemory = dotSplittedIdentifierArray[i].AsMemory();

                var success = TryResolveFaults(
                    versionPart,
                    unresolvedMemory,
                    value => SearchFaults(
                        value.Span,
                        lookupBackslashZero: true,
                        lookupSingleZero: lookupSingleZero,
                        lookupAlphanumeric: true),
                    out var resolvedSpan);

                if (!success) {
                    goto exit;
                }

                if (unresolvedMemory.Span != resolvedSpan.Span) {
                    dotSplittedIdentifierArray[i] = resolvedSpan.ToString();
                }

                if (dotSplittedIdentifierArray[i].Length == 0) {
                    emptyIdentifiers++;
                }
            }

            if (emptyIdentifiers == dotSplittedIdentifierArray.Length
                || (!Normalizer.TrimPreReleaseDots && emptyIdentifiers > 0)) {
                goto exit;
            }

            dotSplittedIdentifiers = new IdentifierEnumerable(dotSplittedIdentifierArray);
            return true;

            exit:
            dotSplittedIdentifiers = null;
            return false;
        }
    }
}
