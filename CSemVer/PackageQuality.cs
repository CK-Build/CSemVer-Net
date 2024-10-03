using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// A package quality is associated to a <see cref="SVersion"/> (quality is functionally dependent on the prerelease).
    /// <para>
    /// Numerical values of these 5 levels are ordered from the less restrictive (CI) to the most one (Release).
    /// </para>
    /// </summary>
    public enum PackageQuality : byte
    {
        /// <summary>
        /// Applies to any <see cref="SVersion"/> that are NOT <see cref="CSVersion"/> and
        /// has a non empty <see cref="SVersion.Prerelease"/> that don't start with "alpha", "beta",
        /// "delta", "epsilon", "gamma", "kappa", "pre" or "rc".
        /// </summary>
        CI = 0,

        /// <summary>
        /// A risky prerelease version ("alpha", "beta", "delta", "epsilon", "gamma", "kappa") that should not be used in production.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with any of the above
        /// strings (case insensitive) or a <see cref="CSVersion"/> that has the corresponding <see cref="CSVersion.PrereleaseName"/>.
        /// </summary>
        Exploratory = 1,

        /// <summary>
        /// A usable prerelease version.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "pre" (case insensitive, this typically
        /// handles "preview" or "prerelease"), or a <see cref="CSVersion"/> with a <see cref="CSVersion.PrereleaseName"/> of "preview"
        /// (<see cref="CSVersion.PrereleaseNameIdx"/> = 6).
        /// </summary>
        Preview = 2,

        /// <summary>
        /// A release candidate version is the last step before <see cref="Stable"/>.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "rc" (case insensitive)
        /// or a <see cref="CSVersion"/> with a <see cref="CSVersion.PrereleaseName"/> of "rc" (<see cref="CSVersion.PrereleaseNameIdx"/> = 7).
        /// </summary>
        ReleaseCandidate = 3,

        /// <summary>
        /// A stable, official, release.
        /// This applies to any version with an empty <see cref="SVersion.Prerelease"/>.
        /// </summary>
        Stable = 4
    }
}
