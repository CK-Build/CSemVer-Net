using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CSemVer;

public sealed partial class CSVersion
{
    [StructLayout( LayoutKind.Explicit )]
    struct SOrderedVersion
    {
        [FieldOffset( 0 )]
        public long Number;
        [FieldOffset( 6 )]
        public UInt16 Major;
        [FieldOffset( 4 )]
        public UInt16 Minor;
        [FieldOffset( 2 )]
        public UInt16 Build;
        [FieldOffset( 0 )]
        public UInt16 Revision;
    }

    readonly SOrderedVersion _orderedVersion;

    /// <summary>
    /// The maximum number of major versions.
    /// </summary>
    public const int MaxMajor = 99999;
    /// <summary>
    /// The maximum number of minor versions for a major one.
    /// </summary>
    public const int MaxMinor = 49999;
    /// <summary>
    /// The maximum number of patches for a minor one.
    /// </summary>
    public const int MaxPatch = 9999;
    /// <summary>
    /// The maximum number of prereleases is also the index of the "rc" entry in <see cref="StandardPrereleaseNames"/>.
    /// </summary>
    public const int MaxPreReleaseNameIdx = 7;
    /// <summary>
    /// The maximum number of prereleases.
    /// </summary>
    public const int MaxPreReleaseNumber = 99;
    /// <summary>
    /// The maximum number of fixes to a prerelease.
    /// </summary>
    public const int MaxPreReleasePatch = 99;

    const long MaxOrderedVersion = (MaxMajor + 1L)
                                   * (MaxMinor + 1L)
                                   * (MaxPatch + 1L)
                                   * (1L + (MaxPreReleaseNameIdx + 1L)
                                   * (MaxPreReleaseNumber + 1L)
                                   * (MaxPreReleasePatch + 1L));

    static readonly string[] _standardNames = new[] { "alpha", "beta", "delta", "epsilon", "gamma", "kappa", "preview", "rc" };
    static readonly string[] _standardNamesI = new[] { "a", "b", "d", "e", "g", "k", "p", "r" };
    static readonly char[] _standardNamesC = new[] { 'a', 'b', 'd', 'e', 'g', 'k', 'p', 'r' };

    const long MulNum = MaxPreReleasePatch + 1;
    const long MulName = MulNum * (MaxPreReleaseNumber + 1);
    const long MulPatch = MulName * (MaxPreReleaseNameIdx + 1) + 1;
    const long MulMinor = MulPatch * (MaxPatch + 1);
    const long MulMajor = MulMinor * (MaxMinor + 1);

    const long DivPatch = MulPatch + 1;
    const long DivMinor = DivPatch * (MaxPatch);
    const long DivMajor = DivMinor * (MaxMinor + 1);

    /// <summary>
    /// Gets the standard <see cref="PrereleaseName"/>.
    /// </summary>
    public static IReadOnlyList<string> StandardPrereleaseNames => _standardNames;

    /// <summary>
    /// Gets the short form <see cref="PrereleaseName"/> (the initials).
    /// </summary>
    public static IReadOnlyList<string> StandardPreReleaseNamesShort => _standardNamesI;

    /// <summary>
    /// Gets the very first possible version (0.0.0-a).
    /// </summary>
    public static readonly CSVersion VeryFirstVersion = new CSVersion( 0, 0, 0, String.Empty, 0, 0, 0, false, 1 );

    /// <summary>
    /// Gets the very first possible release versions (0.0.0, 0.1.0 or 1.0.0 or any prereleases of them).
    /// </summary>
    public static readonly IReadOnlyList<CSVersion> FirstPossibleVersions = BuildFirstPossibleVersions();

    /// <summary>
    /// Gets the very last possible version (99999.49999.9999).
    /// </summary>
    public static readonly CSVersion VeryLastVersion = new CSVersion( MaxMajor, MaxMinor, MaxPatch, String.Empty, -1, 0, 0, false, MaxOrderedVersion );


    static IReadOnlyList<CSVersion> BuildFirstPossibleVersions()
    {
        var versions = new CSVersion[3 * 9];
        long v = 1L;
        int i = 0;
        while( i < 3 * 9 )
        {
            versions[i++] = Create( v );
            if( (i % 18) == 0 ) v += MulMajor - MulMinor - MulPatch + 1;
            else if( (i % 9) == 0 ) v += MulMinor - MulPatch + 1;
            else v += MulName;
        }
        return versions;
    }

    /// <summary>
    /// Creates a new version from an ordered version that must be between 0 (invalid version) and <see cref="VeryLastVersion"/>.<see cref="OrderedVersion"/>.
    /// <para>
    /// This can be used to fully restore a valid instance (if <paramref name="v"/> is 0, a new "Invalid CSVersion." is returned).
    /// </para>
    /// </summary>
    /// <param name="v">The ordered version.</param>
    /// <param name="longForm">True to create a <see cref="IsLongForm"/> version.</param>
    /// <param name="buildMetaData">Optional <see cref="SVersion.BuildMetaData"/>. Must not start with '+'.</param>
    /// <param name="parsedText">Optional original parsed text.</param>
    /// <returns>The version.</returns>
    public static CSVersion Create( long v, bool longForm = false, string? buildMetaData = null, string? parsedText = null )
    {
        if( v < 0 || v > MaxOrderedVersion ) throw new ArgumentException( "Must be between 0 and VeryLastVersion.OrderedVersion." );
        if( v == 0 ) return new CSVersion( "Invalid CSVersion.", parsedText );

        long dV = v;
        int prNameIdx;
        int prNumber;
        int prPatch;
        long preReleasePart = dV % MulPatch;
        if( preReleasePart != 0 )
        {
            preReleasePart -= 1L;
            prNameIdx = (int)(preReleasePart / MulName);
            preReleasePart -= (long)prNameIdx * MulName;
            prNumber = (int)(preReleasePart / MulNum);
            preReleasePart -= (long)prNumber * MulNum;
            prPatch = (int)preReleasePart;
        }
        else
        {
            dV -= MulPatch;
            prNameIdx = -1;
            prNumber = 0;
            prPatch = 0;
        }
        int major = (int)(dV / MulMajor);
        dV -= major * MulMajor;
        int minor = (int)(dV / MulMinor);
        dV -= minor * MulMinor;
        int patch = (int)(dV / MulPatch);

        return new CSVersion( major, minor, patch, buildMetaData ?? String.Empty, prNameIdx, prNumber, prPatch, longForm, v, parsedText );
    }

    static long ComputeOrderedVersion( int major, int minor, int patch, int preReleaseNameIdx = -1, int preReleaseNumber = 0, int preReleaseFix = 0 )
    {
        Debug.Assert( preReleaseNameIdx >= 0 || (preReleaseNumber == 0 && preReleaseFix == 0), "preReleaseNameIdx = -1 ==> preReleaseNumber = preReleaseFix = 0" );
        long v = MulMajor * major;
        v += MulMinor * minor;
        v += MulPatch * (patch + 1);
        if( preReleaseNameIdx >= 0 )
        {
            v -= MulPatch - 1;
            v += MulName * preReleaseNameIdx;
            v += MulNum * preReleaseNumber;
            v += preReleaseFix;
        }
        Debug.Assert( Create( v )._orderedVersion.Number == v );
        Debug.Assert( (preReleaseNameIdx >= 0) == ((v % MulPatch) != 0) );
        Debug.Assert( major == (int)((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulMajor) );
        Debug.Assert( minor == (int)(((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulMinor) - major * (MaxMinor + 1L)) );
        Debug.Assert( patch == (int)(((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulPatch) - (major * (MaxMinor + 1L) + minor) * (MaxPatch + 1L)) );
        Debug.Assert( preReleaseNameIdx == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) / MulName) : -1) );
        Debug.Assert( preReleaseNumber == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) % MulName) / MulNum : 0) );
        Debug.Assert( preReleaseFix == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) % MulNum) : 0) );
        return v;
    }

    /// <summary>
    /// Gets the ordered version number. 0 is invalid, 1 is the <see cref="VeryFirstVersion"/>,
    /// 4000050000000000000 is the greatest value (<see cref="VeryLastVersion"/>).
    /// </summary>
    public long OrderedVersion => _orderedVersion.Number;

    /// <summary>
    /// Gets the Major (first, most significant) part of the <see cref="OrderedVersion"/>: between 0 and 32767.
    /// </summary>
    public int OrderedVersionMajor => _orderedVersion.Major;

    /// <summary>
    /// Gets the Minor (second) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
    /// </summary>
    public int OrderedVersionMinor => _orderedVersion.Minor;

    /// <summary>
    /// Gets the Build (third) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
    /// </summary>
    public int OrderedVersionBuild => _orderedVersion.Build;

    /// <summary>
    /// Gets the Revision (last, less significant) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
    /// </summary>
    public int OrderedVersionRevision => _orderedVersion.Revision;

    /// <summary>
    /// Versions are equal if their <see cref="OrderedVersion"/> are equals.
    /// No other members are used for equality and comparison.
    /// </summary>
    /// <param name="other">Other version.</param>
    /// <returns>True if they have the same OrderedVersion.</returns>
    public bool Equals( CSVersion? other )
    {
        if( other == null ) return false;
        return _orderedVersion.Number == other._orderedVersion.Number;
    }

    /// <summary>
    /// Relies only on <see cref="OrderedVersion"/>.
    /// </summary>
    /// <param name="other">Other release tag (can be null).</param>
    /// <returns>A signed number indicating the relative values of this instance and <paramref name="other"/>.</returns>
    public int CompareTo( CSVersion? other )
    {
        if( other == null ) return 1;
        return _orderedVersion.Number.CompareTo( other._orderedVersion.Number );
    }

    /// <summary>
    /// Implements == operator.
    /// </summary>
    /// <param name="x">First version.</param>
    /// <param name="y">Second version.</param>
    /// <returns>True if they are equal.</returns>
    static public bool operator ==( CSVersion? x, CSVersion? y )
    {
        if( ReferenceEquals( x, y ) ) return true;
        if( x is object && y is object )
        {
            return x._orderedVersion.Number == y._orderedVersion.Number;
        }
        return false;
    }

    /// <summary>
    /// Implements &gt; operator.
    /// </summary>
    /// <param name="x">First version.</param>
    /// <param name="y">Second version.</param>
    /// <returns>True if x is greater than y.</returns>
    static public bool operator >( CSVersion? x, CSVersion? y )
    {
        if( ReferenceEquals( x, y ) ) return false;
        if( x is object )
        {
            if( y is null ) return true;
            return x._orderedVersion.Number > y._orderedVersion.Number;
        }
        return false;
    }

    /// <summary>
    /// Implements &gt;= operator.
    /// </summary>
    /// <param name="x">First version.</param>
    /// <param name="y">Second version.</param>
    /// <returns>True if x is greater than or equal to y.</returns>
    static public bool operator >=( CSVersion? x, CSVersion? y )
    {
        if( ReferenceEquals( x, y ) ) return true;
        if( x is object )
        {
            if( y is null ) return true;
            return x._orderedVersion.Number >= y._orderedVersion.Number;
        }
        return false;
    }

    /// <summary>
    /// Implements != operator.
    /// </summary>
    /// <param name="x">First version.</param>
    /// <param name="y">Second version.</param>
    /// <returns>True if they are not equal.</returns>
    static public bool operator !=( CSVersion? x, CSVersion? y ) => !(x == y);

    /// <summary>
    /// Implements &lt;= operator.
    /// </summary>
    /// <param name="x">First version.</param>
    /// <param name="y">Second version.</param>
    /// <returns>True if x is lower than or equal to y.</returns>
    static public bool operator <=( CSVersion? x, CSVersion? y ) => !(x > y);

    /// <summary>
    /// Implements &lt; operator.
    /// </summary>
    /// <param name="x">First version.</param>
    /// <param name="y">Second version.</param>
    /// <returns>True if x is lower than y.</returns>
    static public bool operator <( CSVersion? x, CSVersion? y ) => !(x >= y);

    /// <summary>
    /// Version are equal it their <see cref="OrderedVersion"/> are equals.
    /// No other members are used for equality and comparison.
    /// </summary>
    /// <param name="obj">Other release version.</param>
    /// <returns>True if obj is a version that has the same OrderedVersion as this.</returns>
    public override bool Equals( object? obj ) => Equals( obj as CSVersion );

    /// <summary>
    /// Versions are equal it their <see cref="OrderedVersion"/> are equals.
    /// No other members are used for equality and comparison.
    /// </summary>
    /// <returns>True if they have the same OrderedVersion.</returns>
    public override int GetHashCode() => _orderedVersion.Number.GetHashCode();

}
