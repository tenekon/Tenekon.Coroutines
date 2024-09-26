﻿using Vernuntii.Collections;
using Vernuntii.Extensions.BranchCases;
using Vernuntii.MessageConventions;
using Vernuntii.MessageConventions.MessageIndicators;
using Vernuntii.VersioningPresets;
using static Vernuntii.Extensions.ServiceCollectionFixture;

namespace Vernuntii.Configuration
{
    public class MessageIndicatorsConfigurationTest
    {
        private const string MessageIndicatorsStringValidFileName = "string-valid.yml";
        private const string MessageIndicatorsStringInvalidFileName = "string-invalid.yml";

        private const string MessageIndicatorsListValidFileName = "list-valid.yml";
        private const string MessageIndicatorsListInvalidFileName = "list-invalid.yml";

        [Fact]
        public void MessageIndicatorObjectShouldThrowDueToMissingName()
        {
            var branchCase = CreateBranchCasesProvider(MessageIndicatorsDirectory, MessageIndicatorsListInvalidFileName).NestedBranchCases["MissingName"];

            var error = Record.Exception(() => branchCase
                .SetVersioningPresetExtensionFactory(DefaultConfiguredVersioningPresetFactory)
                .GetVersioningPresetExtension());

            var configurationValidationError = Assert.IsType<ConfigurationValidationException>(error);
            Assert.Contains("indicator name", error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RegexMessageIndicatorListWithNameAsItemShouldMatch()
        {
            var branchCase = CreateBranchCasesProvider(
                MessageIndicatorsDirectory,
                MessageIndicatorsListValidFileName,
                setVersioningPresetExtension: true).NestedBranchCases["NameAsItem"];

            var expectedVersioningPreset = VersioningPreset.Manual with {
                MessageConvention = MessageConvention.None with {
                    MajorIndicators = new[] {
                        ConventionalCommitsMessageIndicator.Default
                    }
                }
            };

            Assert.Equal(expectedVersioningPreset, branchCase.GetVersioningPresetExtension());
        }

        [Fact]
        public void RegexMessageIndicatorObjectShouldMatch()
        {
            var branchCase = CreateBranchCasesProvider(
                MessageIndicatorsDirectory,
                MessageIndicatorsListValidFileName,
                setVersioningPresetExtension: true).NestedBranchCases["RegexObject"];

            var expectedVersioningPreset = VersioningPreset.Manual with {
                MessageConvention = MessageConvention.None with {
                    MajorIndicators = new[] {
                        RegexMessageIndicator.Empty.With.MajorRegex("test").ToIndicator()
                    }
                }
            };

            Assert.Equal(expectedVersioningPreset, branchCase.GetVersioningPresetExtension());
        }

        [Fact]
        public void RegexMessageIndicatorStringShouldThrow()
        {
            var branchCase = CreateBranchCasesProvider(MessageIndicatorsDirectory, MessageIndicatorsStringInvalidFileName).NestedBranchCases["RegexShouldBeObject"];

            Assert.IsType<ConfigurationValidationException>(Record.Exception(() => branchCase
                .SetVersioningPresetExtensionFactory(DefaultConfiguredVersioningPresetFactory)
                .GetVersioningPresetExtension()));
        }

        [Fact]
        public void NotInbuiltMessageIndicatorStringShouldThrow()
        {
            var branchCase = CreateBranchCasesProvider(MessageIndicatorsDirectory, MessageIndicatorsStringInvalidFileName).NestedBranchCases["StringThatDoesNotExist"];
            Assert.IsType<ItemMissingException>(Record.Exception(() => branchCase
                .SetVersioningPresetExtensionFactory(DefaultConfiguredVersioningPresetFactory)
                .GetVersioningPresetExtension()));
        }

        public static IEnumerable<object[]> ValidMessageIndicatorStringShouldMatchGenerator()
        {
            var branchCases = CreateBranchCasesProvider(MessageIndicatorsDirectory, MessageIndicatorsStringValidFileName, setVersioningPresetExtension: true).NestedBranchCases;

            yield return new object[] {
                 VersioningPreset.Manual with {
                    MessageConvention = MessageConvention.None with {
                        MajorIndicators = new [] { FalsyMessageIndicator.Default }
                    }
                 },
                 branchCases["MajorIndicators"].GetVersioningPresetExtension()
             };

            yield return new object[] {
                 VersioningPreset.Manual with {
                    MessageConvention = MessageConvention.None with {
                        MinorIndicators = new [] { TruthyMessageIndicator.Default }
                    }
                 },
                 branchCases["MinorIndicators"].GetVersioningPresetExtension()
             };

            yield return new object[] {
                 VersioningPreset.Manual with {
                    MessageConvention = MessageConvention.None with {
                        PatchIndicators = new [] { ConventionalCommitsMessageIndicator.Default }
                    }
                 },
                 branchCases["PatchIndicators"].GetVersioningPresetExtension()
             };
        }

        [Theory]
        [MemberData(nameof(ValidMessageIndicatorStringShouldMatchGenerator))]
        public void ValidMessageIndicatorStringShouldMatch(
            IVersioningPreset expectedExtensionOptions,
            IVersioningPreset assumedExtensionOptions) =>
            Assert.Equal(expectedExtensionOptions, assumedExtensionOptions);
    }
}
