using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// A label tags a package based on its <see cref="PackageQuality"/>.
    /// </summary>
    public enum PackageLabel
    {
        /// <summary>
        /// Stable package are <see cref="PackageQuality.Release"/>.
        /// </summary>
        Stable,

        /// <summary>
        /// Latest packages should be the default while developping
        /// without too much risks: <see cref="PackageQuality.Release"/> and <see cref="PackageQuality.ReleaseCandidate"/>.
        /// </summary>
        Latest,

        /// <summary>
        /// Preview packages are <see cref="Latest"/> but also <see cref="PackageQuality.Preview"/> packages.
        /// </summary>
        Preview,

        /// <summary>
        /// CI packages are all the packages except <see cref="PackageQuality.None"/>.
        /// </summary>
        CI
    }
}
