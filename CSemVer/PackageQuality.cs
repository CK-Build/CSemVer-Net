using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// A package quality is associated to a <see cref="SVersion"/>.
    /// The quality is functionally dependent on the version number whereas <see cref="PackageLabel"/>
    /// denotes expected version ranges.
    /// <para>
    /// Among these 5 levels, versions that are not <see cref="CSVersion"/> are either in lowest (CI)
    /// or highest (Release) quality: these are all the versions with -prerelease tag and normal,
    /// stable versions. 
    /// </para>
    /// </summary>
    public enum PackageQuality
    {
        /// <summary>
        /// No quality level applies (invalid versions have None quality).
        /// </summary>
        None,

        /// <summary>
        /// Package produced without any explicit version (all <see cref="SVersion"/>
        /// in prerelease that are not <see cref="CSVersion"/> are CI quality).
        /// </summary>
        CI,

        /// <summary>
        /// A risky prerelease version ("alpha", "beta", "delta") that should not be used in production.
        /// Applies only to <see cref="CSVersion"/>.
        /// </summary>
        Exploratory,

        /// <summary>
        /// A usable prerelease version ("epsilon", "gamma" or "kappa").
        /// Applies only to <see cref="CSVersion"/>.
        /// </summary>
        Preview,

        /// <summary>
        /// A "-pre" (or -"prerelease") or "-rc" prerelease version.
        /// Applies only to <see cref="CSVersion"/>.
        /// </summary>
        ReleaseCandidate,

        /// <summary>
        /// An official release.
        /// Applies to <see cref="SVersion"/> in general.
        /// </summary>
        Release
    }
}
