using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    public readonly partial struct SVersionBound
    {

        public readonly struct ParseResult
        {
            public readonly SVersionBound Result;
            public readonly string? Error;
            public readonly bool IsApproximated;

            public ParseResult( SVersionBound result, bool isApproximated )
            {
                Result = result;
                IsApproximated = isApproximated;
                Error = null;
            }

            public ParseResult( string error )
            {
                Result = SVersionBound.None;
                IsApproximated = false;
                Error = error ?? throw new ArgumentNullException( nameof( error ) );
            }

            /// <summary>
            /// Ensures that this result's <see cref="IsApproximated"/> is true if <paramref name="setApproximated"/> is true
            /// and returns this or a new result.
            /// </summary>
            /// <param name="setApproximated">True to ensures that the flag is set. When false, nothing is done.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult EnsureIsApproximated( bool setApproximated = true )
            {
                return setApproximated && !IsApproximated
                        ? new ParseResult( Result, setApproximated )
                        : this;
            }

            /// <summary>
            /// Applies a new <see cref="Result"/> and returns this or a new result.
            /// </summary>
            /// <param name="result">The new result.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult SetResult( SVersionBound result ) => result.Equals( Result )
                                                                        ? this
                                                                        : new ParseResult( result, IsApproximated );

            /// <summary>
            /// Sets or concatenates a new <see cref="Error"/> line and returns this or a new result.
            /// </summary>
            /// <param name="error">The error message.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult AddError( string? error ) => error == null || Error == error 
                                                            ? this
                                                            : new ParseResult( Error == null ? error : Error + Environment.NewLine + error );

            /// <summary>
            /// Merges another <see cref="ParseResult"/> with this and returs this or a new result.
            /// Note that error wins and <see cref="IsApproximated"/> is propagated.
            /// </summary>
            /// <param name="result">The new result.</param>
            /// <returns>This or a new result.</returns>
            public ParseResult Union( in ParseResult other )
            {
                if( Error != null ) return AddError( other.Error );
                if( other.Error != null ) return other;

                var c = Result.Union( other.Result );
                bool isCovered = c.Equals( Result ) || c.Equals( other.Result );
                return SetResult( c ).EnsureIsApproximated( !isCovered );
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
                    if( s.Length == 0 ) break;
                    s = s.Slice( 1 );
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