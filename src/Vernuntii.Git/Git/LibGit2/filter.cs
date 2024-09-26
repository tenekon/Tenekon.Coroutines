using System.Runtime.InteropServices;

namespace Vernuntii.Git.LibGit2
{
    /// <summary>
    /// An object (blob, commit, tree, etc) in a Git repository.
    /// </summary>
    public struct git_filter_list { };

    /// <summary>
    /// Flags to control the functionality of `git_blob_filter`.
    /// </summary>
    public enum git_filter_mode_t
    {
        GIT_FILTER_TO_WORKTREE = 0,
        GIT_FILTER_TO_ODB = 1,

        GIT_FILTER_SMUDGE = GIT_FILTER_TO_WORKTREE,
        GIT_FILTER_CLEAN = GIT_FILTER_TO_ODB,
    }

    /// <summary>
    /// Filtering options.
    /// </summary>
    public enum git_filter_flags_t
    {
        GIT_FILTER_DEFAULT = 0,

        /// <summary>
        /// Don't error for `safecrlf` violations, allow them to continue.
        /// </summary>
        GIT_FILTER_ALLOW_UNSAFE = (1 << 0),

        /// <summary>
        /// When set, filters will no load configuration from the
        /// system-wide `gitattributes` in `/etc` (or system equivalent).
        /// </summary>
        GIT_FILTER_NO_SYSTEM_ATTRIBUTES = (1 << 1),

        /// <summary>
        /// When set, filters will be loaded from a `.gitattributes` file
        /// in the HEAD commit.
        /// </summary>
        GIT_FILTER_ATTRIBUTES_FROM_HEAD = (1 << 2),

        /// <summary>
        /// When set, filters will be loaded from a `.gitattributes` file
        /// in a given commit.  This can only be specified in a
        /// `git_filter_options`.
        /// </summary>
        GIT_FILTER_ATTRIBUTES_FROM_COMMIT = (1 << 3),
    }

    /// <summary>
    /// Filtering options.  Initialize with `GIT_FILTER_OPTIONS_INIT`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct git_filter_options
    {
        /// <summary>
        /// Version of the options.
        /// </summary>
        public int version;

        /// <summary>
        /// Flags to control the filtering process, see
        /// `git_blob_filter_flag_t` above.
        /// </summary>
        public git_filter_flags_t flags;

        public IntPtr reserved;

        /// <summary>
        /// The commit to load attributes from, when
        /// `GIT_FILTER_ATTRIBUTES_FROM_COMMIT` is specified.
        /// </summary>
        public git_oid attr_commit_id;

        /// <summary>
        /// Current version of the options structure.
        /// </summary>
        public static int GIT_FILTER_OPTIONS_VERSION {
            get {
                return 1;
            }
        }

        /// <summary>
        /// The default values for our options structure.
        /// </summary>
        public static git_filter_options GIT_FILTER_OPTIONS_INIT {
            get {
                return new git_filter_options() {
                    version = GIT_FILTER_OPTIONS_VERSION,
                    flags = git_filter_flags_t.GIT_FILTER_DEFAULT
                };
            }
        }
    }
}
