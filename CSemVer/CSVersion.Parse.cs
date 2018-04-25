using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CSemVer
{
    public sealed partial class CSVersion
    {
        internal static CSVersion FromSVersion( string parsedText, int major, int minor, int patch, string prerelease, string metadata )
        {
            Debug.Assert( prerelease != null && metadata != null );
            if( major > MaxMajor || minor > MaxMinor || patch > MaxPatch ) return null;
            var error = ParsePreRelease( prerelease, out string prName, out int prNameIdx, out int prNum, out int prPatch );
            if( error != null ) return null;
            return new CSVersion( parsedText, major, minor, patch, ComputeStandardPreRelease( prNameIdx, prNum, prPatch ), metadata, prName, prNameIdx, prNum, prPatch );
        }

        static string ParsePreRelease( string prerelease, out string prName, out int prNameIdx, out int prNum, out int prPatch )
        {
            Debug.Assert( prerelease != null );
            prName = String.Empty;
            prNameIdx = -1;
            prNum = 0;
            prPatch = 0;
            if( prerelease.Length > 0 )
            {
                bool shortForm = false;
                Match m = _rPreReleaseLongForm.Match( prerelease );
                if( !m.Success )
                {
                    shortForm = true;
                    m = _rPreReleaseShortForm.Match( prerelease );
                    if( !m.Success ) return "Not a CSVersion prerelease syntax.";
                }
                prName = m.Groups[1].Value;
                prNameIdx = GetPreReleaseNameIdx( prName, shortForm );
                string sPRNum = m.Groups[2].Value;
                string sPRFix = m.Groups[3].Value;
                if( sPRFix.Length > 0 ) prPatch = Int32.Parse( sPRFix );
                if( sPRNum.Length > 0 ) prNum = Int32.Parse( sPRNum );
                if( prPatch == 0 && prNum == 0 && sPRNum.Length > 0 ) return String.Format( "Incorrect '.0' Release Number version. 0 can appear only to fix the first pre release (ie. '.0.F' where F is between 1 and {0}).", MaxPreReleasePatch );
            }
            return null;
        }

        /// <summary>
        /// Computes the pre release name index ('alpha' is 0, 'rc' is <see cref="MaxPreReleaseNameIdx"/>).
        /// This is -1 if the pre release name is null or empty (no pre release name defines a final release).
        /// The lookup into <see cref="StandardPrereleaseNames"/> or <see cref="StandardPreReleaseNamesShort"/> is
        /// case sensitive.
        /// Any unmatched pre release name is <see cref="MaxPreReleaseNameIdx"/> - 1 ('prerelease', the last one before 'rc').
        /// </summary>
        /// <param name="parsedPrereleaseName">Pre release name.</param>
        /// <returns>Index between -1 (release) and MaxPreReleaseNameIdx.</returns>
        public static int GetPreReleaseNameIdx( string parsedPrereleaseName ) => GetPreReleaseNameIdx( parsedPrereleaseName, false );

        static int GetPreReleaseNameIdx( string parsedPrereleaseName, bool shortForm )
        {
            if( parsedPrereleaseName == null || parsedPrereleaseName.Length == 0 ) return -1;
            int prNameIdx = Array.IndexOf( shortForm ? _standardNamesI : _standardNames, parsedPrereleaseName );
            if( prNameIdx < 0 )
            {
                if( !shortForm ) prNameIdx = Array.IndexOf( _standardNamesI, parsedPrereleaseName );
                if( prNameIdx < 0 )
                {
                    prNameIdx = MaxPreReleaseNameIdx - 1;
                }
            }
            return prNameIdx;
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
            if( !sv.IsValid ) new CSVersion( sv.ErrorMessage, s );
            return new CSVersion( "Not a CSVersion.", s );
        }

        /// <summary>
        /// Standard TryParse pattern that returns a boolean rather than the resulting <see cref="CSVersion"/>.
        /// See <see cref="TryParse(string,bool)"/>.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="v">Resulting version.</param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="SVersion.BuildMetaData"/> compliance.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParse( string s, out CSVersion v, bool checkBuildMetaDataSyntax = true )
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
            CSVersion v = sv as CSVersion;
            if( v == null ) throw new ArgumentException( "Not a CSVersion.", nameof( s ) );
            return v;
        }

    }
}
