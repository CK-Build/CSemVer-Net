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
        static readonly PackageLabel[][] _map = new PackageLabel[][]
            {
                Array.Empty<PackageLabel>(),
                new PackageLabel[]{ PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Exploratory, PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Preview, PackageLabel.Exploratory, PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Latest, PackageLabel.Preview, PackageLabel.Exploratory, PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Stable, PackageLabel.Latest, PackageLabel.Preview, PackageLabel.Exploratory, PackageLabel.CI }
            };

        /// <summary>
        /// Gets the standard package labels that corresponds to this <see cref="PackageQuality"/>.
        /// </summary>
        /// <param name="this">This PackageQuality.</param>
        /// <returns>The corresponding labels.</returns>
        public static IReadOnlyList<PackageLabel> GetLabels( this PackageQuality @this )
        {
            return _map[(int)@this];
        }

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
