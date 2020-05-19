using CSemVer;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace CSemVer
{
    /// <summary>
    /// Immutable base <see cref="Base"/> valid version that is the inclusive minimum possible
    /// and an optional <see cref="Lock"/>.
    /// <para>
    /// This aims to define a sensible response to one of the dependency management issue: how to specify
    /// "version ranges".
    /// </para>
    /// </summary>
    public partial class SVersionRange : IEquatable<SVersionRange>, IComparable<SVersionRange>
    {
        /// <summary>
        /// Gets the base version (inclusive minimum version). <see cref="SVersion.IsValid"/> is necessarily true.
        /// </summary>
        public SVersion Base { get; }

        /// <summary>
        /// Gets whether only the same Major, Minor, Patch (or the whole version) of <see cref="Base"/> must be considered.
        /// </summary>
        public SVersionLock Lock { get; }

        /// <summary>
        /// Gets the minimal package quality that must be used.
        /// </summary>
        public PackageQuality MinQuality { get; }

        /// <summary>
        /// Initializes a new version range.
        /// </summary>
        /// <param name="version">The base version that must be valid.</param>
        /// <param name="r">The lock to apply.</param>
        /// <param name="minQuality">The minimal quality to accept.</param>
        public SVersionRange( SVersion version, SVersionLock r = SVersionLock.None, PackageQuality minQuality = PackageQuality.None )
        {
            Base = version ?? throw new ArgumentNullException( nameof( version ) );
            if( !version.IsValid ) throw new ArgumentException( "Must be valid. Error: " + version.ErrorMessage, nameof( version ) );
            MinQuality = minQuality;
            Lock = r;
        }

        /// <summary>
        /// Sets a lock by returning this or a new <see cref="SVersionRange"/>.
        /// </summary>
        /// <param name="r">The lock to set.</param>
        /// <returns>This or a new range.</returns>
        public SVersionRange SetLock( SVersionLock r ) => r != Lock ? new SVersionRange( Base, r, MinQuality ) : this;

        /// <summary>
        /// Sets a lock by returning this or a new <see cref="SVersionRange"/>.
        /// </summary>
        /// <param name="r">The lock to set.</param>
        /// <returns>This or a new range.</returns>
        public SVersionRange SetMinQuality( PackageQuality min ) => MinQuality != min ? new SVersionRange( Base, Lock, min ) : this;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public SVersionRange Union( SVersionRange other )
        {
            if( Base == other.Base )
            {

            }
        }

        /// <summary>
        /// Compares this with another range: the <see cref="Base"/> and the <see cref="Lock"/> (in this order)
        /// are considered.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns>Standard positive, negative or zero value.</returns>
        public int CompareTo( SVersionRange other )
        {
            int cmp = Base.CompareTo( other.Base );
            return cmp == 0 ? (int)Lock - (int)other.Lock : cmp;
        }

        /// <summary>
        /// Equality is based on <see cref="Base"/> and <see cref="Lock"/>.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns>True if they are the same version and restriction.</returns>
        public bool Equals( SVersionRange other ) => Base == other.Base && Lock == other.Lock;

        /// <summary>
        /// Equality is based on <see cref="Base"/> and <see cref="Lock"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals( object obj ) => obj is SVersionRange r && Equals( r );

        /// <summary>
        /// Equality is based on <see cref="Base"/> and <see cref="Lock"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => Base.GetHashCode() ^ ((int)Lock << 26);

        static readonly CSVersion _000Version = CSVersion.FirstPossibleVersions[CSVersion.FirstPossibleVersions.Count - 1];

        static ref ReadOnlySpan<char> Trim( ref ReadOnlySpan<char> s ) { s = s.TrimStart(); return ref s; }

        static bool TryMatch( ref ReadOnlySpan<char> s, char c )
        {
            if( s[0] == c )
            {
                s = s.Slice( 1 );
                return true;
            }
            return false;
        }

        static bool TryMatchNonNegativeInt( ref ReadOnlySpan<char> s, out int i )
        {
            i = 0;
            int v = s[0] - '0';
            if( v >= 0 && v <= 9 )
            {
                do
                {
                    i = i * 10 + v;
                    if( s.Length == 0 ) break;
                    s = s.Slice( 1 );
                    v = s[0] - '0';
                }
                while( v >= 0 && v <= 9 );
                return true;
            }
            return false;
        }

        static bool TryMatchXInt( ref ReadOnlySpan<char> s, out int i )
        {
            if( s[0] == '*' || s[0] == 'x' || s[0] == 'X' )
            {
                s = s.Slice( 1 );
                i = -1;
                return true;
            }
            return TryMatchNonNegativeInt( ref s, out i );
        }

        static (SVersion? Version, int FMajor, int FMinor, string? Error) TryMatchFloatingVersion( ref ReadOnlySpan<char> s )
        {
            // Handling the marvellous "" (empty string), that is like '*'.
            if( s.Length == 0 ) return (null, -1, 0, null);
            var version = SVersion.TryParse( ref s );
            if( version.IsValid ) return (version, 0, 0, null);
            int major, minor = -1;
            if( TryMatchXInt( ref s, out major ) )
            {
                if( TryMatch( ref s, '.' ) && !TryMatchXInt( ref s, out minor ) )
                {
                    return (null, 0, 0, "Expecting minor number or *.");
                }
                if( major < 0 ) minor = 0;
                // Forgetting any trailing "X.Y.*" since it is like "X.Y".
                if( TryMatch( ref s, '.' ) ) TryMatchXInt( ref s, out _ );
                // Even if the npm grammar allows "3.*-alpha" or "3.1.*+meta", we reject this: https://semver.npmjs.com/ selects nothing.
                if( TryMatch( ref s, '-' ) || TryMatch( ref s, '+' ) )
                {
                    return (null, 0, 0, "Floating version should not specify -prerelease tag or +meta data.");
                }
                return (null, major, minor, null);
            }
            return (null, 0, 0, version.ErrorMessage);
        }

        static (SVersionRange? Result, string? Error) TryMatchRangeAlone( ref ReadOnlySpan<char> s, SVersionLock defaultBound, PackageQuality quality )
        {
            var r = TryMatchFloatingVersion( ref s );
            if( r.Error != null ) return (null, r.Error);
            if( r.Version != null ) return (new SVersionRange( r.Version, defaultBound ), null);
            // Empty string, '*' leads to ">=0.0.0" and this excludes any prereleases for npm.
            if( r.FMajor < 0 ) return (new SVersionRange( _000Version, SVersionLock.None, quality ), null);
            if( r.FMinor < 0 ) return (new SVersionRange( SVersion.Create( r.FMajor, 0, 0 ), SVersionLock.LockedMajor, quality ), null);
            return (new SVersionRange( SVersion.Create( r.FMajor, r.FMinor, 0 ), SVersionLock.LockedMinor, quality ), null);
        }

        static (SVersionRange? Result, string? Error) TryMatchHeadRange( ref ReadOnlySpan<char> s, bool includePreRelease, bool strict )
        {
            if( TryMatch( ref s, '>' ) )
            {
                if( !TryMatch( ref s, '=' ) )
                {
                    if( strict ) return (null, "Unhandled comparator.");
                }
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Release );
            }
            if( TryMatch( ref s, '<' ) )
            {
                TryMatch( ref s, '=' );
                if( strict ) return (null, "Unhandled comparator.");
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Release );
            }
            if( TryMatch( ref s, '~' ) )
            {
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockedMinor, includePreRelease ? PackageQuality.None : PackageQuality.Release );
            }
            if( TryMatch( ref s, '^' ) )
            {
                var r = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockedMajor, includePreRelease ? PackageQuality.None : PackageQuality.Release );
                if( r.Error != null ) return (null, r.Error);
                var v = r.Result!;
                if( v.Base.Major == 0 && v.Lock == SVersionLock.LockedMajor )
                {
                    v = v.SetLock( SVersionLock.LockedMinor );
                    if( v.Base.Minor == 0 && v.Lock == SVersionLock.LockedMinor )
                    {
                        v = v.SetLock( SVersionLock.LockedPatch );
                    }
                }
                return (v, null);
            }
            // '=' prefix is optional.
            if( TryMatch( ref s, '=' ) ) Trim( ref s );
            return TryMatchRangeAlone( ref s, SVersionLock.Locked, includePreRelease ? PackageQuality.None : PackageQuality.Release );
        }

        public static (SVersionRange? Result, string? Error) TryMatchRange( ref ReadOnlySpan<char> s, bool includePreRelease, bool strict )
        {
            var r = TryMatchHeadRange( ref s, includePreRelease, strict );
            if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
            if( TryMatch( ref s, '-' ) )
            {
                // https://semver.npmjs.com/ forbids this "1.0.0 -2": there must be a space after the dash.
                // Here, we don't care: we skip any whitespace and eat any upper bound: we simply ignore upper bound.
                TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Release );
            }
            return r;
        }

        public static (SVersionRange? Result, string? Error) NpmTryParse( ReadOnlySpan<char> s, bool includePreRelease, bool strict = false )
        {
            var r = TryMatchRange( ref s, includePreRelease, strict );
            if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
            while( TryMatch( ref s, '|' ) )
            {
                if( !TryMatch( ref s, '|' ) ) return (null, "Expecting '||': '|' alone is invalid.");
                var r2 = TryMatchRange( ref Trim( ref s ), includePreRelease, strict );
                if( r2.Error != null ) return r2;
                r.Result = r.Result!.Union( r2.Result! );
                Trim( ref s );
            }
            return r;
        }

    }

}
