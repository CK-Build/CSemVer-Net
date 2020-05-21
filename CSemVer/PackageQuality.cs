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
    public enum PackageQuality
    {
        /// <summary>
        /// No quality level applies (invalid versions have None quality).
        /// </summary>
        None = 0,

        /// <summary>
        /// Package produced without any explicit version (all <see cref="SVersion"/>
        /// in prerelease that are not <see cref="CSVersion"/> are CI quality).
        /// </summary>
        CI = 1,

        /// <summary>
        /// A risky prerelease version ("alpha", "beta", "delta") that should not be used in production.
        /// Applies only to <see cref="CSVersion"/>.
        /// </summary>
        Exploratory = 2 | CI,

        /// <summary>
        /// A usable prerelease version ("epsilon", "gamma" or "kappa").
        /// Applies only to <see cref="CSVersion"/>.
        /// </summary>
        Preview = 4 | Exploratory,

        /// <summary>
        /// A "-pre" (or -"prerelease") or "-rc" prerelease version.
        /// Applies only to <see cref="CSVersion"/>.
        /// </summary>
        ReleaseCandidate = 8 | Preview,

        /// <summary>
        /// An official release.
        /// Applies to <see cref="SVersion"/> in general.
        /// </summary>
        Release = 16 | ReleaseCandidate
    }
}
