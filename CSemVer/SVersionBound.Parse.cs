using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CSemVer
{
    public readonly partial struct SVersionBound
    {
        /// <summary>
        /// Tries to parse a version bound: it is a <see cref="SVersion.TryParse(ref ReadOnlySpan{char}, bool, bool)"/> that may be
        /// followed by an optional bracketed "[<see cref="TryParseLockAndMinQuality"/>]".
        /// </summary>
        /// <param name="head">The string to parse (leading and internal white spaces between tokens are skipped).</param>
        /// <param name="bound">The result. This is <see cref="SVersionBound.None"/> on error.</param>
        /// <param name="defaultLock">The <see cref="SVersionLock"/> to use when no lock appears in the string to parse.</param>
        /// <param name="defaultQuality">The <see cref="PackageQuality"/> to use when no lock appears in the string to parse.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParse( ReadOnlySpan<char> head, out SVersionBound bound, SVersionLock defaultLock = SVersionLock.None, PackageQuality defaultQuality = PackageQuality.None ) => TryParse( ref head, out bound, defaultLock, defaultQuality );

        /// <summary>
        /// Tries to parse a version bound: it is a <see cref="SVersion.TryParse(ref ReadOnlySpan{char}, bool, bool)"/> that may be
        /// followed by an optional bracketed "[<see cref="TryParseLockAndMinQuality"/>]".
        /// The head is forwarded right after the match: on success, the head may be on any kind of character.
        /// </summary>
        /// <param name="head">The string to parse (leading and internal white spaces between tokens are skipped).</param>
        /// <param name="bound">The result. This is <see cref="SVersionBound.None"/> on error.</param>
        /// <param name="defaultLock">Default lock to apply if none is provided.</param>
        /// <param name="defaultQuality">Default quality to apply if none is provided.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParse( ref ReadOnlySpan<char> head, out SVersionBound bound, SVersionLock defaultLock = SVersionLock.None, PackageQuality defaultQuality = PackageQuality.None )
        {
            var sHead = head;
            bound = SVersionBound.None;
            var v = SVersion.TryParse( ref Trim( ref head ), checkBuildMetaDataSyntax: false );
            if( !v.IsValid )
            {
                head = sHead;
                return false;
            }
            SVersionLock l = SVersionLock.None;
            PackageQuality q = PackageQuality.None;
            if( Trim( ref head ).Length > 0 && TryMatch( ref head, '[' ) )
            {
                // Allows empty [].
                TryParseLockAndMinQuality( ref head, out l, out q );
                // Match the closing ] if it's here. Ignores it if it's not here.
                if( Trim( ref head ).Length > 0 ) TryMatch( ref head, ']' );
            }
            if( l == SVersionLock.None ) l = defaultLock;
            if( q == PackageQuality.None ) q = defaultQuality;
            bound = new SVersionBound( v, l, q );
            return true;
        }

        /// <summary>
        /// Tries to parse "<see cref="SVersionLock"/>[,<see cref="PackageQuality"/>]" or "<see cref="PackageQuality"/>[,<see cref="SVersionLock"/>]".
        /// Note that match is case insensitive, that white spaces are silently ignored, that all "Lock" wan be written as "Locked" and that "rc" is
        /// a synonym of <see cref="PackageQuality.ReleaseCandidate"/>.
        /// The <paramref name="head"/> is forwarded right after the match: the head may be on any kind of character.
        /// </summary>
        /// <param name="head">The string to parse (leading and internal white spaces are skipped).</param>
        /// <param name="l">The read lock.</param>
        /// <param name="q">The read quality.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParseLockAndMinQuality( ref ReadOnlySpan<char> head,
                                                      out SVersionLock l,
                                                      out PackageQuality q )
        {
            var sSaved = head;
            l = SVersionLock.None;
            q = PackageQuality.None;
            if( head.Length == 0 ) return false;
            if( SVersionLockExtension.TryMatch( ref Trim( ref head ), out l ) )
            {
                if( TryMatch( ref Trim( ref head ), ',' ) )
                {
                    // This handles None,None: if the first read is "None", then it could have been the "None" of the quality:
                    // if we don't match a Quality we give a second chance to the Lock.
                    if( !PackageQualityExtension.TryMatch( ref Trim( ref head ), out q ) && l == SVersionLock.None )
                    {
                        SVersionLockExtension.TryMatch( ref Trim( ref head ), out l );
                    }
                    return true;
                }
                return true;
            }
            if( PackageQualityExtension.TryMatch( ref Trim( ref head ), out q ) )
            {
                if( TryMatch( ref Trim( ref head ), ',' ) )
                {
                    SVersionLockExtension.TryMatch( ref Trim( ref head ), out l );
                    return true;
                }
                return true;
            }
            head = sSaved;
            return false;
        }



        /// <summary>
        /// Captures the result of a parse from other syntaxes that can be invalid or <see cref="IsApproximated"/>.
        /// </summary>
        public readonly struct ParseResult
        {
            /// <summary>
            /// The version bound parsed.
            /// </summary>
            public readonly SVersionBound Result;

            /// <summary>
            /// The error if any (<see cref="IsValid"/> is false).
            /// </summary>
            public readonly string? Error;

            /// <summary>
            /// True if the <see cref="Result"/> is an approximation of the parsed string.
            /// </summary>
            public readonly bool IsApproximated;

            /// <summary>
            /// True if the parsed version had a 4th (or more) part like "1.2.3.4". Those parts are ignored, this
            /// supports legacy package versions (npm and nuget both accept these - and ignores them).
            /// </summary>
            public readonly bool FourthPartLost;

            /// <summary>
            /// Initializes a new valid <see cref="ParseResult"/>.
            /// </summary>
            /// <param name="result">The version bound.</param>
            /// <param name="isApproximated">Whether the version bound is an approximation.</param>
            /// <param name="fourthPartLost">Whether a 4th (or more) part (like in "1.2.3.4") has been ignored.</param>
            public ParseResult( SVersionBound result, bool isApproximated, bool fourthPartLost )
            {
                Result = result;
                IsApproximated = isApproximated;
                FourthPartLost = fourthPartLost;
                Error = null;
            }

            /// <summary>
            /// Initializes a new <see cref="ParseResult"/> on error.
            /// </summary>
            /// <param name="error">The error message.</param>
            public ParseResult( string error )
            {
                Result = SVersionBound.None;
                IsApproximated = FourthPartLost = false;
                Error = error ?? throw new ArgumentNullException( nameof( error ) );
            }

            /// <summary>
            /// Gets whether this is valid (<see cref="Error"/> is null).
            /// </summary>
            public bool IsValid => Error == null;

            /// <summary>
            /// Ensures that this result's <see cref="IsApproximated"/> is true if <paramref name="setApproximated"/> is true
            /// and returns this or a new result.
            /// </summary>
            /// <param name="setApproximated">True to ensures that the flag is set. When false, nothing is done.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult EnsureIsApproximated( bool setApproximated = true )
            {
                return setApproximated && !IsApproximated
                        ? new ParseResult( Result, true, FourthPartLost )
                        : this;
            }

            /// <summary>
            /// Ensures that this result's <see cref="FourthPartLost"/> is true if <paramref name="setFourtPartLost"/> is true
            /// and returns this or a new result.
            /// </summary>
            /// <param name="setFourtPartLost">True to ensures that the flag is set. When false, nothing is done.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult EnsureFourtPartLost( bool setFourtPartLost = true )
            {
                return setFourtPartLost && !FourthPartLost
                        ? new ParseResult( Result, IsApproximated, true )
                        : this;
            }

            internal ParseResult ClearApproximated()
            {
                return IsApproximated
                        ? new ParseResult( Result, false, FourthPartLost )
                        : this;
            }

            /// <summary>
            /// Applies a new <see cref="Result"/> and returns this or a new result.
            /// </summary>
            /// <param name="result">The new result.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult SetResult( SVersionBound result ) => result.Equals( Result )
                                                                        ? this
                                                                        : new ParseResult( result, IsApproximated, FourthPartLost );

            /// <summary>
            /// Sets or concatenates a new <see cref="Error"/> line and returns this or a new result.
            /// </summary>
            /// <param name="error">The error message.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult AddError( string? error ) => error == null || Error == error 
                                                            ? this
                                                            : new ParseResult( Error == null ? error : Error + Environment.NewLine + error );

            /// <summary>
            /// Merges another <see cref="ParseResult"/> with this and returns this or a new result.
            /// Note that error wins and <see cref="IsApproximated"/> and <see cref="FourthPartLost"/> are propagated.
            /// </summary>
            /// <param name="other">The other result.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult Union( in ParseResult other )
            {
                if( Error != null ) return AddError( other.Error );
                if( other.Error != null ) return other;

                var c = Result.Union( other.Result );
                // The result IsApproximate if any of the 2 is an approximation.
                // If both are exact, then the unioned result is exact only if one covers the other.
                return SetResult( c )
                        .EnsureFourtPartLost( other.FourthPartLost )
                        // Testing IsApproximated here shortcuts the Contains evaluation when true.
                        .EnsureIsApproximated( IsApproximated || other.IsApproximated || !(c.Contains( Result ) || c.Contains( other.Result )) );
            }

            /// <summary>
            /// Intersects another <see cref="ParseResult"/> with this and returns this or a new result.
            /// Note that error wins and <see cref="IsApproximated"/> is propagated.
            /// </summary>
            /// <param name="other">The other result.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult Intersect( in ParseResult other )
            {
                if( Error != null ) return AddError( other.Error );
                if( other.Error != null ) return other;

                var c = Result.Intersect( other.Result );
                // The result IsApproximate if any of the 2 is an approximation.
                // If both are exact, then the unioned result is exact only if one covers the other.
                return SetResult( c )
                        .EnsureFourtPartLost( other.FourthPartLost )
                        // Testing IsApproximated here shortcuts the Contains evaluation when true.
                        .EnsureIsApproximated( IsApproximated || other.IsApproximated || !(c.Contains( Result ) || c.Contains( other.Result )) );
            }
        }

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
                    s = s.Slice( 1 );
                    if( s.Length == 0 ) break;
                    v = s[0] - '0';
                }
                while( v >= 0 && v <= 9 );
                return true;
            }
            return false;
        }

        static bool TryMatchXStarInt( ref ReadOnlySpan<char> s, out int i )
        {
            if( s[0] == '*' || s[0] == 'x' || s[0] == 'X' )
            {
                s = s.Slice( 1 );
                i = -1;
                return true;
            }
            return TryMatchNonNegativeInt( ref s, out i );
        }

    }
}
