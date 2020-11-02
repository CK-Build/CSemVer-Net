using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace CSemVer
{
    public readonly partial struct SVersionBound
    {
        static readonly SVersion _000Version = SVersion.Create( 0, 0, 0 );

        static (SVersion? Version, int FMajor, int FMinor, string? Error) TryMatchFloatingVersion( ref ReadOnlySpan<char> s )
        {
            // Handling the marvellous "" (empty string), that is like '*'.
            if( s.Length == 0 ) return (null, -1, 0, null);
            var version = SVersion.TryParse( ref s );
            if( version.IsValid ) return (version, 0, 0, null);
            int major, minor = -1;
            if( TryMatchXStarInt( ref s, out major ) )
            {
                if( major >= 0 )
                {
                    if( s.Length > 0 && TryMatch( ref s, '.' ) )
                    {
                        if( s.Length == 0 || !TryMatchXStarInt( ref s, out minor ) )
                        {
                            return (null, 0, 0, "Expecting minor number or *.");
                        }
                    }
                }
                else minor = 0;
                // Forgetting any trailing "X.Y.*" since it is like "X.Y".
                if( s.Length > 0 )
                {
                    if( TryMatch( ref s, '.' ) ) TryMatchXStarInt( ref s, out _ );
                    // Even if the npm grammar allows "3.*-alpha" or "3.1.*+meta", we reject this: https://semver.npmjs.com/ selects nothing.
                    if( s.Length > 0 && (TryMatch( ref s, '-' ) || TryMatch( ref s, '+' )) )
                    {
                        return (null, 0, 0, "Floating version should not specify -prerelease tag or +meta data.");
                    }
                }
                return (null, major, minor, null);
            }
            return (null, 0, 0, version.ErrorMessage);
        }

        static (ParseResult Result, bool IsFloatingMinor) TryMatchRangeAlone( ref ReadOnlySpan<char> s, SVersionLock defaultBound, bool includePreRelease )
        {
            var r = TryMatchFloatingVersion( ref s );
            if( r.Error != null ) return (new ParseResult( r.Error ), false);

            PackageQuality quality = includePreRelease ? PackageQuality.None : PackageQuality.Stable;
            if( r.Version != null )
            {
                // As soon as a prerelease appears, this can only be an approximation (with one exception - see below) since for npm:
                //
                // "For example, the range >1.2.3-alpha.3 would be allowed to match the version 1.2.3-alpha.7, but it
                //  would not be satisfied by 3.4.5-alpha.9, even though 3.4.5-alpha.9 is technically "greater than"
                //  1.2.3-alpha.3 according to the SemVer sort rules. The version range only accepts prerelease tags
                //  on the 1.2.3 version. The version 3.4.5 would satisfy the range, because it does not have a prerelease
                //  flag, and 3.4.5 is greater than 1.2.3-alpha.7."
                //
                // We also set the MinQuality to CI (otherwise alpha.7 will not be authorized for alpha.3) regardless of the includePreRelease.
                // Moreover, if we are coming from the ~ (tilde range), the lock is on the patch, not on the minor, and this exactly matches the
                // npm behavior.
                //
                bool isApproximated = !includePreRelease;
                if( r.Version.IsPrerelease )
                {
                    quality = PackageQuality.None;
                    if( defaultBound == SVersionLock.LockMinor )
                    {
                        defaultBound = SVersionLock.LockPatch;
                        isApproximated = false;
                    }
                }
                return (new ParseResult( new SVersionBound( r.Version, defaultBound, quality ), isApproximated ), false );
            }
            if( r.FMajor < 0 ) return (new ParseResult( new SVersionBound( _000Version, SVersionLock.None, quality ), isApproximated: !includePreRelease), false );
            if( r.FMinor < 0 ) return (new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, 0, 0 ), SVersionLock.LockMajor, quality ), isApproximated: !includePreRelease), true );
            return (new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, r.FMinor, 0 ), SVersionLock.LockMinor, quality ), isApproximated: !includePreRelease ), false );
        }

        static ParseResult TryMatchHeadRange( ref ReadOnlySpan<char> s, bool includePreRelease )
        {
            // Handling the marvellous "" (empty string), that is like '*'.
            if( s.Length == 0 ) return new ParseResult( new SVersionBound( _000Version, SVersionLock.None, includePreRelease ? PackageQuality.None : PackageQuality.Stable ), isApproximated: !includePreRelease );

            if( TryMatch( ref s, '>' ) )
            {
                bool isApproximated = !TryMatch( ref s, '=' );
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ).Result.EnsureIsApproximated( isApproximated );
            }
            if( TryMatch( ref s, '<' ) )
            {
                if( TryMatch( ref s, '=' ) )
                {
                    return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.Lock, includePreRelease ).Result.EnsureIsApproximated( true );
                }
                // We totally ignore any '<'...
                var forget = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ).Result;
                return forget.Error != null ? forget : new ParseResult( SVersionBound.All.SetMinQuality( includePreRelease ? PackageQuality.CI : PackageQuality.Stable ), true );
            }
            if( TryMatch( ref s, '~' ) )
            {
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockMinor, includePreRelease ).Result;
            }
            if( TryMatch( ref s, '^' ) )
            {
                var r = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockMajor, includePreRelease );
                if( r.Result.Error == null )
                {
                    bool noMoreApproximation = false;
                    SVersionBound v = r.Result.Result;
                    if( v.Base.Major == 0 )
                    {
                        if( v.Lock == SVersionLock.LockMajor && !r.IsFloatingMinor )
                        {
                            v = v.SetLock( SVersionLock.LockMinor );
                            if( v.Base.Minor == 0 && v.Lock == SVersionLock.LockMinor )
                            {
                                v = v.SetLock( SVersionLock.LockPatch );
                                noMoreApproximation = true;
                            }
                        }
                    }
                    else
                    {
                        // Major != 0.
                        if( v.Lock > SVersionLock.LockMajor )
                        {
                            v = v.SetLock( SVersionLock.LockMajor );
                        }
                    }
                    r.Result = r.Result.SetResult( v );
                    if( noMoreApproximation ) r.Result = r.Result.ClearApproximated();
                }
                return r.Result;
            }
            // '=' prefix is optional.
            if( TryMatch( ref s, '=' ) ) Trim( ref s );
            return TryMatchRangeAlone( ref s, SVersionLock.Lock, includePreRelease ).Result;
        }

        static ParseResult TryMatchRange( ref ReadOnlySpan<char> s, bool includePreRelease )
        {
            var r = TryMatchHeadRange( ref s, includePreRelease );
            if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
            if( TryMatch( ref s, '-' ) )
            {
                // https://semver.npmjs.com/ forbids this "1.0.0 -2": there must be a space after the dash.
                // Here, we don't care: we skip any whitespace.
                var up = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePreRelease ).Result;
                if( !up.IsValid )
                {
                    // Propagate error.
                    r = r.Union( up );
                }
                else
                {
                    // Discard if up is smaller.
                    if( up.Result.Base >= r.Result.Base )
                    {
                        if( r.Result.Base.Major == up.Result.Base.Major )
                        {
                            r = r.SetResult( r.Result.SetLock( SVersionLock.LockMajor ) );
                            if( r.Result.Base.Minor == up.Result.Base.Minor )
                            {
                                r = r.SetResult( r.Result.SetLock( SVersionLock.LockMinor ) );
                            }
                        }
                        else
                        {
                            r = r.SetResult( r.Result.SetLock( SVersionLock.None ) );
                        }
                    }
                }
                r = r.EnsureIsApproximated( true );
            }
            return r;
        }

        static ParseResult TryMatchSet( ref ReadOnlySpan<char> s, bool includePreRelease )
        {
            var r = TryMatchRange( ref s, includePreRelease );
            for( ; ;)
            {
                if( r.Error != null || Trim( ref s ).Length == 0 || s[0] == '|' ) return r;
                var right = TryMatchRange( ref s, includePreRelease );
                r = r.Intersect( right );
            }
        }

        /// <summary>
        /// Attempts to parse a npm version range. See  https://github.com/npm/node-semver and https://semver.npmjs.com/.
        /// </summary>
        /// <param name="s">The span to parse.</param>
        /// <param name="includePreRelease">
        /// See https://github.com/npm/node-semver#prerelease-tags: setting this to true "treats all prerelease versions
        /// as if they were normal versions, for the purpose of range matching".
        /// When this is false, then <see cref="ParseResult.IsApproximated"/> is always true since fro npm, prerelease
        /// specification is valid only for the same [major, minor, patch] when this "incldePreRelease" flag is not specified... But for us,
        /// this is the default.
        /// </param>
        /// <returns>The result of the parse that can be invalid.</returns>
        public static ParseResult NpmTryParse( ReadOnlySpan<char> s, bool includePreRelease = true )
        {
            // Parsing syntaxically invalid version is not common: we analyze existing stuff that are supposed
            // to have already been parsed.
            // Instead of handling such errors explicitly, we trap any IndexOutOfRangeException that will eventually be raised.
            var sSaved = s;
            try
            {
                var r = TryMatchSet( ref s, includePreRelease );
                if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
                while( r.Error == null
                        && Trim( ref s ).Length > 0
                        && TryMatch( ref s, '|' ) )
                {
                    if( !TryMatch( ref s, '|' ) ) return new ParseResult( "Expecting '||': '|' alone is invalid." );
                    r = r.Union( TryMatchSet( ref Trim( ref s ), includePreRelease ) );
                }
                return r;
            }
            catch( IndexOutOfRangeException )
            {
                return new ParseResult( $"Invalid npm version: '{sSaved.ToString()}'." );
            }

        }
    }
}
