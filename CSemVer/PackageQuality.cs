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
    /// the table except that this express the fact that a Release version "superseds" a Preview that itself "superseds" a CI version.
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
        /// Package produced without any explicit version (all <see cref="SVersion"/>
        /// in prerelease that are not <see cref="CSVersion"/> are CI quality by default).
        /// </summary>
        CI = 1,

        /// <summary>
        /// A risky prerelease version ("alpha", "beta", "delta", "epsilon", "gamma", "kappa") that should not be used in production.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "alpha" or "beta" or any of the above
        /// strings (case insensitive).
        /// </summary>
        Exploratory = 2 | CI,

        /// <summary>
        /// A usable prerelease version.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "pre" (case insensitive).
        /// This is typically "preview" or "prerelease".
        /// </summary>
        Preview = 4 | Exploratory,

        /// <summary>
        /// A release candidate version is the last step before <see cref="StableRelease"/>.
        /// This applies to any version with a <see cref="SVersion.Prerelease"/> that starts with "rc" (case insensitive).
        /// </summary>
        ReleaseCandidate = 8 | Preview,

        /// <summary>
        /// A stable, official, release.
        /// This applies to any version with an empty <see cref="SVersion.Prerelease"/>.
        /// </summary>
        StableRelease = 16 | ReleaseCandidate
    }
}
