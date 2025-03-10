using System;
using System.Diagnostics;
using System.Threading;

namespace CSemVer;

/// <summary>
/// Immutable <see cref="Base"/> valid version that is the inclusive minimum acceptable <see cref="PackageQuality"/>
/// and an optional <see cref="Lock"/> to the Base's components.
/// <para>
/// This aims to define a sensible response to one of the dependency management issue: how to specify "version ranges".
/// </para>
/// <para>
/// The <see cref="Union(in SVersionBound)"/> binary operation defines a partial order (materialized by <see cref="Contains"/>)
/// on the set of all possible bounds: <see cref="None"/> is the identity element (and the greatest element of the whole set)
/// and <see cref="All"/> is the absorbing element of the <see cref="Union(in SVersionBound)"/> operation and the lowest element
/// of the set.
/// </para>
/// <para>
/// The text format (<see cref="ToString"/> method) is the "Base" version alone or "Base[Lock]", "Base[MinQuality]" or "Base[Lock,MinQuality]":
/// "13.0.2", "1.2.3[Stable]", "1.0.0[LockMinor]", "15.0.5[LockMajor,Preview]". <see cref="TryParse(ReadOnlySpan{char}, out CSemVer.SVersionBound)"/>
/// methods parse them back.
/// </para>
/// <para>
/// Npm and Nuget version range syntax can be parsed with <see cref="NpmTryParse(ReadOnlySpan{char}, bool)"/> or <see cref="NugetTryParse(ReadOnlySpan{char})"/> 
/// methods: they return a <see cref="ParseResult"/> that can be invalid or <see cref="ParseResult.IsApproximated"/>.
/// </para>
/// </summary>
public readonly partial struct SVersionBound : IEquatable<SVersionBound>
{
    readonly SVersion? _base;
    readonly SVersionLock _lock;
    readonly PackageQuality _minQuality;

    /// <summary>
    /// All bound with no restriction: <see cref="Base"/> is <see cref="SVersion.ZeroVersion"/> and there is
    /// no restriction: <see cref="Satisfy(in SVersion, bool)"/> is true for any valid version.
    /// <para>
    /// This bound is the absorbing element of the <see cref="Union(in SVersionBound)"/> operation and the neutral element
    /// of the <see cref="Intersect(in SVersionBound)"/>.
    /// </para>
    /// This is the <c>default</c> of this <see cref="SVersionBound"/> value type.
    /// </summary>
    public static readonly SVersionBound All = new SVersionBound();

    /// <summary>
    /// None bound: <see cref="Base"/> is <see cref="SVersion.LastVersion"/>, <see cref="Lock"/> and <see cref="MinQuality"/> are
    /// the strongest possible (<see cref="SVersionLock.Lock"/> and <see cref="PackageQuality.Stable"/>): <see cref="Satisfy(in SVersion, bool)"/> is
    /// true only for the last version.
    /// This bound is the identity element of the <see cref="Union(in SVersionBound)"/> operation and the absorbing element of the <see cref="Intersect(in SVersionBound)"/>.
    /// </summary>
    public static readonly SVersionBound None = new SVersionBound( SVersion.LastVersion, SVersionLock.Lock, PackageQuality.Stable );

    /// <summary>
    /// Gets the base version (inclusive minimum version). <see cref="SVersion.IsValid"/> is necessarily true
    /// since it defaults to <see cref="SVersion.ZeroVersion"/>.
    /// </summary>
    public SVersion Base => _base ?? SVersion.ZeroVersion;

    /// <summary>
    /// Gets whether only the same Major, Minor, Patch (or the exact version) of <see cref="Base"/> must be considered.
    /// </summary>
    public SVersionLock Lock => _lock;

    /// <summary>
    /// Gets the minimal package quality that must be used.
    /// <see cref="PackageQuality.CI"/> is the minimum.
    /// <para>
    /// Note that when <see cref="Lock"/> is <see cref="SVersionLock.Lock"/>, this is ignored but the value is kept,
    /// not "erased" or set to CI or Stable.
    /// </para>
    /// </summary>
    public PackageQuality MinQuality => _minQuality;

    /// <summary>
    /// Initializes a new version range on a valid <see cref="Base"/> version.
    /// </summary>
    /// <param name="version">The base version that must be valid (defaults to <see cref="SVersion.ZeroVersion"/>).</param>
    /// <param name="lock">The lock to apply.</param>
    /// <param name="minQuality">The minimal quality to accept.</param>
    public SVersionBound( SVersion? version = null,
                          SVersionLock @lock = SVersionLock.NoLock,
                          PackageQuality minQuality = PackageQuality.CI )
    {
        _base = version ?? SVersion.ZeroVersion;
        if( !_base.IsValid ) throw new ArgumentException( "Must be valid. Error: " + _base.ErrorMessage, nameof( version ) );
        if( minQuality == PackageQuality.Stable )
        {
            // LockPatch on Stable is Lock.
            if( @lock == SVersionLock.LockPatch )
            {
                @lock = SVersionLock.Lock;
            }
            // Get rid of any prerelease if we are on a Stable quality.
            if( _base.IsPrerelease )
            {
                _base = SVersion.Create( _base.Major, _base.Minor, _base.Patch );
            }
        }
        _minQuality = minQuality;
        _lock = @lock;
    }

    /// <summary>
    /// Sets a lock by returning this or a new <see cref="SVersionBound"/>.
    /// </summary>
    /// <param name="r">The lock to set.</param>
    /// <returns>This or a new range.</returns>
    public SVersionBound SetLock( SVersionLock r ) => r != _lock ? new SVersionBound( _base, r, _minQuality ) : this;

    /// <summary>
    /// Sets a minimal quality by returning this or a new <see cref="SVersionBound"/>.
    /// </summary>
    /// <param name="min">The minimal quality to set.</param>
    /// <returns>This or a new range.</returns>
    public SVersionBound SetMinQuality( PackageQuality min )
    {
        return min == _minQuality
                ? this
                : new SVersionBound( _base, _lock, min );
    }

    /// <summary>
    /// Merges this version bound with another one: weakest quality wins, weakest lock wins and weakest <see cref="Base"/> version wins.
    /// </summary>
    /// <param name="other">The other bound.</param>
    /// <returns>The union of this and the other.</returns>
    public SVersionBound Union( in SVersionBound other )
    {
        var minBase = _base > other._base ? other : this;
        return minBase.SetLock( _lock.Union( other._lock ) ).SetMinQuality( _minQuality.Union( other._minQuality ) );
    }

    /// <summary>
    /// Intersects this version bound with another one.
    /// </summary>
    /// <param name="other">The other bound.</param>
    /// <returns>The intersection of this and the other.</returns>
    public SVersionBound Intersect( in SVersionBound other )
    {
        var minBase = _base > other._base ? this : other;
        return minBase.SetLock( _lock.Intersect( other._lock ) ).SetMinQuality( _minQuality.Intersect( other._minQuality ) );
    }

    /// <summary>
    /// Applies this bound to a version and returns whether it satisfies this <see cref="SVersionBound"/>.
    /// <para>
    /// This uses the <see cref="SVersion.CSemVerCompareTo(SVersion?, bool)"/> by default (to handle "-a" and "-alpha").
    /// </para>
    /// </summary>
    /// <param name="v">The version to challenge.</param>
    /// <param name="useCSemVerComparison">False to consider the <see cref="SVersion.Prerelease"/> as-is.</param>
    /// <returns>True if this version fits in this bound, false otherwise.</returns>
    public bool Satisfy( in SVersion v, bool useCSemVerComparison = true )
    {
        if( _base == null ) return true;
        int cmp = useCSemVerComparison ? _base.CSemVerCompareTo( v ) : _base.CompareTo( v );
        // If v is lower than this Base, it's over.
        if( cmp > 0 ) return false;
        // If v is the Base, it's trivially okay. 
        if( cmp == 0 ) return true;
        // Is the greater v "reachable"?
        Debug.Assert( v.IsValid, "Since v is greater than this Base and this Base is valid." );
        switch( _lock )
        {
            case SVersionLock.Lock: return false;
            case SVersionLock.LockPatch:
                if( v.Major != _base.Major || v.Minor != _base.Minor || v.Patch != _base.Patch ) return false; break;
            case SVersionLock.LockMinor:
                if( v.Major != _base.Major || v.Minor != _base.Minor ) return false; break;
            case SVersionLock.LockMajor:
                if( v.Major != _base.Major ) return false; break;
        }
        return _minQuality <= v.PackageQuality;
    }

    /// <summary>
    /// Returns <see cref="SVersionBound.All"/> if this bound is the "*" or "" of npm.
    /// <para>
    /// For npm, "*" and "" are ">=0.0.0[Stable]" when the "includePrerelease" is not used and this is the default.
    /// In such case, this will return the <see cref="All"/> bound that is ">=0.0.0-0".
    /// </para>
    /// </summary>
    /// <returns>This bound or the <see cref="All"/>.</returns>
    public SVersionBound NormalizeNpmVersionBoundAll()
    {
        return _base == _000Version && _lock == SVersionLock.NoLock && _minQuality == PackageQuality.Stable
                ? All
                : this;
    }

    /// <summary>
    /// Checks whether this version bound supersedes another one.
    /// </summary>
    /// <param name="other">The other bound.</param>
    /// <returns>True if this version bound supersedes the other one.</returns>
    public bool Contains( in SVersionBound other )
    {
        // If the other.Base version doesn't satisfy this bound, it's over.
        if( !Satisfy( other.Base ) ) return false;
        // But this is not enough: the versions that the other satisfy must all be satisfied by this bound.
        // If the other allows lowest quality, it's over.
        if( _minQuality > other._minQuality ) return false;
        // Trivial case for locks: this Lock is stronger than the other one: it's over (for
        // instance, this locks the Minor and the other one only locks the Major: it will allow
        // versions that this one disallows).
        if( _lock > other._lock ) return false;
        // When the other Lock is stronger than this one, since the other.Base satisfies this Base,
        // the other cannot be more restrictive than this... I know it may not be obvious, but it's true.
        // To "see" this consider that Base versions "starts" with the "locked" part and "ends free". The
        // exclamation point expresses this:
        //  - This: 1!.0.0-beta (LockMajor) 
        //  - Other: 1.0.0!-rc (LockPatch)
        // If we are here, then the prefix of the other.Base satisfies this bound: any stronger locks
        // don't change the prefix.
        return true;
    }

    /// <summary>
    /// Equality is based on <see cref="Base"/>, <see cref="MinQuality"/> and <see cref="Lock"/>.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <returns>True if they are the same version and restrictions.</returns>
    public bool Equals( SVersionBound other ) => Base == other.Base && _minQuality == other._minQuality && _lock == other._lock;

    /// <summary>
    /// Equality is based on <see cref="Base"/>, <see cref="MinQuality"/> and <see cref="Lock"/>.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
    public override bool Equals( object obj ) => obj is SVersionBound r && Equals( r );

    /// <summary>
    /// Equality is based on <see cref="Base"/>, <see cref="MinQuality"/> and <see cref="Lock"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Base.GetHashCode() ^ ((int)_minQuality << 13) ^ ((int)_lock << 26);

    /// <summary>
    /// Support == operator.
    /// </summary>
    /// <param name="b1">The left bound.</param>
    /// <param name="b2">The right bound.</param>
    /// <returns>True if equal, false otherwise.</returns>
    public static bool operator ==( in SVersionBound b1, in SVersionBound b2 ) => b1.Equals( b2 );

    /// <summary>
    /// Support != operator.
    /// </summary>
    /// <param name="b1">The left bound.</param>
    /// <param name="b2">The right bound.</param>
    /// <returns>True if different, false when the are equal.</returns>
    public static bool operator !=( in SVersionBound b1, in SVersionBound b2 ) => !b1.Equals( b2 );

    /// <summary>
    /// Overridden to return the base version and the restrictions.
    /// <list type="bullet">
    ///     <item>
    ///     <see cref="SVersionLock.NoLock"/>,<see cref="PackageQuality.CI"/>: only Base version is returned ("1.2.3").
    ///     </item>
    ///     <item>
    ///     <see cref="SVersionLock.NoLock"/>, any other MinQuality: Base[MinQuality] is returned ("1.2.3[Stable]").
    ///     </item>
    ///     <item>
    ///     <see cref="SVersionLock.Lock"/>,<see cref="PackageQuality.CI"/>: locked Base version is returned ("1.2.3[Lock]").
    ///     </item>
    ///     <item>
    ///     Otherwise both are displayed "1.2.3[LockMajor,CI]". 
    ///     </item>
    /// </list>
    /// This has been designed to be roundtripable: it can always be parsed back by <see cref="TryParse(ReadOnlySpan{char}, out SVersionBound)"/>.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString()
    {
        if( _lock == SVersionLock.NoLock )
        {
            return _minQuality != PackageQuality.CI ? $"{Base}[{_minQuality}]" : Base.ToString();
        }
        else
        {
            if( _lock == SVersionLock.Lock && _minQuality == PackageQuality.CI )
            {
                return $"{Base}[{_lock}]";
            }
            return $"{Base}[{_lock},{_minQuality}]";
        }
    }

    /// <summary>
    /// [Highly perfectible!] Returns the best possible NuGet version range for this bound.
    /// <para>
    /// What we can guaranty here is that if the initial parse result gives us a <see cref="SVersionBound"/>,
    /// its <see cref="ToNpmString()"/> parsed back provides the exact same SVersionBound... but no more.
    /// </para>
    /// <list type="bullet">
    ///     <item><see cref="SVersionLock.Lock"/> is expressed in brackets: [5.1.2].</item>
    ///     <item>
    ///     <see cref="SVersionLock.LockMajor"/> is "5.*" when <see cref="PackageQuality.Stable"/>
    ///     and "5.*-*" for all other qualities.
    ///     </item>
    ///     <item>
    ///     <see cref="SVersionLock.LockMinor"/> is "5.3.*" when <see cref="PackageQuality.Stable"/>
    ///     and "5.3.*-*" for all other qualities.
    ///     </item>
    ///     <item>
    ///     <see cref="SVersionLock.LockPatch"/> is "5.3.1-*" because LockPatch can only be not stable
    ///     (the [LockPatch,Stable] combination is normalized as [Lock]).
    ///     </item>
    ///     <item>
    ///     The "0.0.0" version when Stable is expressed as "*".
    ///     </item>
    ///     <item>
    ///     The "0.0.0-0" version when NOT Stable is expressed as "*-*".
    ///     </item>
    /// </list>
    /// All other versions are simply the <see cref="Base"/> version: this uses the NuGet "min version inclusive" range.
    /// </summary>
    /// <returns>The NuGet version range.</returns>
    public string ToNuGetString()
    {
        if( _lock == SVersionLock.Lock )
        {
            return $"[{Base}]";
        }
        if( _lock == SVersionLock.LockMajor )
        {
            var suffix = _minQuality == PackageQuality.Stable ? "" : "-*";
            return $"{Base.Major}.*{suffix}";

        }
        var b = Base;
        if( _lock == SVersionLock.LockMinor )
        {
            var suffix = _minQuality == PackageQuality.Stable ? "" : "-*";
            return $"{b.Major}.{b.Minor}.*{suffix}";
        }
        if( Lock == SVersionLock.LockPatch )
        {
            Debug.Assert( _minQuality != PackageQuality.Stable, "Normalized in ctor." );
            return $"{b.Major}.{b.Minor}.{b.Patch}-*";
        }
        // There is no Lock. There is unfortunately no way to express
        // the quality. The "min version inclusive" is the only way except
        // for the special case "0.0.0" when Stable that is "*" and the
        // ZeroVersion "0.0.0-0" when NOT stable that is "*-*".
        if( b.IsZeroVersion && _minQuality != PackageQuality.Stable ) return "*-*";
        if( _minQuality == PackageQuality.Stable
            && b.Major == 0
            && b.Minor == 0
            && b.Patch == 0
            && !b.IsPrerelease )
        {
            return "*";
        }
        return b.ToString();
    }

    /// <summary>
    /// [Highly perfectible!] Returns an approximation as a npm version range.
    /// <para>
    /// What we can guaranty here is that if the initial parse result gives us a <see cref="SVersionBound"/>,
    /// its <see cref="ToNpmString()"/> parsed back provides the exact same SVersionBound... but no more.
    /// </para>
    /// </summary>
    /// <returns>An approximated npm version range (but round-trippable).</returns>
    public string ToNpmString()
    {
        // If we have a prerealease tag and accept CI, we can use the "not includePreRelease" default
        // behavior: see https://github.com/npm/node-semver?tab=readme-ov-file#caret-ranges-123-025-004
        // This is very loose... But since prerelease tags are not use in the npm ecosystem (IMO because
        // it is unusable), we don't really care...
        //
        // There is a single exception to this...
        // Whatever the includePrerelease is, when we parse ">=0.0.0-0" we obtain the SVersionBound.All
        // (whereas "*" or "" give "^0.0.0" with the default false includePrerelease).
        // When this is the All (0.0.0-0[NoLock,CI]) then we return the ">=0.0.0-0": the SVersionBound.All expressed as
        // ">=0.0.0-0" is roundtripable and this is important.
        // 
        // We could have also extended the exception to more "0.0.0" version but it is safer avoid exceptions
        // (the npm ecosystem uses the 0 major a lot). This Zero issue is better handled by using
        // the NormalizeNpmVersionBoundAll() helper.
        //
        var b = Base;
        if( b.IsPrerelease && _minQuality != PackageQuality.Stable )
        {
            if( b.IsZeroVersion && _lock == SVersionLock.NoLock && _minQuality == PackageQuality.CI )
            {
                return ">=0.0.0-0";
            }
            return $"^{b}";
        }
        // If we are locked, use the "=".
        if( _lock == SVersionLock.Lock )
        {
            return $"={b}";
        }
        if( _lock == SVersionLock.LockMajor )
        {
            if( b.Patch == 0 )
            {
                if( b.Minor == 0 )
                {
                    return $"^{b.Major}";
                }
                return $"^{b.Major}.{b.Minor}";
            }
            return $"^{b.Major}.{b.Minor}.{b.Patch}";
        }
        if( Lock == SVersionLock.LockMinor )
        {
            if( b.Patch == 0 )
            {
                if( b.Minor == 0 )
                {
                    return $"~{b.Major}";
                }
                return $"~{b.Major}.{b.Minor}";
            }
            return $"~{b.Major}.{b.Minor}.{b.Patch}";
        }
        return $">={b}";
    }
}
