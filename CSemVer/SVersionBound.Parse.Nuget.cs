using System;
using System.Collections.Generic;
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
        public static ParseResult NugetTryParse( ReadOnlySpan<char> s )
        {
            // Parsing syntaxically invalid version is not common: we analyze existing stuff that are supposed
            // to have already been parsed.
            // Instead of handling such errors explicitly, we trap any IndexOutOfRangeException that will eventually be raised.
            var sSaved = s;
            try
            {
                if( Trim( ref s ).Length == 0 ) return new ParseResult( "Version range expected." );
                bool begExclusive = TryMatch( ref s, '(' );
                bool begInclusive = !begExclusive && TryMatch( ref s, '[' );
                if( begInclusive || begExclusive )
                {
                    SVersion? v1 = null;
                    SVersion? v2 = null;
                    bool v1FourthPartLost = false;
                    bool v2FourthPartLost = false;
                    bool endInclusive;

                    bool hasComma = TryMatch( ref s, ',' );
                    if( !hasComma )
                    {
                        if( s.Length == 0 ) return new ParseResult( "Expected nuget version." );
                        v1 = TryParseVersion( ref s, out v1FourthPartLost );
                        if( v1.ErrorMessage != null ) return new ParseResult( v1.ErrorMessage );
                        if( s.Length > 0 )
                        {
                            hasComma = TryMatch( ref s, ',' );
                        }
                    }
                    if( s.Length == 0 ) return new ParseResult( "Unclosed nuget version range." );

                    if( !hasComma && v1 != null )
                    {
                        if( !begInclusive || !TryMatch( ref s, ']' ) )
                        {
                            return new ParseResult( "Invali singled version range. Must only be '[version]'." );
                        }
                        return new ParseResult( new SVersionBound( v1, SVersionLock.Lock ), false );
                    }
                    Debug.Assert( hasComma || v1 == null );
                    endInclusive = TryMatch( ref s, ']' );
                    if( !endInclusive && !TryMatch( ref s, ')' ) )
                    {
                        v2 = TryParseVersion( ref s, out v2FourthPartLost );
                        if( v2.ErrorMessage != null ) return new ParseResult( v2.ErrorMessage );
                        if( s.Length == 0
                            || ( !(endInclusive = TryMatch( ref s, ']' )) && !TryMatch( ref s, ')' ) ))
                        {
                            return new ParseResult( "Unclosed nuget version range." );
                        }
                    }
                    else if( v1 == null )
                    {
                        return new ParseResult( "Invalid nuget version range." );
                    }
                    Debug.Assert( v1 != null || v2 != null );
                    return CreateResult( begInclusive, v1, v2, endInclusive, v1FourthPartLost|v2FourthPartLost );
                }
                return TryParseVersionResult( ref s, false );
            }
            catch( IndexOutOfRangeException )
            {
                return new ParseResult( $"Invalid nuget version: '{sSaved.ToString()}'." );
            }

        }

        static ParseResult CreateResult( bool begInclusive, SVersion? v1, SVersion? v2, bool endInclusive, bool fourthPartLost )
        {
            if( v1 == null ) v1 = SVersion.ZeroVersion;
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
            // About exclusive lower bound: this doesn't make a lot of sense... That would mean that yo release a package
            // that depends on a package "A" (so you necessarily use a given verision of it: "vBase") and say: "I can't work with the
            // package "A" is version "vBase". I need a future version... Funny isn't it?
            // So, we deliberately foget the "begInclusive" parameter. It stil appears in the parameters of this method for the sake of completness. 
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
            return new ParseResult( new SVersionBound( v1 ), v2 != null, fourthPartLost );
        }

        static ParseResult TryParseVersionResult( ref ReadOnlySpan<char> s, bool isApproximate )
        {
            var v = TryParseVersion( ref s, out bool fourthPartLost );
            return v.ErrorMessage == null ? new ParseResult( new SVersionBound( v ), isApproximate, fourthPartLost ) : new ParseResult( v.ErrorMessage );
        }

        static SVersion TryParseVersion( ref ReadOnlySpan<char> s, out bool fourthPartLost )
        {
            Debug.Assert( s.Length > 0 );
            fourthPartLost = false;
            var v = SVersion.TryParse( ref s );
            if( !v.IsValid )
            {
                if( TryMatchNonNegativeInt( ref s, out int major ) )
                {
                    if( s.Length == 0 || !TryMatch( ref s, '.' ) )
                    {
                        return SVersion.Create( major, 0, 0 );
                    }
                    if( !TryMatchNonNegativeInt( ref s, out int minor ) )
                    {
                        return SVersion.Create( "Expected Nuget minor part.", null );
                    }
                    // Try to save the fourth part.
                    if( s.Length > 0 && TryMatch( ref s, '.' )
                        && s.Length > 0 && TryMatchNonNegativeInt( ref s, out int patch )
                        && s.Length > 0 && TryMatch( ref s, '.' )
                        && s.Length > 0 && TryMatchNonNegativeInt( ref s, out int _ ) )
                    {
                        fourthPartLost = true;
                        return SVersion.Create( major, minor, patch );
                    }
                    return SVersion.Create( major, minor, 0 );
                }
            }
            return v;
        }
    }
}
