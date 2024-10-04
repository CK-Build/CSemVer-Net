using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CSemVer;

public sealed partial class CSVersion
{
    internal static CSVersion? FromSVersion( string? parsedText, int major, int minor, int patch, string prerelease, string metadata )
    {
        if( major > MaxMajor || minor > MaxMinor || patch > MaxPatch ) return null;
        var error = ParsePreRelease( prerelease, out string _, out int prNameIdx, out int prNum, out int prPatch, out bool longForm );
        if( error != null ) return null;
        return new CSVersion( major, minor, patch, metadata, prNameIdx, prNum, prPatch, longForm, 0, parsedText );
    }

    static string? ParsePreRelease( string prerelease, out string prName, out int prNameIdx, out int prNum, out int prPatch, out bool longForm )
    {
        prName = String.Empty;
        prNameIdx = -1;
        prNum = 0;
        prPatch = 0;
        longForm = false;
        if( prerelease.Length > 0 )
        {
            Match m = _rRelaxed.Match( prerelease );
            prName = m.Groups[1].Value;
            if( !m.Success || prName.Length == 0 )
            {
                return "CSVersion prerelease name must match a|b|d|e|g|k|p|r|alpha|beta|delta|epsilon|gamma|kappa|pre(view|release)?|rc.";
            }
            longForm = prName.Length > 1;
            prNameIdx = prName.Length == 0 ? -1 : Array.IndexOf( _standardNamesC, Char.ToLowerInvariant( prName[0] ) );
            string sPRNum = m.Groups[2].Value;
            string sPRFix = m.Groups[3].Value;
            if( sPRFix.Length > 0 ) prPatch = Int32.Parse( sPRFix );
            if( sPRNum.Length > 0 ) prNum = Int32.Parse( sPRNum );
            if( prPatch == 0 && prNum == 0 && sPRNum.Length > 0 ) return String.Format( "Incorrect '.0' Release Number version. 0 can appear only to fix the first prerelease (for instance '.0.F' where F is between 1 and {0}).", MaxPreReleasePatch );
        }
        return null;
    }

    /// <summary>
    /// Parses the specified string to a constrained semantic version and returns a <see cref="CSVersion"/> that 
    /// may not be <see cref="SVersion.IsValid"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="SVersion.BuildMetaData"/> compliance.</param>
    /// <returns>The CSVersion object that may not be <see cref="SVersion.IsValid"/>.</returns>
    public static CSVersion TryParse( string s, bool checkBuildMetaDataSyntax = true )
    {
        SVersion sv = SVersion.TryParse( s, true, checkBuildMetaDataSyntax );
        if( sv is CSVersion v ) return v;
        Debug.Assert( sv.IsValid == (sv.ErrorMessage == null) );
        return new CSVersion( sv.ErrorMessage ?? "Not a CSVersion.", s );
    }

    /// <summary>
    /// Standard TryParse pattern that returns a boolean rather than the resulting <see cref="CSVersion"/>.
    /// See <see cref="TryParse(string,bool)"/>.
    /// </summary>
    /// <param name="s">String to parse.</param>
    /// <param name="v">Resulting version.</param>
    /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="SVersion.BuildMetaData"/> compliance.</param>
    /// <returns>True on success, false otherwise.</returns>
    public static bool TryParse( string s, [MaybeNullWhen( false )] out CSVersion v, bool checkBuildMetaDataSyntax = true )
    {
        v = null;
        SVersion sv = SVersion.TryParse( s, true, checkBuildMetaDataSyntax );
        if( !sv.IsValid ) return false;
        v = sv as CSVersion;
        return v != null;
    }

    /// <summary>
    /// Parses the specified string to a constrained semantic version and throws an <see cref="ArgumentException"/> 
    /// it the resulting <see cref="SVersion"/> is not a <see cref="CSVersion"/> or <see cref="SVersion.IsValid"/> is false.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="SVersion.BuildMetaData"/> compliance.</param>
    /// <returns>The CSVersion object.</returns>
    public static CSVersion Parse( string s, bool checkBuildMetaDataSyntax = true )
    {
        SVersion sv = SVersion.TryParse( s, true, checkBuildMetaDataSyntax );
        if( !sv.IsValid ) throw new ArgumentException( sv.ErrorMessage, nameof( s ) );
        return sv as CSVersion ?? throw new ArgumentException( "Not a CSVersion.", nameof( s ) );
    }

}
