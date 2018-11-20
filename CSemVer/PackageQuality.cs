using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// A package quality is associated to a <see cref="SVersion"/>.
    /// </summary>
    public enum PackageQuality
    {
        /// <summary>
        /// No quality level applies.
        /// </summary>
        None,

        /// <summary>
        /// Package produced without any explicit version.
        /// </summary>
        CI,

        /// <summary>
        /// A prerelease version ("alpha", "beta", "delta", "epsilon", "gamma" or "kappa").
        /// </summary>
        Preview,

        /// <summary>
        /// A "-pre" (or -"prerelease") or "-rc" prerelease version.
        /// </summary>
        ReleaseCandidate,

        /// <summary>
        /// An official release.
        /// </summary>
        Release
    }
}
