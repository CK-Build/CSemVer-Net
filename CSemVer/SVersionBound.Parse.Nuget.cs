using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.Xml.Linq;

namespace CSemVer
{
    public readonly partial struct SVersionBound
    {
        /// <summary>
        /// Attempts to parse a nuget version range. See  https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges.
        /// There is nothing really simple here. Check for instance: https://github.com/NuGet/Home/issues/6434#issuecomment-358782297 
        /// </summary>
        /// <param name="s">The span to parse.</param>
        /// <returns>The result of the parse that can be invalid.</returns>
        public static ParseResult NugetTryParse( ReadOnlySpan<char> s ) => NugetTryParse( ref s );

        /// <summary>
        /// Attempts to parse a nuget version range. See  https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges.
        /// There is nothing really simple here. Check for instance: https://github.com/NuGet/Home/issues/6434#issuecomment-358782297 
        /// </summary>
        /// <param name="head">The span to parse.</param>
        /// <returns>The result of the parse that can be invalid.</returns>
        public static ParseResult NugetTryParse( ref ReadOnlySpan<char> head )
        {
            // Parsing syntactically invalid version is not common: we analyze existing stuff that are supposed
            // to have already been parsed.
            // Instead of handling such errors explicitly, we trap any IndexOutOfRangeException that will eventually be raised.
            var sSaved = head;
            try
            {
                if( Trim( ref head ).Length == 0 ) return new ParseResult( "Version range expected." );
                bool begExclusive = TryMatch( ref head, '(' );
                bool begInclusive = !begExclusive && TryMatch( ref head, '[' );
                if( begInclusive || begExclusive )
                {
                    SVersion? v1 = null;
                    SVersion? v2 = null;
                    bool endInclusive;

                    if( Trim( ref head ).Length == 0 ) return new ParseResult( "Expected comma or version." );
                    bool hasComma = TryMatch( ref head, ',' );
                    if( !hasComma )
                    {
                        if( head.Length == 0 ) return new ParseResult( "Expected nuget version." );
                        v1 = TryParseLoosyVersion( ref head );
                        if( v1.ErrorMessage != null ) return new ParseResult( v1.ErrorMessage );
                        if( Trim( ref head ).Length > 0 )
                        {
                            hasComma = TryMatch( ref head, ',' );
                        }
                    }
                    if( Trim( ref head ).Length == 0 ) return new ParseResult( "Unclosed nuget version range." );

                    if( !hasComma && v1 != null )
                    {
                        if( !begInclusive || !TryMatch( ref head, ']' ) )
                        {
                            return new ParseResult( "Invalid singled version range. Must only be '[version]'." );
                        }
                        return new ParseResult( new SVersionBound( v1, SVersionLock.Lock ), false );
                    }
                    Debug.Assert( hasComma || v1 == null );
                    endInclusive = TryMatch( ref head, ']' );
                    if( !endInclusive && !TryMatch( ref head, ')' ) )
                    {
                        v2 = TryParseLoosyVersion( ref head );
                        if( v2.ErrorMessage != null ) return new ParseResult( v2.ErrorMessage );
                        if( Trim( ref head ).Length == 0
                            || ( !(endInclusive = TryMatch( ref head, ']' )) && !TryMatch( ref head, ')' ) ))
                        {
                            return new ParseResult( "Unclosed nuget version range." );
                        }
                    }
                    else if( v1 == null )
                    {
                        return new ParseResult( "Invalid nuget version range." );
                    }
                    Debug.Assert( v1 != null || v2 != null );
                    return CreateResult( begInclusive, v1, v2, endInclusive );
                }
                return TryParseVersionWithWildcards( ref head );
            }
            catch( IndexOutOfRangeException )
            {
                return new ParseResult( $"Invalid nuget version: '{sSaved.ToString()}'." );
            }

        }

        static ParseResult CreateResult( bool begInclusive, SVersion? v1, SVersion? v2, bool endInclusive )
        {
            if( v1 == null ) v1 = SVersion.ZeroVersion;

            // Special case for [x,x] or [x,x). This is a locked version.
            if( v1 == v2 )
            {
                return new ParseResult( new SVersionBound( v1, SVersionLock.Lock ), false );
            }
            // Currently, we have no way to handle exclusive bounds.
            // The only non approximative projections are:
            //   - [Major.Minor.Patch[-whatever],(Major+1).0.0) => LockMajor
            //   - [Major.Minor.Patch[-whatever],Major.(Minor+1).0) => LockMinor
            //   - [Major.Minor.Patch[-whatever],Major.Minor.(Patch+1)) => LockPatch
            // This is really not an approximation if v2 is actually "v2-0".
            // If v2 is "v2-a" this is less perfect... and when v2 has no prerelease, this is not exact BUT
            // captures the real intent behind the range: we clearly don't want any prerelease of the next major (or
            // minor or patch) to be satisfied!
            //
            // About exclusive lower bound: this doesn't make a lot of sense... That would mean that you release a package
            // that depends on a package "A" (so you necessarily use a given version of it: "vBase") and say: "I can't work with the
            // package "A" is version "vBase". I need a future version... Funny isn't it?
            // So, we deliberately forget the "begInclusive" parameter. It still appears in the parameters of this method for the sake of completeness. 
            //
            if( v2 != null && !endInclusive && (!v2.IsPrerelease || v2.Prerelease == "0" || v2.Prerelease == "a" || v2.Prerelease == "A") )
            {
                if( v1.Major + 1 == v2.Major && v2.Minor == 0 && v2.Patch == 0 )
                {
                    return new ParseResult( new SVersionBound( v1, SVersionLock.LockMajor ), false );
                }
                if( v1.Major == v2.Major && v1.Minor + 1 == v2.Minor && v2.Patch == 0 )
                {
                    return new ParseResult( new SVersionBound( v1, SVersionLock.LockMinor ), false );
                }
                if( v1.Major == v2.Major && v1.Minor == v2.Minor && v1.Patch + 1 == v2.Patch )
                {
                    return new ParseResult( new SVersionBound( v1, SVersionLock.LockPatch ), false );
                }
            }
            // Only if v2 is not null is this an approximation since we ignore the notion of "exclusive lower bound".
            return new ParseResult( new SVersionBound( v1 ), v2 != null );
        }

        static ParseResult TryParseVersionWithWildcards( ref ReadOnlySpan<char> s )
        {
            Debug.Assert( s.Length > 0 );
            var v = SVersion.TryParse( ref s );
            if( v.IsValid ) 
            {
                return new ParseResult( new SVersionBound( v ), false );
            }
            Debug.Assert( v.ErrorMessage != null, "Missing [MemberNotNullWhen] in netstandard2.1" );
            // The version is NOT a valid SVersion: there may be wildcards or a "shorten" version (like "1" or "1.2").
            // In NuGet, "1.0" is not the same as "1.0.*":
            //  - 1.0 => 1.0.0 (Minimum version, inclusive)
            //  - 1.0.* => 1.1.0[LockMinor,Stable]
            //  - 1.0.*-* => 1.1.0[LockMinor,CI]
            // We allow 'x' or '*' for the wildcard (even if 'x' won't appear in a NuGet version range). 
            if( !TryMatchXStarInt( ref s, out var major ) )
            {
                // No major nor wildcard. This is definitly invalid.
                return new ParseResult( v.ErrorMessage );
            }
            if( major == -1 )
            {
                // Skips ".minor.patch" if any.
                var skip = TryMatch( ref s, '.' )
                            && TryMatchXStarInt( ref s, out var _ )
                            && TryMatch( ref s, '.' )
                            && TryMatchXStarInt( ref s, out var _ );
                // "*-*" is all versions. 
                if( TryMatch( ref s ,'-') && TryMatch( ref s, '*' ) )
                {
                    return new ParseResult( SVersionBound.All, false );
                }
                // "*" alone implies only Stable versions.
                return new ParseResult( SVersionBound.All.SetMinQuality( PackageQuality.Stable ), false );
            }
            Debug.Assert( major >= 0 );
            bool expectNextPart = TryMatch( ref s, '.' );
            if( !expectNextPart )
            {
                // "Major" only: (Minimum version, inclusive)
                var bound = new SVersionBound( SVersion.Create( major, 0, 0 ) );
                return new ParseResult( bound, false );
            }
            bool hasNextPart;
            if( ((hasNextPart = TryMatchXStarInt( ref s, out var minor )) && minor == -1) )
            {
                // Skips any ".patch".
                var skip = TryMatch( ref s, '.' )
                           && TryMatchXStarInt( ref s, out var _ );

                var minQuality = TryMatch( ref s, '-' ) && TryMatch( ref s, '*' )
                                    ? PackageQuality.CI
                                    : PackageQuality.Stable;
                var bound = new SVersionBound( SVersion.Create( major, 0, 0 ), SVersionLock.LockMajor, minQuality );
                return new ParseResult( bound, false );
            }
            if( !hasNextPart )
            {
                return new ParseResult( "Missing expected Minor." );
            }
            Debug.Assert( major >= 0 && minor >= 0 );
            expectNextPart = TryMatch( ref s, '.' );
            if( !expectNextPart )
            {
                // "Major.Minor" only: (Minimum version, inclusive)
                var bound = new SVersionBound( SVersion.Create( major, minor, 0 ) );
                return new ParseResult( bound, false );
            }
            if( (hasNextPart = TryMatchXStarInt( ref s, out var patch )) && patch == -1 )
            {
                var minQuality = TryMatch( ref s, '-' ) && TryMatch( ref s, '*' )
                                 ? PackageQuality.CI
                                 : PackageQuality.Stable;
                var bound = new SVersionBound( SVersion.Create( major, minor, 0 ), SVersionLock.LockMinor, minQuality );
                return new ParseResult( bound, false );
            }
            if( !hasNextPart )
            {
                return new ParseResult( "Missing expected Patch." );
            }
            Debug.Assert( major >= 0 && minor >= 0 && patch >= 0 );
            // We have a Major.Minor.Patch here but it has failed to be parsed.
            // The single possibility to be valid is the "-*" pattern:
            // this is a [LockPatch, CI].
            if( TryMatch( ref s, '-' ) && TryMatch( ref s, '*' ) )
            {
                var bound = new SVersionBound( SVersion.Create( major, minor, patch ), SVersionLock.LockPatch, PackageQuality.CI );
                return new ParseResult( bound, false );
            }
            return new ParseResult( v.ErrorMessage );
        }

        static SVersion TryParseLoosyVersion( ref ReadOnlySpan<char> s )
        {
            Debug.Assert( s.Length > 0 );
            var v = SVersion.TryParse( ref s );
            if(!v.IsValid)
            {
                if( TryMatchNonNegativeInt( ref s, out int major ) )
                {
                    if( s.Length == 0 || !TryMatch( ref s, '.' ) )
                    {
                        return SVersion.Create( major, 0, 0 );
                    }
                    if( !TryMatchNonNegativeInt( ref s, out int minor ) )
                    {
                        return new SVersion( "Expected Nuget minor part.", null );
                    }
                    // Try to save the fourth part: in such case the patch is read.
                    int patch = 0;
  
                    return SVersion.Create( major, minor, patch );
                }
            }
            return v;
        }

    }
}
