using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// Provides extension methods to <see cref="PackageQuality"/>.
    /// </summary>
    public static class PackageQualityExtension
    {
        /// <summary>
        /// Merges this quality with another one: the weakest wins, merging <see cref="PackageQuality.CI"/> and <see cref="PackageQuality.Release"/>
        /// results in <see cref="PackageQuality.CI"/>.
        /// </summary>
        /// <param name="this">This quality.</param>
        /// <param name="other">The other quality.</param>
        /// <returns>The weakest of the two.</returns>
        public static PackageQuality Union( this PackageQuality @this, PackageQuality other )
        {
            return @this < other ? @this : other;

        }

    }
}
