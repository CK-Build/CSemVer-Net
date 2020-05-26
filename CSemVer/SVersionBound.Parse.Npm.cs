using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace CSemVer
{
    public readonly partial struct SVersionBound
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

        static ParseResult TryMatchRangeAlone( ref ReadOnlySpan<char> s, SVersionLock defaultBound, bool includePreRelease )
        {
            var r = TryMatchFloatingVersion( ref s );
            if( r.Error != null ) return new ParseResult( r.Error );

            PackageQuality quality = includePreRelease ? PackageQuality.None : PackageQuality.StableRelease;
            if( r.Version != null )
            {
                return new ParseResult( new SVersionBound( r.Version, defaultBound, quality ), isApproximated: false );
            }
            if( r.FMajor < 0 ) return new ParseResult( new SVersionBound( _000Version, SVersionLock.None, quality ), false );
            if( r.FMinor < 0 ) return new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, 0, 0 ), SVersionLock.LockedMajor, quality ), false );
            return new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, r.FMinor, 0 ), SVersionLock.LockedMinor, quality ), false );
        }

        static ParseResult TryMatchHeadRange( ParseResult left, ref ReadOnlySpan<char> s, bool includePreRelease )
        {
            if( TryMatch( ref s, '>' ) )
            {
                bool isApproximated = !TryMatch( ref s, '=' );
                // if( strict && isApproximated ) return new ParseResult( "> alone is not handled.");
                var right = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease );
                return left.Union( right ).EnsureIsApproximated( isApproximated );
            }
            if( TryMatch( ref s, '<' ) )
            {
                TryMatch( ref s, '=' );
                // if( strict ) return new ParseResult( "Unhandled range: upper limit makes no sense.");
                var forget = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease );
                return forget.Error != null ? forget : left.EnsureIsApproximated( true );
            }
            if( TryMatch( ref s, '~' ) )
            {
                return left.Union( TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockedMinor, includePreRelease ) );
            }
            if( TryMatch( ref s, '^' ) )
            {
                var r = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockedMajor, includePreRelease );
                if( r.Error == null )
                {
                    SVersionBound v = r.Result;
                    if( v.Base.Major == 0 && v.Lock == SVersionLock.LockedMajor )
                    {
                        v = v.SetLock( SVersionLock.LockedMinor );
                        if( v.Base.Minor == 0 && v.Lock == SVersionLock.LockedMinor )
                        {
                            v = v.SetLock( SVersionLock.LockedPatch );
                        }
                        r = r.SetResult( v );
                    }
                }
                return left.Union( r );
            }
            // '=' prefix is optional.
            if( TryMatch( ref s, '=' ) ) Trim( ref s );
            return left.Union( TryMatchRangeAlone( ref s, SVersionLock.Locked, includePreRelease ) );
        }

        public static ParseResult TryMatchRange( ref ReadOnlySpan<char> s, bool includePreRelease )
        {
            var r = TryMatchHeadRange( new ParseResult( SVersionBound.None, false ), ref s, includePreRelease );
            if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
            if( TryMatch( ref s, '-' ) )
            {
                // https://semver.npmjs.com/ forbids this "1.0.0 -2": there must be a space after the dash.
                // Here, we don't care: we skip any whitespace and eat any upper bound: we simply ignore upper bound.
                TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease );
                r = r.EnsureIsApproximated( true );
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
        public static ParseResult NpmTryParse( ReadOnlySpan<char> s, bool includePreRelease )
        {
            var r = TryMatchRange( ref s, includePreRelease );
            if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
            while( r.Error == null
                    && Trim( ref s ).Length > 0
                    && TryMatch( ref s, '|' ) )
            {
                if( !TryMatch( ref s, '|' ) ) return new ParseResult( "Expecting '||': '|' alone is invalid." );
                r = r.Union( TryMatchRange( ref Trim( ref s ), includePreRelease ) );
            }
            return r;
        }
    }
}
