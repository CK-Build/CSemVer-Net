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

        static readonly PackageQuality[][] _map = new PackageQuality[][]
        {
                    Array.Empty<PackageQuality>(),
                    new PackageQuality[]{ PackageQuality.CI },
                    new PackageQuality[]{ PackageQuality.Exploratory, PackageQuality.CI },
                    new PackageQuality[]{ PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI },
                    new PackageQuality[]{ PackageQuality.ReleaseCandidate, PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI },
                    new PackageQuality[]{ PackageQuality.Stable, PackageQuality.ReleaseCandidate, PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI }
        };

        /// <summary>
        /// Merges this quality with another one: the weakest wins, merging <see cref="PackageQuality.CI"/> and <see cref="PackageQuality.Stable"/>
        /// results in <see cref="PackageQuality.CI"/>.
        /// </summary>
        /// <param name="this">This quality.</param>
        /// <param name="other">The other quality.</param>
        /// <returns>The weakest of the two.</returns>
        public static PackageQuality Union( this PackageQuality @this, PackageQuality other )
        {
            return @this < other ? @this : other;
        }

        /// <summary>
        /// Intersects this quality with another one: the strongest wins, merging <see cref="PackageQuality.CI"/> and <see cref="PackageQuality.Stable"/>
        /// results in <see cref="PackageQuality.Stable"/>.
        /// </summary>
        /// <param name="this">This quality.</param>
        /// <param name="other">The other quality.</param>
        /// <returns>The strongest of the two.</returns>
        public static PackageQuality Intersect( this PackageQuality @this, PackageQuality other )
        {
            return @this > other ? @this : other;

        }

        /// <summary>
        /// Gets this quality followed by all its lowest qualities.
        /// </summary>
        /// <param name="this">This quality.</param>
        /// <returns>This quality followed by its lowest ones.</returns>
        public static IReadOnlyList<PackageQuality> GetAllQualities( this PackageQuality @this ) => _map[(int)@this];

        /// <summary>
        /// Tries to parse a text <see cref="PackageQuality"/>. The parsing is case insensitive and supports synonyms, supported forms are:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Quality</term>
        ///         <description>Accepted forms (case insensitive).</description>
        ///     </listheader>
        ///     <item>
        ///         <term><see cref="PackageQuality.CI"/></term>
        ///         <description>CI, All</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="PackageQuality.Exploratory"/></term>
        ///         <description>Exploratory, Exp</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="PackageQuality.Preview"/></term>
        ///         <description>Preview, Pre</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="PackageQuality.ReleaseCandidate"/></term>
        ///         <description>ReleaseCandidate, RC</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="PackageQuality.Stable"/></term>
        ///         <description>Stable, Release, StableRelease</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="q"></param>
        /// <returns>The resulting quality.</returns>
        static public bool TryParse( string s, out PackageQuality q )
        {
            q = default;
            if( s.Length == 0 ) return false;
            if( Enum.TryParse<PackageQuality>( s, true, out q ) ) return true;
            if( s.Equals( "Release", StringComparison.OrdinalIgnoreCase )
                || s.Equals( "StableRelease", StringComparison.OrdinalIgnoreCase ) )
            {
                q = PackageQuality.Stable;
                return true;
            }
            if( s.Equals( "rc", StringComparison.OrdinalIgnoreCase ) )
            {
                q = PackageQuality.ReleaseCandidate;
                return true;
            }
            if( s.Equals( "pre", StringComparison.OrdinalIgnoreCase ) )
            {
                q = PackageQuality.Preview;
                return true;
            }
            if( s.Equals( "exp", StringComparison.OrdinalIgnoreCase ) )
            {
                q = PackageQuality.Exploratory;
                return true;
            }
            if( s.Equals( "all", StringComparison.OrdinalIgnoreCase ) )
            {
                q = PackageQuality.CI;
                return true;
            }
            return false;
        }


    }
}
