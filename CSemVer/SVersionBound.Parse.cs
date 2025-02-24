using System;
using System.Diagnostics.CodeAnalysis;

namespace CSemVer;

public readonly partial struct SVersionBound
{
    /// <summary>
    /// Tries to parse a version bound: it is a <see cref="SVersion.TryParse(ref ReadOnlySpan{char}, bool, bool, bool)"/> that may be
    /// followed by an optional bracketed "[Lock,Quality]".
    /// </summary>
    /// <param name="head">The string to parse (leading and internal white spaces between tokens are skipped).</param>
    /// <param name="bound">The result. This is <see cref="SVersionBound.None"/> on error.</param>
    /// <returns>True on success, false otherwise.</returns>
    public static bool TryParse( ReadOnlySpan<char> head, out SVersionBound bound ) => TryParse( ref head, out bound );

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, out SVersionBound)"/>
    public static bool TryParse( ref ReadOnlySpan<char> head, out SVersionBound bound )
    {
        var sHead = head;
        bound = SVersionBound.None;
        var v = SVersion.TryParse( ref Trim( ref head ), checkBuildMetaDataSyntax: false );
        if( !v.IsValid )
        {
            head = sHead;
            return false;
        }
        // Allow empty [].
        SVersionLock l = SVersionLock.NoLock;
        PackageQuality q = PackageQuality.CI;
        if( TryMatch( ref Trim( ref head ), '[' )
            && !TryParseConstraints( ref Trim( ref head ), ref l, ref q ) )
        {
            head = sHead;
            return false;
        }
        bound = new SVersionBound( v, l, q );
        return true;

        static bool TryParseConstraints( ref ReadOnlySpan<char> head,
                                         ref SVersionLock l,
                                         ref PackageQuality q )
        {
            if( SVersionLockExtension.TryMatch( ref head, ref l ) )
            {
                if( TryMatch( ref Trim( ref head ), ',' ) && !PackageQualityExtension.TryMatch( ref Trim( ref head ), ref q ) )
                {
                    return false;
                }
            }
            else if( PackageQualityExtension.TryMatch( ref head, ref q ) )
            {
                if( TryMatch( ref Trim( ref head ), ',' ) && !SVersionLockExtension.TryMatch( ref Trim( ref head ), ref l ) )
                {
                    return false;
                }
            }
            return TryMatch( ref Trim( ref head ), ']' ); ;
        }

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
        /// Gets whether this is valid (<see cref="Error"/> is null).
        /// </summary>
        [MemberNotNullWhen( false, nameof( Error ) )]
        public bool IsValid => Error == null;

        /// <summary>
        /// Initializes a new valid <see cref="ParseResult"/>.
        /// </summary>
        /// <param name="result">The version bound.</param>
        /// <param name="isApproximated">Whether the version bound is an approximation.</param>
        public ParseResult( SVersionBound result, bool isApproximated )
        {
            Result = result;
            IsApproximated = isApproximated;
            Error = null;
        }

        /// <summary>
        /// Initializes a new <see cref="ParseResult"/> on error.
        /// </summary>
        /// <param name="error">The error message.</param>
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
            return setApproximated && IsValid && !IsApproximated
                    ? new ParseResult( Result, true )
                    : this;
        }


        internal ParseResult ClearApproximated()
        {
            return IsApproximated
                    ? new ParseResult( Result, false )
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
        /// Merges another <see cref="ParseResult"/> with this and returns this or a new result.
        /// Note that error wins and <see cref="IsApproximated"/> is propagated.
        /// </summary>
        /// <param name="other">The other result.</param>
        /// <returns>This or a new result.</returns>
        public ParseResult Union( in ParseResult other )
        {
            if( Error != null ) return AddError( other.Error );
            if( other.Error != null ) return other;

            var c = Result.Union( other.Result );
            // The result IsApproximate if any of the 2 is an approximation.
            // If both are exact, then the union-ed result is exact only if one covers the other.
            return SetResult( c )
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
            // If both are exact, then the union-ed result is exact only if one covers the other.
            return SetResult( c )
                    .EnsureIsApproximated( IsApproximated || other.IsApproximated || !(c.Contains( Result ) || c.Contains( other.Result )) );
        }
    }

    static ref ReadOnlySpan<char> Trim( ref ReadOnlySpan<char> s ) { s = s.TrimStart(); return ref s; }

    static bool TryMatch( ref ReadOnlySpan<char> s, char c )
    {
        if( s.Length > 0 && s[0] == c )
        {
            s = s.Slice( 1 );
            return true;
        }
        return false;
    }

    static bool TryMatchNonNegativeInt( ref ReadOnlySpan<char> s, out int i )
    {
        i = 0;
        if( s.Length > 0 )
        {
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
        }
        return false;
    }


}
