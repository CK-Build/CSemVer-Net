using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CSemVer
{
    partial class SVersionBound
    {
        static readonly CSVersion _000Version = CSVersion.FirstPossibleVersions[CSVersion.FirstPossibleVersions.Count - 1];

        static (SVersion? Version, int FMajor, int FMinor, string? Error) TryMatchFloatingVersion( ref ReadOnlySpan<char> s )
        {
            // Handling the marvellous "" (empty string), that is like '*'.
            if( s.Length == 0 ) return (null, -1, 0, null);
            var version = SVersion.TryParse( ref s );
            if( version.IsValid ) return (version, 0, 0, null);
            int major, minor = -1;
            if( TryMatchXStarInt( ref s, out major ) )
            {
                if( TryMatch( ref s, '.' ) && !TryMatchXStarInt( ref s, out minor ) )
                {
                    return (null, 0, 0, "Expecting minor number or *.");
                }
                if( major < 0 ) minor = 0;
                // Forgetting any trailing "X.Y.*" since it is like "X.Y".
                if( TryMatch( ref s, '.' ) ) TryMatchXStarInt( ref s, out _ );
                // Even if the npm grammar allows "3.*-alpha" or "3.1.*+meta", we reject this: https://semver.npmjs.com/ selects nothing.
                if( TryMatch( ref s, '-' ) || TryMatch( ref s, '+' ) )
                {
                    return (null, 0, 0, "Floating version should not specify -prerelease tag or +meta data.");
                }
                return (null, major, minor, null);
            }
            return (null, 0, 0, version.ErrorMessage);
        }

        static ParseResult TryMatchRangeAlone( ref ReadOnlySpan<char> s, SVersionLock defaultBound, PackageQuality quality )
        {
            var r = TryMatchFloatingVersion( ref s );
            if( r.Error != null ) return new ParseResult( r.Error );
            if( r.Version != null ) return new ParseResult( new SVersionBound( r.Version, defaultBound, quality ), false );
            // Empty string, '*' leads to ">=0.0.0" and this excludes any prereleases for npm.
            if( r.FMajor < 0 ) return new ParseResult( new SVersionBound( _000Version, SVersionLock.None, PackageQuality.Release ), false );
            if( r.FMinor < 0 ) return new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, 0, 0 ), SVersionLock.LockedMajor, quality ), false );
            return new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, r.FMinor, 0 ), SVersionLock.LockedMinor, quality ), false );
        }

        static ParseResult TryMatchHeadRange( ParseResult? left, ref ReadOnlySpan<char> s, bool includePreRelease, bool strict )
        {
            if( TryMatch( ref s, '>' ) )
            {
                if( !TryMatch( ref s, '=' ) )
                {
                    if( left == null )
                    {
                        if( strict ) return new ParseResult( "Unhandled comparator." );
                        // We approximate a strict > with a >=...
                        return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Release ).SetIsApproximated( true );
                    }
                    Debug.Assert( left.Value.Result != null, "The left result is valid." );
                    // There is a left: if this is one is "contained" in the left, nothing changes.
                    var right = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Release );
                    if( right.Result == null ) return right;

                    if( left.Value.Result.Contains( right.Result ) ) 

                    // 
                }
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Release ).SetIsApproximated( isApproximated );
            }
            if( TryMatch( ref s, '<' ) )
            {
                TryMatch( ref s, '=' );
                if( strict ) return new ParseResult( "Unhandled range: upper limit makes no sense.");
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

        public static ParseResult TryMatchRange( ref ReadOnlySpan<char> s, bool includePreRelease, bool strict )
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

        /// <summary>
        /// Attempts to parse a npm version range. See  https://github.com/npm/node-semver and https://semver.npmjs.com/.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="includePreRelease"></param>
        /// <param name="strict"></param>
        /// <returns></returns>
        public static ParseResult NpmTryParse( ReadOnlySpan<char> s, bool includePreRelease, bool strict = false )
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
