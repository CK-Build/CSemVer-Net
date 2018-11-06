using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    public static class PackageQualityExtension
    {
        static readonly PackageLabel[][] _map = new PackageLabel[][]
            {
                Array.Empty<PackageLabel>(),
                new PackageLabel[]{ PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Preview, PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Latest, PackageLabel.Preview, PackageLabel.CI },
                new PackageLabel[]{ PackageLabel.Stable, PackageLabel.Latest, PackageLabel.Preview, PackageLabel.CI }
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
    }
}
