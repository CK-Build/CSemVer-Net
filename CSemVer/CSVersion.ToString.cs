using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CSemVer
{
    public sealed partial class CSVersion
    {
        string _cacheLongForm;

        /// <summary>
        /// Gets this version in a <see cref="CSVersionFormat.FileVersion"/> format.
        /// </summary>
        /// <param name="isCIBuild">True to indicate a CI build: the revision part (last part) is odd.</param>
        /// <returns>The Major.Minor.Build.Revision number where each part are between 0 and 65535.</returns>
        public string ToStringFileVersion( bool isCIBuild )
        {
            SOrderedVersion v = _orderedVersion;
            v.Number <<= 1;
            if( isCIBuild ) v.Revision |= 1;
            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision );
        }

        /// <summary>
        /// Gets the string version in the given format.
        /// Returns the <see cref="SVersion.ErrorMessage"/> if it is not null.
        /// </summary>
        /// <param name="f">Format to use.</param>
        /// <param name="buildInfo">Not null to generate a post-release version.</param>
        /// <returns>Formated string (or <see cref="SVersion.ErrorMessage"/> if any).</returns>
        public string ToString( CSVersionFormat f, CIBuildDescriptor buildInfo = null )
        {
            if( ErrorMessage != null ) return ErrorMessage;
            if( buildInfo != null && !buildInfo.IsValid ) throw new ArgumentException( "buildInfo, when not null, must be valid." );
            // Fast path and cache for format with no build info.
            if( buildInfo == null )
            {
                if( f == CSVersionFormat.Normalized )
                {
                    return NormalizedText;
                }
                if( f == CSVersionFormat.LongForm )
                {
                    if( _cacheLongForm == null )
                    {
                        _cacheLongForm = ComputeLongFormVersion( Major, Minor, Patch, PrereleaseName, PrereleaseNumber, PrereleasePatch, String.Empty, null );
                    }
                    return _cacheLongForm;
                }
            }
            if( f == CSVersionFormat.FileVersion )
            {
                return ToStringFileVersion( buildInfo != null );
            }

            if( f == CSVersionFormat.LongForm || f == CSVersionFormat.LongFormWithBuildMetaData )
            {
                string suffix = f == CSVersionFormat.LongFormWithBuildMetaData && BuildMetaData.Length > 0
                                        ? "+" + BuildMetaData
                                        : String.Empty;
                return ComputeLongFormVersion( Major, Minor, Patch, PrereleaseName, PrereleaseNumber, PrereleasePatch, suffix, buildInfo );
            }
            else
            {
                Debug.Assert( f == CSVersionFormat.Normalized || f == CSVersionFormat.NormalizedWithBuildMetaData );
                string suffix = f == CSVersionFormat.NormalizedWithBuildMetaData && BuildMetaData.Length > 0
                                        ? "+" + BuildMetaData
                                        : String.Empty;
                return ComputeShortFormVersion( Major, Minor, Patch, PrereleaseNameIdx, PrereleaseNumber, PrereleasePatch, suffix, buildInfo );
            }
        }

        static string ComputeStandardPreRelease( int preReleaseNameIdx, int preReleaseNumber, int preReleasePatch )
        {
            Debug.Assert( preReleaseNameIdx >= -1 );
            Debug.Assert( preReleaseNumber >= 0 && preReleaseNumber <= MaxPreReleaseNumber );
            Debug.Assert( preReleasePatch >= 0 && preReleasePatch <= MaxPreReleaseNumber );
            if( preReleaseNameIdx == -1 ) return String.Empty;
            if( preReleasePatch > 0 )
            {
                return String.Format( "{0}{1:00}-{2:00}", _standardNamesI[preReleaseNameIdx], preReleaseNumber, preReleasePatch );
            }
            else if( preReleaseNumber > 0 )
            {
                return String.Format( "{0}{1:00}", _standardNamesI[preReleaseNameIdx], preReleaseNumber );
            }
            return _standardNamesI[preReleaseNameIdx];
        }

        static string ComputeLongFormVersion( int major, int minor, int patch, string prereleaseName, int preReleaseNumber, int preReleasePatch, string suffix, CIBuildDescriptor buildInfo = null )
        {
            if( buildInfo != null )
            {
                suffix = buildInfo.ToString() + suffix;
            }
            if( prereleaseName.Length > 0 )
            {
                if( preReleasePatch > 0 )
                {
                    if( buildInfo != null )
                    {
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}.{6}", major, minor, patch, prereleaseName, preReleaseNumber, preReleasePatch, suffix );
                    }
                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}{6}", major, minor, patch, prereleaseName, preReleaseNumber, preReleasePatch, suffix );
                }
                if( preReleaseNumber > 0 )
                {
                    if( buildInfo != null )
                    {
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.0.{5}", major, minor, patch, prereleaseName, preReleaseNumber, suffix );
                    }
                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}{5}", major, minor, patch, prereleaseName, preReleaseNumber, suffix );
                }
                if( buildInfo != null )
                {
                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.0.0.{4}", major, minor, patch, prereleaseName, suffix );
                }
                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4}", major, minor, patch, prereleaseName, suffix );
            }
            if( buildInfo != null )
            {
                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}--{3}", major, minor, patch + 1, suffix );
            }
            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}{3}", major, minor, patch, suffix );
        }

        static string ComputeShortFormVersion( int major, int minor, int patch, int preReleaseNameIdx, int preReleaseNumber, int preReleasePatch, string suffix, CIBuildDescriptor buildInfo = null )
        {
            if( buildInfo != null )
            {
                if( !buildInfo.IsValidForShortForm ) throw new ArgumentException( "buildInfo must be valid for ShortForm format." );
                suffix = buildInfo.ToStringForShortForm() + suffix;
            }
            if( preReleaseNameIdx >= 0 )
            {
                string prName = _standardNamesI[preReleaseNameIdx];
                if( preReleasePatch > 0 )
                {
                    if( buildInfo != null )
                    {
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}-{5:00}-{6}", major, minor, patch, prName, preReleaseNumber, preReleasePatch, suffix );
                    }
                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}-{5:00}{6}", major, minor, patch, prName, preReleaseNumber, preReleasePatch, suffix );
                }
                if( preReleaseNumber > 0 )
                {
                    if( buildInfo != null )
                    {
                        return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}-00-{5}", major, minor, patch, prName, preReleaseNumber, suffix );
                    }
                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4:00}{5}", major, minor, patch, prName, preReleaseNumber, suffix );
                }
                if( buildInfo != null )
                {
                    return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}00-00-{4}", major, minor, patch, prName, suffix );
                }
                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}{4}", major, minor, patch, prName, suffix );
            }
            if( buildInfo != null )
            {
                return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}--{3}", major, minor, patch + 1, suffix );
            }
            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}{3}", major, minor, patch, suffix );
        }

        /// <summary>
        /// Gets the standard Informational version string.
        /// If <see cref="SVersion.IsValid"/> is false this throws an <see cref="InvalidOperationException"/>: 
        /// the constant <see cref="InformationalVersion.ZeroInformationalVersion"/> should be used when IsValid is false.
        /// </summary>
        /// <param name="commitSha">The SHA1 of the commit (must be 40 hex digits).</param>
        /// <param name="commitDateUtc">The commit date (must be in UTC).</param>
        /// <param name="buildInfo">
        /// Not null for post-release version. 
        /// <see cref="CIBuildDescriptor.IsValid"/> and <see cref="CIBuildDescriptor.IsValidForShortForm"/> must be true.
        /// </param>
        /// <returns>The informational version.</returns>
        public string GetInformationalVersion( string commitSha, DateTime commitDateUtc, CIBuildDescriptor buildInfo = null )
        {
            if( !IsValid ) throw new InvalidOperationException( "IsValid must be true. Use CSVersion.InvalidInformationalVersion when IsValid is false." );
            var semVer = ToString( CSVersionFormat.LongForm, buildInfo );
            var shortVer = ToString( CSVersionFormat.Normalized, buildInfo );
            return InformationalVersion.BuildInformationalVersion( semVer, shortVer, commitSha, commitDateUtc );
        }
    }
}


