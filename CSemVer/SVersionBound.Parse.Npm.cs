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

        static (SVersion? Version, int FMajor, int FMinor, string? Error, bool FourtPartLost) TryMatchFloatingVersion( ref ReadOnlySpan<char> s )
        {
            // Handling the marvelous "" (empty string), that is like '*'.
            if( s.Length == 0 ) return (null, -1, 0, null, false);
            var version = SVersion.TryParse( ref s );
            if( version.IsValid )
            {
                // If only the 3 first parts have been read: launch SkipExtraPartsAndPrereleaseIfAny on the head to skip
                // potential extra parts and prerelease.
                return (version, 0, 0, null, SkipExtraPartsAndPrereleaseIfAny( ref s ));
            }
            int major, minor = -1;
            if( TryMatchXStarInt( ref s, out major ) )
            {
                if( major >= 0 )
                {
                    if( s.Length > 0 && TryMatch( ref s, '.' ) )
                    {
                        if( s.Length == 0 || !TryMatchXStarInt( ref s, out minor ) )
                        {
                            return (null, 0, 0, "Expecting minor number or *.", false);
                        }
                        if( minor >= 0 )
                        {
                            // If a fourth part caused the version parse to fail, handle it here.
                            if( s.Length > 0 && TryMatch( ref s, '.' ) && s.Length > 0 && TryMatchNonNegativeInt( ref s, out int patch ) )
                            {
                                return (SVersion.Create( major, minor, patch ), 0, 0, null, SkipExtraPartsAndPrereleaseIfAny( ref s ));
                            }
                        }
                    }
                }
                else minor = 0;
                // Forgetting any trailing "X.Y.*" since it is like "X.Y".
                // Even if the npm grammar allows "3.*-alpha" or "3.1.*+meta", we ignores this: https://semver.npmjs.com/ selects nothing.
                // We consider this stupid trail as being FourthPartLost.
                return (null, major, minor, null, SkipExtraPartsAndPrereleaseIfAny( ref s ) );
            }
            return (null, 0, 0, version.ErrorMessage, false);
        }

        static (ParseResult Result, bool IsFloatingMinor) TryMatchRangeAlone( ref ReadOnlySpan<char> s, SVersionLock defaultBound, bool includePrerelease )
        {
            var r = TryMatchFloatingVersion( ref s );
            if( r.Error != null ) return (new ParseResult( r.Error ), false);

            PackageQuality quality = includePrerelease ? PackageQuality.None : PackageQuality.Stable;
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
                // We also set the MinQuality to CI (otherwise alpha.7 will not be authorized for alpha.3) regardless of the includePrerelease.
                // Moreover, if we are coming from the ~ (tilde range), the lock is on the patch, not on the minor, and this exactly matches the
                // npm behavior.
                //
                bool isApproximated = !includePrerelease;
                if( r.Version.IsPrerelease )
                {
                    quality = PackageQuality.None;
                    if( defaultBound == SVersionLock.LockMinor )
                    {
                        defaultBound = SVersionLock.LockPatch;
                        isApproximated = false;
                    }
                }
                return (new ParseResult( new SVersionBound( r.Version, defaultBound, quality ), isApproximated, r.FourtPartLost ), false );
            }
            if( r.FMajor < 0 ) return (new ParseResult( new SVersionBound( _000Version, SVersionLock.None, quality ), isApproximated: !includePrerelease, false), false );
            if( r.FMinor < 0 ) return (new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, 0, 0 ), SVersionLock.LockMajor, quality ), isApproximated: !includePrerelease, false), true );
            return (new ParseResult( new SVersionBound( SVersion.Create( r.FMajor, r.FMinor, 0 ), SVersionLock.LockMinor, quality ), isApproximated: !includePrerelease, false ), false );
        }

        static ParseResult TryMatchHeadRange( ref ReadOnlySpan<char> s, bool includePrerelease )
        {
            // Handling the marvelous "" (empty string), that is like '*'.
            if( s.Length == 0 ) return new ParseResult( new SVersionBound( _000Version, SVersionLock.None, includePrerelease ? PackageQuality.None : PackageQuality.Stable ), isApproximated: !includePrerelease, false );

            if( TryMatch( ref s, '>' ) )
            {
                bool isApproximated = !TryMatch( ref s, '=' );
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePrerelease ).Result.EnsureIsApproximated( isApproximated );
            }
            if( TryMatch( ref s, '<' ) )
            {
                if( TryMatch( ref s, '=' ) )
                {
                    return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.Lock, includePrerelease ).Result.EnsureIsApproximated( true );
                }
                // We totally ignore any '<'...
                var forget = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePrerelease ).Result;
                return forget.Error != null ? forget : new ParseResult( SVersionBound.All.SetMinQuality( includePrerelease ? PackageQuality.CI : PackageQuality.Stable ), true, forget.FourthPartLost );
            }
            if( TryMatch( ref s, '~' ) )
            {
                return TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockMinor, includePrerelease ).Result;
            }
            if( TryMatch( ref s, '^' ) )
            {
                var r = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.LockMajor, includePrerelease );
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
            return TryMatchRangeAlone( ref s, SVersionLock.Lock, includePrerelease ).Result;
        }

        static ParseResult TryMatchRange( ref ReadOnlySpan<char> s, bool includePrerelease )
        {
            var r = TryMatchHeadRange( ref s, includePrerelease );
            if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
            if( TryMatch( ref s, '-' ) )
            {
                // https://semver.npmjs.com/ forbids this "1.0.0 -2": there must be a space after the dash.
                // Here, we don't care: we skip any whitespace.
                var up = TryMatchRangeAlone( ref Trim( ref s ), SVersionLock.None, includePrerelease ).Result;
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

        static ParseResult TryMatchSet( ref ReadOnlySpan<char> s, bool includePrerelease )
        {
            var r = TryMatchRange( ref s, includePrerelease );
            for( ; ;)
            {
                if( r.Error != null || Trim( ref s ).Length == 0 || s[0] == '|' ) return r;
                var right = TryMatchRange( ref s, includePrerelease );
                r = r.Intersect( right );
            }
        }

        /// <summary>
        /// Attempts to parse a npm version range. See  https://github.com/npm/node-semver and https://semver.npmjs.com/.
        /// </summary>
        /// <param name="s">The span to parse.</param>
        /// <param name="includePrerelease">
        /// See https://github.com/npm/node-semver#prerelease-tags: setting this to true "treats all prerelease versions
        /// as if they were normal versions, for the purpose of range matching".
        /// When this is false, then <see cref="ParseResult.IsApproximated"/> is always true since fro npm, prerelease
        /// specification is valid only for the same [major, minor, patch] when this "incldePreRelease" flag is not specified... But for us,
        /// this is the default.
        /// </param>
        /// <returns>The result of the parse that can be invalid.</returns>
        public static ParseResult NpmTryParse( ReadOnlySpan<char> s, bool includePrerelease = true ) => NpmTryParse( ref s, includePrerelease );

        /// <summary>
        /// Attempts to parse a npm version range. See https://github.com/npm/node-semver and https://semver.npmjs.com/.
        /// </summary>
        /// <param name="s">The span to parse.</param>
        /// <param name="includePrerelease">
        /// See https://github.com/npm/node-semver#prerelease-tags: setting this to true "treats all prerelease versions
        /// as if they were normal versions, for the purpose of range matching".
        /// When this is false, then <see cref="ParseResult.IsApproximated"/> is always true since fro npm, prerelease
        /// specification is valid only for the same [major, minor, patch] when this "includePrerelease" flag is not specified... But for us,
        /// this is the default.
        /// </param>
        /// <returns>The result of the parse that can be invalid.</returns>
        public static ParseResult NpmTryParse( ref ReadOnlySpan<char> s, bool includePrerelease = true )
        {
            // Parsing syntactically invalid version is not common: we analyze existing stuff that are supposed
            // to have already been parsed.
            // Instead of handling such errors explicitly, we trap any IndexOutOfRangeException that will eventually be raised.
            var sSaved = s;
            try
            {
                var r = TryMatchSet( ref s, includePrerelease );
                if( r.Error != null || Trim( ref s ).Length == 0 ) return r;
                while( r.Error == null
                        && Trim( ref s ).Length > 0
                        && TryMatch( ref s, '|' ) )
                {
                    if( !TryMatch( ref s, '|' ) ) return new ParseResult( "Expecting '||': '|' alone is invalid." );
                    r = r.Union( TryMatchSet( ref Trim( ref s ), includePrerelease ) );
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
