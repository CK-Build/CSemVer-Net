using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CSemVer;

/// <summary>
/// Handles 5 potentially different versions associated to <see cref="PackageQuality"/>.
/// When all 5 versions are null then <see cref="IsValid"/> is false: as long as at least a <see cref="CI"/> version
/// is specified, this is valid.
/// </summary>
public readonly struct PackageQualityVector : IEnumerable<SVersion>
{
    readonly SVersion? _sta;
    readonly SVersion? _rc;
    readonly SVersion? _pre;
    readonly SVersion? _exp;
    readonly SVersion? _ci;

    /// <summary>
    /// Initializes a new <see cref="PackageQualityVector"/> from a set of versions.
    /// </summary>
    /// <param name="versions">Set of available versions.</param>
    /// <param name="versionsAreOrdered">True to shortcut the work as soon as a <see cref="PackageQuality.Stable"/> has been met.</param>
    public PackageQualityVector( IEnumerable<SVersion> versions, bool versionsAreOrdered = false )
    {
        _ci = _exp = _pre = _rc = _sta = null;
        foreach( var v in versions )
        {
            Apply( v, ref _ci, ref _exp, ref _pre, ref _rc, ref _sta );
            if( versionsAreOrdered && v.PackageQuality == PackageQuality.Stable ) break;
        }
    }

    /// <summary>
    /// Initializes a new <see cref="PackageQualityVector"/> with known best versions.
    /// (this is a low level constructor that does not test/ensure anything).
    /// </summary>
    /// <param name="ci">The current best CI version.</param>
    /// <param name="exp">The current best Exploratory version.</param>
    /// <param name="pre">The current best Preview version.</param>
    /// <param name="rc">The current best ReleaseCandidate version.</param>
    /// <param name="sta">The current best Stable version.</param>
    public PackageQualityVector( SVersion? ci, SVersion? exp, SVersion? pre, SVersion? rc, SVersion? sta )
    {
        _ci = ci;
        _exp = exp;
        _pre = pre;
        _rc = rc;
        _sta = sta;
    }

    /// <summary>
    /// Low level method that applies a new version to a vector of best versions.
    /// </summary>
    /// <param name="v">The new version.</param>
    /// <param name="ci">The current best CI version.</param>
    /// <param name="exp">The current best Exploratory version.</param>
    /// <param name="pre">The current best Preview version.</param>
    /// <param name="rc">The current best ReleaseCandidate version.</param>
    /// <param name="sta">The current best Stable version.</param>
    public static void Apply( SVersion v, [AllowNull] ref SVersion ci, ref SVersion? exp, ref SVersion? pre, ref SVersion? rc, ref SVersion? sta )
    {
        if( v != null && v.IsValid )
        {
            switch( v.PackageQuality )
            {
                case PackageQuality.Stable: if( v > sta ) sta = v; goto case PackageQuality.ReleaseCandidate;
                case PackageQuality.ReleaseCandidate: if( v > rc ) rc = v; goto case PackageQuality.Preview;
                case PackageQuality.Preview: if( v > pre ) pre = v; goto case PackageQuality.Exploratory;
                case PackageQuality.Exploratory: if( v > exp ) exp = v; goto default;
                default: if( v > ci ) ci = v; break;
            }
        }
    }

    PackageQualityVector( PackageQualityVector q, SVersion v )
    {
        Debug.Assert( v?.IsValid ?? false, "v must be not null and valid." );
        _ci = q.CI;
        _exp = q.Exploratory;
        _pre = q.Preview;
        _rc = q.ReleaseCandidate;
        _sta = q.Stable;
        Apply( v, ref _ci, ref _exp, ref _pre, ref _rc, ref _sta );
    }

    /// <summary>
    /// Gets whether this <see cref="PackageQualityVector"/> is valid: at least <see cref="CI"/>
    /// is available.
    /// </summary>
    public bool IsValid => CI != null;

    /// <summary>
    /// Gets the best version for a given quality or null if no such version exists.
    /// </summary>
    /// <param name="quality">The minimal required quality.</param>
    /// <returns>The best version or null if not found.</returns>
    public SVersion? GetVersion( PackageQuality quality )
    {
        return quality switch
        {
            PackageQuality.Stable => Stable,
            PackageQuality.ReleaseCandidate => ReleaseCandidate,
            PackageQuality.Preview => Preview,
            PackageQuality.Exploratory => Exploratory,
            _ => CI,
        };
    }

    /// <summary>
    /// Gets the best stable version or null if no such version exists.
    /// </summary>
    public SVersion? Stable => _sta;

    /// <summary>
    /// Gets the best ReleaseCandidate compatible version or null if no such version exists.
    /// </summary>
    public SVersion? ReleaseCandidate => _rc;

    /// <summary>
    /// Gets the best preview compatible version or null if no such version exists.
    /// </summary>
    public SVersion? Preview => _pre;

    /// <summary>
    /// Gets the best exploratory compatible version or null if no such version exists.
    /// </summary>
    public SVersion? Exploratory => _exp;

    /// <summary>
    /// Gets the best version or null if <see cref="IsValid"/> is false.
    /// </summary>
    public SVersion? CI => _ci;

    /// <summary>
    /// Returns this <see cref="PackageQualityVector"/> or a new one that combines a new version.
    /// </summary>
    /// <param name="v">Version to handle. May be null or invalid.</param>
    /// <returns>The QualityVersions.</returns>
    public PackageQualityVector WithVersion( SVersion v )
    {
        if( v == null || !v.IsValid ) return this;
        return IsValid ? new PackageQualityVector( this, v ) : new PackageQualityVector( new[] { v } );
    }

    /// <summary>
    /// Returns this <see cref="PackageQualityVector"/> or a new one combined with another one.
    /// </summary>
    /// <param name="other">Other versions to be combined.</param>
    /// <returns>The resulting QualityVersions.</returns>
    public PackageQualityVector With( PackageQualityVector other )
    {
        if( !IsValid ) return other;
        if( !other.IsValid ) return this;
        return new PackageQualityVector( other.Concat( this ) );
    }

    /// <summary>
    /// Overridden to return the non null versions separated by / from CI to Stable.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString()
    {
        if( CI == null ) return String.Empty;
        var b = new StringBuilder();
        b.Append( CI.ToString() );
        if( Exploratory != null && Exploratory != CI )
        {
            b.Append( " / " ).Append( Exploratory.ToString() );
        }
        if( Preview != null && Preview != Exploratory )
        {
            b.Append( " / " ).Append( Preview.ToString() );
        }
        if( ReleaseCandidate != null && ReleaseCandidate != Preview )
        {
            b.Append( " / " ).Append( ReleaseCandidate.ToString() );
        }
        if( Stable != null && Stable != ReleaseCandidate )
        {
            b.Append( " / " ).Append( Stable.ToString() );
        }
        return b.ToString();
    }

    /// <summary>
    /// Gets the number of different versions: it is the length of the <see cref="GetEnumerator()"/>
    /// and the number of versions that <see cref="ToString()"/> displays.
    /// This is 0 if <see cref="IsValid"/> is false, up to 5 otherwise.
    /// </summary>
    public int ActualCount
    {
        get
        {
            int c = 0;
            if( CI != null )
            {
                c = 1;
                if( Exploratory != null )
                {
                    if( Exploratory != CI ) c = 2;
                    if( Preview != null )
                    {
                        if( Preview != Exploratory ) ++c;
                        if( ReleaseCandidate != null )
                        {
                            if( ReleaseCandidate != Preview ) ++c;
                            if( Stable != null && Stable != ReleaseCandidate )
                            {
                                ++c;
                            }
                        }
                    }
                }
            }
            return c;
        }
    }

    /// <summary>
    /// Returns the distinct CI, Exploratory, Preview, ReleaseCandidate, Stable (in this order) as long as they are not null.
    /// The actual count is <see cref="ActualCount"/>.
    /// </summary>
    /// <returns>The set of distinct versions (empty if <see cref="IsValid"/> is false).</returns>
    public IEnumerator<SVersion> GetEnumerator()
    {
        if( CI != null )
        {
            yield return CI;
            if( Exploratory != null )
            {
                if( Exploratory != CI ) yield return Exploratory;
                if( Preview != null )
                {
                    if( Preview != Exploratory ) yield return Preview;
                    if( ReleaseCandidate != null )
                    {
                        if( ReleaseCandidate != Preview ) yield return ReleaseCandidate;
                        if( Stable != null && Stable != ReleaseCandidate )
                        {
                            yield return Stable;
                        }
                    }
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
