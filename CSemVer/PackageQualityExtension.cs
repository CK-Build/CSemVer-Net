using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSemVer;

/// <summary>
/// Provides extension methods to <see cref="PackageQuality"/>.
/// </summary>
public static class PackageQualityExtension
{
    static readonly PackageQuality[][] _map =
    [
        [PackageQuality.CI],
        [PackageQuality.Exploratory, PackageQuality.CI],
        [PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI],
        [PackageQuality.ReleaseCandidate, PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI],
        [PackageQuality.Stable, PackageQuality.ReleaseCandidate, PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI]
    ];

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
    /// Tries to match one of the <see cref="PackageQuality"/> terms (the <paramref name="head"/> must be at the start, no trimming is done).
    /// Note that match is case insensitive and that "rc" is a synonym of <see cref="PackageQuality.ReleaseCandidate"/>.
    /// </summary>
    /// <param name="head">The string to parse.</param>
    /// <param name="q">The read quality. On error, the value is unchanged.</param>
    /// <returns>True on success, false otherwise.</returns>
    public static bool TryMatch( ReadOnlySpan<char> head, ref PackageQuality q ) => TryMatch( ref head, ref q );

    /// <summary>
    /// Tries to match one of the <see cref="PackageQuality"/> terms (the <paramref name="head"/> must be at the start, no trimming is done).
    /// Note that match is case insensitive and that "rc" is a synonym of <see cref="PackageQuality.ReleaseCandidate"/>.
    /// On success, the head is forwarded right after the match: on success, the head may be on any kind of character.
    /// </summary>
    /// <param name="head">The string to parse.</param>
    /// <param name="q">The read quality. On error, the value is unchanged.</param>
    /// <returns>True on success, false otherwise.</returns>
    public static bool TryMatch( ref ReadOnlySpan<char> head, ref PackageQuality q )
    {
        if( head.Length == 0 ) return false;
        if( head.StartsWith( nameof( PackageQuality.CI ).AsSpan(), StringComparison.OrdinalIgnoreCase ) )
        {
            Debug.Assert( nameof( PackageQuality.CI ).Length == 2 );
            q = PackageQuality.CI;
            head = head.Slice( 2 );
            return true;
        }
        if( head.StartsWith( nameof( PackageQuality.Exploratory ).AsSpan(), StringComparison.OrdinalIgnoreCase ) )
        {
            Debug.Assert( nameof( PackageQuality.Exploratory ).Length == 11 );
            head = head.Slice( 11 );
            q = PackageQuality.Exploratory;
            return true;
        }
        if( head.StartsWith( nameof( PackageQuality.Preview ).AsSpan(), StringComparison.OrdinalIgnoreCase ) )
        {
            Debug.Assert( nameof( PackageQuality.Preview ).Length == 7 );
            head = head.Slice( 7 );
            q = PackageQuality.Preview;
            return true;
        }
        if( head.StartsWith( nameof( PackageQuality.ReleaseCandidate ).AsSpan(), StringComparison.OrdinalIgnoreCase ) )
        {
            Debug.Assert( nameof( PackageQuality.ReleaseCandidate ).Length == 16 );
            head = head.Slice( 16 );
            q = PackageQuality.ReleaseCandidate;
            return true;
        }
        if( head.StartsWith( "rc".AsSpan(), StringComparison.OrdinalIgnoreCase ) )
        {
            head = head.Slice( 2 );
            q = PackageQuality.ReleaseCandidate;
            return true;
        }
        if( head.StartsWith( nameof( PackageQuality.Stable ).AsSpan(), StringComparison.OrdinalIgnoreCase ) )
        {
            Debug.Assert( nameof( PackageQuality.Stable ).Length == 6 );
            head = head.Slice( 6 );
            q = PackageQuality.Stable;
            return true;
        }
        return false;
    }

}
