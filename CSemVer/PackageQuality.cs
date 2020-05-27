using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// A package quality is associated to a <see cref="SVersion"/> (quality is functionally dependent on the version number).
    /// <para>
    /// Numerical values of these 5 levels are ordered from the less restrictive (CI) to the most one (Release).
    /// These values can also be used as bitflags (most restrictive values "cover" less restrictive ones). This brings nothing on
    /// the table except that this express the fact that a Release version "superseds" a Preview that itself "superseds" a CI version:
    /// see <see cref="PackageQualityExtension.GetPackageQualities(PackageQuality)"/> extension method that provides the lower qualities.
    /// </para>
    /// </summary>
    [Flags]
    public enum PackageQuality : byte
    {
        /// <summary>
        /// No quality level applies (invalid versions have None quality).
        /// </summary>
        None = 0,

        /// <summary>
        /// Applies to any valid <see cref="SVersion"/> that are NOT <see cref="CSVersion"/> and
        /// has a non empty <see cref="SVersion.Prerelease"/> that don't start with "alpha", "beta",
        /// "delta", "epsilon", "gamma", "kappa", "pre" or "rc".
        /// </summary>
        CI = 1,

        /// <summary>
        /// A risky prerelease version ("alpha", "beta", "delta", "epsilon", "gamma", "kappa") that should not be used in production.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with any of the above
        /// strings (case insensitive) or a <see cref="CSVersion"/> that has the corresponding <see cref="CSVersion.PrereleaseName"/>.
        /// </summary>
        Exploratory = 2 | CI,

        /// <summary>
        /// A usable prerelease version.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "pre" (case insensitive, this typically
        /// handles "preview" or "prerelease"), or a <see cref="CSVersion"/> with a <see cref="CSVersion.PrereleaseName"/> of "preview"
        /// (<see cref="CSVersion.PrereleaseNameIdx"/> = 6).
        /// </summary>
        Preview = 4 | Exploratory,

        /// <summary>
        /// A release candidate version is the last step before <see cref="Stable"/>.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "rc" (case insensitive)
        /// or a <see cref="CSVersion"/> with a <see cref="CSVersion.PrereleaseName"/> of "rc" (<see cref="CSVersion.PrereleaseNameIdx"/> = 7).
        /// </summary>
        ReleaseCandidate = 8 | Preview,

        /// <summary>
        /// A stable, official, release.
        /// This applies to any version with an empty <see cref="SVersion.Prerelease"/>.
        /// </summary>
        Stable = 16 | ReleaseCandidate
    }
}
