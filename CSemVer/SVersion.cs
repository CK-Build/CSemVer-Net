using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CSemVer
{
    /// <summary>
    /// Semantic version implementation.
    /// Strictly conforms to http://semver.org/ v2.0.0 (with a capture of the <see cref="ErrorMessage"/>
    /// when <see cref="IsValid"/> is false) except that the 'v' prefix is allowed and handled transparently.
    /// </summary>
    public class SVersion : IEquatable<SVersion?>, IComparable<SVersion?>
    {
        // This checks a SVersion.
        static readonly Regex _regExSVersion =
            new Regex( @"^v?(?<1>0|[1-9][0-9]*)\.(?<2>0|[1-9][0-9]*)\.(?<3>0|[1-9][0-9]*)(\-(?<4>[0-9A-Za-z\-\.]+))?(\+(?<5>[0-9A-Za-z\-\.]+))?",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );

        // This applies to PreRelease and BuildMetaData.
        static readonly Regex _regexDottedPart =
            new Regex( @"^(?<1>0|[1-9][0-9]*|[0-9A-Za-z\-]+)(\.(?<1>0|[1-9][0-9]*|[0-9A-Za-z\-]+))*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );

        readonly CSVersion? _csVersion;

        /// <summary>
        /// The zero version is "0.0.0-0". It is syntactically valid and 
        /// its precedence is greater than null and lower than any other syntactically valid <see cref="SVersion"/>.
        /// </summary>
        static public readonly SVersion ZeroVersion = new SVersion( null, 0, 0, 0, "0", String.Empty, null );

        /// <summary>
        /// The last SemVer version possible has <see cref="int.MaxValue"/> as its Major, Minor and Patch and has no prerelease.
        /// It is syntactically valid and its precedence is greater than any other <see cref="SVersion"/>.
        /// </summary>
        static public readonly SVersion LastVersion = new SVersion( null, int.MaxValue, int.MaxValue, int.MaxValue, prerelease: String.Empty, buildMetaData: String.Empty, csVersion: null );

        /// <summary>
        /// Protected straight constructor for valid versions.
        /// No checks are done here except that buildMetaData must not start with '+'.
        /// </summary>
        /// <param name="parsedText">The parsed text. Null if not parsed.</param>
        /// <param name="major">The major.</param>
        /// <param name="minor">The minor.</param>
        /// <param name="patch">The patch.</param>
        /// <param name="prerelease">The prerelease. Can be null (normalized to the empty string).</param>
        /// <param name="buildMetaData">The build meta data. Can be null (normalized to the empty string).</param>
        /// <param name="csVersion">Companion CSVersion.</param>
        protected SVersion( string? parsedText, int major, int minor, int patch, string? prerelease, string? buildMetaData, CSVersion? csVersion )
        {
            prerelease ??= String.Empty;
            buildMetaData ??= String.Empty;
            if( buildMetaData.Length > 0 && buildMetaData[0] == '+' ) throw new ArgumentException( "Must not start with '+'.", nameof( buildMetaData ) );
            _csVersion = csVersion ?? (this as CSVersion);
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease;
            BuildMetaData = buildMetaData;
            ParsedText = parsedText;
            NormalizedText = ComputeNormalizedText( major, minor, patch, prerelease, buildMetaData );
        }

        static string ComputeNormalizedText( int major, int minor, int patch, string prerelease, string buildMetaData )
        {
            var t = String.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}", major, minor, patch );
            if( prerelease.Length > 0 ) t += '-' + prerelease;
            if( buildMetaData.Length > 0 ) t += '+' + buildMetaData;
            return t;
        }

        /// <summary>
        /// Initializes a new invalid instance.
        /// </summary>
        /// <param name="error">Error message. Must not be null, empty or whitespace.</param>
        /// <param name="parsedText">Optional parsed text.</param>
        public SVersion( string error, string? parsedText )
        {
            if( String.IsNullOrWhiteSpace( error ) ) throw new ArgumentNullException( nameof( error ) );
            ErrorMessage = error;
            Major = Minor = Patch = -1;
            Prerelease = String.Empty;
            BuildMetaData = String.Empty;
            ParsedText = parsedText;
        }

        /// <summary>
        /// Protected copy constructor with <see cref="BuildMetaData"/>.
        /// </summary>
        /// <param name="other">Origin version.</param>
        /// <param name="buildMetaData">New BuildMetaData. Must not be null.</param>
        /// <param name="csVersion">Companion CSVersion.</param>
        protected SVersion( SVersion other, string buildMetaData, CSVersion? csVersion )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            if( !other.IsValid ) throw new InvalidOperationException( "Version must be valid." );
            _csVersion = csVersion ?? (this as CSVersion);
            Major = other.Major;
            Minor = other.Minor;
            Patch = other.Patch;
            Prerelease = other.Prerelease;
            BuildMetaData = buildMetaData ?? throw new ArgumentNullException( nameof(buildMetaData) );
            NormalizedText = ComputeNormalizedText( Major, Minor, Patch, Prerelease, buildMetaData );
        }

        /// <summary>
        /// Gets the major version.
        /// When <see cref="IsValid"/> is true, necessarily greater or equal to 0, otherwise -1.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version.
        /// When <see cref="IsValid"/> is true, necessarily greater or equal to 0, otherwise -1.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Gets the patch version.
        /// When <see cref="IsValid"/> is true, necessarily greater or equal to 0, otherwise -1.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Gets the prerelease tag without the leading '-'.
        /// Normalized to the empty string when this is a Stable release or when <see cref="IsValid"/> is false.
        /// </summary>
        public string Prerelease { get; }

        /// <summary>
        /// Gets whether this is a prerelease: a prerelease -tag exists.
        /// If this version is valid and is not a prerelease then this is a Stable release.
        /// </summary>
        public bool IsPrerelease => Prerelease.Length > 0;

        /// <summary>
        /// Gets whether this is a Stable release (valid and not a prerelease).
        /// </summary>
        public bool IsStable => IsValid && Prerelease.Length == 0;

        /// <summary>
        /// Gets the build meta data (without the leading '+').
        /// Never null, always normalized to the empty string.
        /// </summary>
        public string BuildMetaData { get; }

        /// <summary>
        /// An error message that describes the error if <see cref="IsValid"/> is false. Null otherwise.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets whether this <see cref="SVersion"/> has a null <see cref="ErrorMessage"/>.
        /// </summary>
        public bool IsValid => ErrorMessage == null;

        /// <summary>
        /// Gets the <see cref="PackageQuality"/> associated to this version.
        /// </summary>
        public PackageQuality PackageQuality
        {
            get
            {
                if( !IsValid ) return PackageQuality.None;
                if( Prerelease.Length > 0 )
                {
                    if( _csVersion != null )
                    {
                        return _csVersion.PrereleaseNameIdx == CSVersion.MaxPreReleaseNameIdx
                                ? PackageQuality.ReleaseCandidate
                                : (_csVersion.PrereleaseNameIdx < CSVersion.MaxPreReleaseNameIdx - 1
                                    ? PackageQuality.Exploratory
                                    : PackageQuality.Preview);
                    }
                    Debug.Assert( CSVersion.StandardPrereleaseNames[0] == "alpha"
                                  && CSVersion.StandardPrereleaseNames[1] == "beta"
                                  && CSVersion.StandardPrereleaseNames[2] == "delta"
                                  && CSVersion.StandardPrereleaseNames[3] == "epsilon"
                                  && CSVersion.StandardPrereleaseNames[4] == "gamma"
                                  && CSVersion.StandardPrereleaseNames[5] == "kappa" );
                    var prerelease = Prerelease;
                    if( prerelease.StartsWith( "alpha", StringComparison.OrdinalIgnoreCase )
                        || prerelease.StartsWith( "beta", StringComparison.OrdinalIgnoreCase )
                        || prerelease.StartsWith( "delta", StringComparison.OrdinalIgnoreCase )
                        || prerelease.StartsWith( "epsilon", StringComparison.OrdinalIgnoreCase )
                        || prerelease.StartsWith( "gamma", StringComparison.OrdinalIgnoreCase )
                        || prerelease.StartsWith( "kappa", StringComparison.OrdinalIgnoreCase ) ) return PackageQuality.Exploratory;
                    if( prerelease.StartsWith( "pre" ) ) return PackageQuality.Preview;
                    if( prerelease.StartsWith( "rc" ) ) return PackageQuality.ReleaseCandidate;
                    return PackageQuality.CI;
                }
                return PackageQuality.Stable;
            }
        }

        /// <summary>
        /// Gets whether this version is the <see cref="ZeroVersion"/> (0.0.0-0).
        /// </summary>
        public bool IsZeroVersion => Major == 0 && Minor == 0 && Patch == 0 && Prerelease == "0";

        /// <summary>
        /// Gets the normalized version as a string.
        /// Null if <see cref="IsValid"/> is false.
        /// </summary>
        public string? NormalizedText { get; }

        /// <summary>
        /// Gets the parsed text. Available even if <see cref="IsValid"/> is false.
        /// It is null if the original parsed string was null or this version has been explicitly created and not parsed.
        /// </summary>
        public string? ParsedText { get; }

        /// <summary>
        /// Gets this <see cref="SVersion"/> as a <see cref="CSVersion"/> if this version happens to
        /// be a valid CSemVer compliant version. Null otherwise.
        /// </summary>
        /// <remarks>
        /// If this version is CSemVer compliant, it can be this object (usual case) or another object
        /// if this SVersion has been built without the CSVersion lookup (parameter handleCSVersion of create or
        /// parse methods sets to false).
        /// </remarks>
        public CSVersion? AsCSVersion => _csVersion;

        /// <summary>
        /// Manages to return the normalized form of this version, whatever it is:
        /// first, on error, returns the <see cref="ErrorMessage"/>, then if <see cref="AsCSVersion"/> is not null
        /// the <see cref="CSVersion.ToString(CSVersionFormat, CIBuildDescriptor)"/> with <see cref="CSVersionFormat.Normalized"/> format 
        /// or fallbacks to the <see cref="NormalizedText"/>.
        /// </summary>
        /// <returns>The normalized version string (always short form).</returns>
        public string ToNormalizedString() => ErrorMessage
                                                ?? _csVersion?.ToString( CSVersionFormat.Normalized )
                                                ?? NormalizedText!;

        /// <summary>
        /// Returns a new <see cref="SVersion"/> with a potentially new <see cref="BuildMetaData"/>.
        /// <see cref="IsValid"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="buildMetaData">The new build meta data or null to remove it.</param>
        /// <returns>The version.</returns>
        public SVersion WithBuildMetaData( string? buildMetaData )
        {
            if( buildMetaData == null ) buildMetaData = String.Empty;
            return buildMetaData == BuildMetaData ? this : DoWithBuildMetaData( buildMetaData );
        }

        /// <summary>
        /// Hidden overridable implementation.
        /// </summary>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <returns>The new version.</returns>
        private protected virtual SVersion DoWithBuildMetaData( string buildMetaData )
        {
            Debug.Assert( buildMetaData != null );
            Debug.Assert( _csVersion != this, "Virtual/override routing did its job." );
            return new SVersion( this, buildMetaData, _csVersion?.WithBuildMetaData( buildMetaData ) );
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SVersion" />.
        /// The created version may not be <see cref="IsValid"/>.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="prerelease">The prerelease version ("alpha", "rc.1.2", etc.).</param>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <param name="handleCSVersion">
        /// False to skip <see cref="CSVersion"/> conformance lookup. The resulting version
        /// will be a <see cref="SVersion"/> even if it is a valid CSemVer pattern.
        /// This should be used in rare scenario where the normalization of a <see cref="CSVersion"/> (standardization
        /// of prerelease names) must not be done.
        /// </param>
        /// <param name="parsedText">Optional parsed text.</param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        /// <returns>The <see cref="SVersion"/>.</returns>
        public static SVersion Create( int major,
                                       int minor,
                                       int patch,
                                       string? prerelease = null,
                                       string? buildMetaData = null,
                                       bool handleCSVersion = true,
                                       bool checkBuildMetaDataSyntax = true,
                                       string? parsedText = null )
        {
            return DoCreate( parsedText, major, minor, patch, prerelease ?? String.Empty, buildMetaData ?? String.Empty, handleCSVersion, checkBuildMetaDataSyntax );
        }

        /// <summary>
        /// Forwards a head if a semantic version is found and returns a <see cref="SVersion"/> that 
        /// may not be <see cref="IsValid"/>. When the returned version is not valid, the head is not forwarded.
        /// </summary>
        /// <param name="s">The parse head.</param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        /// <param name="handleCSVersion">
        /// False to skip <see cref="CSVersion"/> conformance lookup. The resulting version
        /// will be a <see cref="SVersion"/> even if it is a valid <see cref="CSVersion"/>.
        /// This should be used in rare scenario where the normalization of a <see cref="CSVersion"/> (standardization
        /// of prerelease names) must not be done.
        /// </param>
        /// <param name="allowPrefixParse">
        /// When set to true (the default), the parsed string can be longer than the version. Note that on success, the exact
        /// parsed version length is given by the <see cref="SVersion.ParsedText"/>'s length.
        /// </param>
        /// <returns>The SVersion object that may not be <see cref="IsValid"/>.</returns>
        public static SVersion TryParse( ref ReadOnlySpan<char> s, bool handleCSVersion = true, bool checkBuildMetaDataSyntax = true, bool allowPrefixParse = true )
        {
            // Note: Waiting for .Net 5. Regex will support ReadOnlySpan<char> and so this will be the real
            //       implementation and the string version will call it.
            var r = DoTryParse( new string( s ), handleCSVersion, checkBuildMetaDataSyntax, allowPrefixParse );
            if( r.IsValid )
            {
                Debug.Assert( r.ParsedText != null );
                s = s.Slice( r.ParsedText.Length );
            }
            return r;
        }

        /// <summary>
        /// Parses the specified string to a semantic version and returns a <see cref="SVersion"/> that 
        /// may not be <see cref="IsValid"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        /// <param name="handleCSVersion">
        /// False to skip <see cref="CSVersion"/> conformance lookup. The resulting version
        /// will be a <see cref="SVersion"/> even if it is a valid <see cref="CSVersion"/>.
        /// This should be used in rare scenario where the normalization of a <see cref="CSVersion"/> (standardization
        /// of prerelease names) must not be done.
        /// </param>
        /// <returns>The SVersion object that may not be <see cref="IsValid"/>.</returns>
        public static SVersion TryParse( string s, bool handleCSVersion = true, bool checkBuildMetaDataSyntax = true )
        {
            if( string.IsNullOrEmpty( s ) ) return new SVersion( "Null or empty version string.", s );
            return DoTryParse( s, handleCSVersion, checkBuildMetaDataSyntax, allowPrefixParse: false );
        }

        static SVersion DoTryParse( string s, bool handleCSVersion, bool checkBuildMetaDataSyntax, bool allowPrefixParse )
        {
            Match m = _regExSVersion.Match( s );
            // On success, the actual parsed length is the length of the parsed text.
            bool isLonger;
            if( !m.Success || ((isLonger = s.Length > m.Length) && !allowPrefixParse) ) return new SVersion( m.Success ? "Unexpected characters after version." : "Pattern not matched.", s );
            if( isLonger ) s = s.Substring( 0, m.Length );
            string sMajor = m.Groups[1].Value;
            string sMinor = m.Groups[2].Value;
            string sPatch = m.Groups[3].Value;
            if( !int.TryParse( sMajor, out int major ) ) return new SVersion( "Invalid Major.", s );
            if( !int.TryParse( sMinor, out int minor ) ) return new SVersion( "Invalid Major.", s );
            if( !int.TryParse( sPatch, out int patch ) ) return new SVersion( "Invalid Patch.", s );
            return DoCreate( s, major, minor, patch, m.Groups[4].Value, m.Groups[5].Value, handleCSVersion, checkBuildMetaDataSyntax );
        }

        /// <summary>
        /// Standard TryParse pattern that returns a boolean rather than the resulting <see cref="SVersion"/>.
        /// See <see cref="TryParse(string,bool,bool)"/>.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="v">Resulting version.</param>
        /// <param name="handleCSVersion">
        /// False to skip <see cref="CSVersion"/> conformance lookup. The resulting version
        /// will be a <see cref="SVersion"/> even if it is a valid <see cref="CSVersion"/>.
        /// This should be used in rare scenario where the normalization of a <see cref="CSVersion"/> (standardization
        /// of prerelease names) must not be done.
        /// </param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParse( string s, out SVersion v, bool handleCSVersion = true, bool checkBuildMetaDataSyntax = true )
        {
            v = TryParse( s, handleCSVersion, checkBuildMetaDataSyntax );
            return v.IsValid;
        }


        /// <summary>
        /// Parses the specified string to a semantic version and throws an <see cref="ArgumentException"/> 
        /// it the resulting <see cref="IsValid"/> is false.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="handleCSVersion">
        /// False to skip <see cref="CSVersion"/> conformance lookup. The resulting version
        /// will be a <see cref="SVersion"/> even if it is a valid <see cref="CSVersion"/>.
        /// This should be used in rare scenario where the normalization of a <see cref="CSVersion"/> (standardization
        /// of prerelease names) must not be done.
        /// </param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        /// <returns>The SVersion object.</returns>
        public static SVersion Parse( string s, bool handleCSVersion = true, bool checkBuildMetaDataSyntax = true )
        {
            var v = TryParse( s, handleCSVersion, checkBuildMetaDataSyntax );
            if( !v.IsValid ) throw new ArgumentException( v.ErrorMessage, nameof( s ) );
            return v;
        }

        static SVersion DoCreate( string? parsedText, int major, int minor, int patch, string prerelease, string buildMetaData, bool handleCSVersion, bool checkBuildMetaDataSyntax )
        {
            Debug.Assert( prerelease != null && buildMetaData != null );
            if( major < 0 || minor < 0 || patch < 0 ) return new SVersion( "Major, minor and patch must positive or 0.", parsedText );

            if( buildMetaData.Length > 0 && checkBuildMetaDataSyntax )
            {
                var error = ValidateDottedIdentifiers( buildMetaData, "build metadata" );
                if( error != null ) return new SVersion( error, parsedText );
            }
            // Try CSVersion first.
            CSVersion? c = CSVersion.FromSVersion( parsedText, major, minor, patch, prerelease, buildMetaData );
            if( handleCSVersion && c != null ) return c;
            // If it is not a CSVersion, validate the prerelease.
            // A Stable is not necessarily a CSVersion (too big Major/Minor/Patch).
            if( c == null && prerelease.Length > 0 )
            {
                var error = ValidateDottedIdentifiers( prerelease, "pre-release" );
                if( error != null ) return new SVersion( error, parsedText );
            }
            return new SVersion( parsedText, major, minor, patch, prerelease, buildMetaData, c );
        }

        static string? ValidateDottedIdentifiers( string s, string partName )
        {
            Match m = _regexDottedPart.Match( s );
            if( !m.Success ) return "Invalid " + partName;
            else
            {
                CaptureCollection captures = m.Groups[1].Captures;
                Debug.Assert( captures.Count > 0 );
                foreach( Capture id in captures )
                {
                    Debug.Assert( id.Value.Length > 0 );
                    string p = id.Value;
                    if( p.Length > 1 && p[0] == '0' )
                    {
                        int i = 1;
                        while( i < p.Length )
                        {
                            if( !Char.IsDigit( p, i++ ) ) break;
                        }
                        if( i == p.Length )
                        {
                            return $"Numeric identifiers in {partName} must not start with a 0.";
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Overridden to return the <see cref="ErrorMessage"/> if not null or the <see cref="NormalizedText"/>.
        /// </summary>
        /// <returns>The textual representation.</returns>
        public override string ToString() => ErrorMessage ?? NormalizedText!;

        /// <summary>
        /// Gets the standard Informational version string.
        /// If <see cref="SVersion.IsValid"/> is false this throws an <see cref="InvalidOperationException"/>: 
        /// the constant <see cref="InformationalVersion.ZeroInformationalVersion"/> should be used when IsValid is false.
        /// </summary>
        /// <param name="commitSha">The SHA1 of the commit (must be 40 hex digits).</param>
        /// <param name="commitDateUtc">The commit date (must be in UTC).</param>
        /// <returns>The informational version.</returns>
        public string GetInformationalVersion( string commitSha, DateTime commitDateUtc )
        {
            if( !IsValid ) throw new InvalidOperationException( "IsValid must be true. Use InformationalVersion.ZeroInformationalVersion when IsValid is false." );
            return InformationalVersion.BuildInformationalVersion( this, commitSha, commitDateUtc );
        }

        /// <summary>
        /// Compares this with another <see cref="SVersion"/>.
        /// Null is lower than any version. An invalid version is lower than any valid version.
        /// This (and the overloaded comparison operators) compares Semantic Version form (the <see cref="NormalizedText"/>)
        /// according to the SemVer 2.0 specification.
        /// Use <see cref="CSemVerCompareTo"/> to compare 2 versions according to CSemVer rules: "1.0.0-a" is equal
        /// to "1.0.0-alpha".
        /// </summary>
        /// <param name="other">
        /// The other version to compare with this instance. Can be null (null is lower than any version).
        /// </param>
        /// <returns>Standard positive, negative or zero value.</returns>
        public int CompareTo( SVersion? other )
        {
            if( other is null ) return 1;
            if( IsValid )
            {
                if( !other.IsValid ) return 1;
            }
            else if( other.IsValid ) return -1;
            return CompareValid( other );
        }

        int CompareValid( SVersion other )
        {
            var r = Major - other.Major;
            if( r != 0 ) return r;

            r = Minor - other.Minor;
            if( r != 0 ) return r;

            r = Patch - other.Patch;
            if( r != 0 ) return r;

            return ComparePreRelease( Prerelease, other.Prerelease );
        }

        /// <summary>
        /// Compares this with another <see cref="SVersion"/>, handling potential <see cref="CSVersion"/>.
        /// Note that as with <see cref="CompareTo"/>, null is lower than any version and an invalid version is lower than any valid version.
        /// With this comparison method, "1.0.0-a" is equal to "1.0.0-alpha".
        /// When this version or the other one is NOT a CSemVer version, the long form (<see cref="CSVersionFormat.Normalized"/>) is used
        /// by default.
        /// </summary>
        /// <param name="other">The other version to compare to (that may be a <see cref="CSVersion"/>).</param>
        /// <param name="useShortForm">
        /// By default, long form is considered when an actual CSVersion must be compared to a
        /// mere SVersion. Use false to consider the long form instead.
        /// </param>
        /// <returns>Standard positive, negative or zero value.</returns>
        public int CSemVerCompareTo( SVersion? other, bool useShortForm = true )
        {
            if( other is null ) return 1;
            if( IsValid )
            {
                if( !other.IsValid ) return 1;
            }
            else if( other.IsValid ) return -1;
            if( AsCSVersion != null )
            {
                if( other.AsCSVersion != null )
                {
                    return AsCSVersion.OrderedVersion.CompareTo( other.AsCSVersion.OrderedVersion );
                }
                if( !useShortForm )
                {
                    var vLong = Parse( AsCSVersion.ToString( CSVersionFormat.Normalized ), handleCSVersion: false );
                    return vLong.CompareValid( other );
                }
            }
            else if( other.AsCSVersion != null && !useShortForm )
            {
                var vLong = Parse( other.AsCSVersion.ToString( CSVersionFormat.Normalized ), handleCSVersion: false );
                return -vLong.CompareValid( other );
            }
            return CompareValid( other );
        }

        /// <summary>
        /// Compares <paramref name="x"/> with <paramref name="y"/>, allowing null on both sides.
        /// Null is lower than any version. An invalid version is lower than any valid version.
        /// See <see cref="CompareTo(SVersion)"/>.
        /// </summary>
        /// <param name="x">The left version to compare. Can be null.</param>
        /// <param name="y">The right version to compare. Can be null.</param>
        /// <returns>Standard positive, negative or zero value.</returns>
        static public int SafeCompare( SVersion? x, SVersion? y )
        {
            if( x is null )
            {
                return y is null ? 0 : -1;
            }
            return x.CompareTo( y );
        }

        /// <summary>
        /// Compares <paramref name="x"/> with <paramref name="y"/> like <see cref="CSemVerCompareTo(SVersion, bool)"/>,
        /// allowing null on both sides.
        /// Null is lower than any version. An invalid version is lower than any valid version.
        /// </summary>
        /// <param name="x">The left version to compare. Can be null.</param>
        /// <param name="y">The right version to compare. Can be null.</param>
        /// <param name="useShortForm">
        /// By default, short form is considered when an actual CSVersion must be compared to a
        /// mere SVersion. Use false to consider the long form instead.
        /// </param>
        /// <returns>Standard positive, negative or zero value.</returns>
        static public int CSemVerSafeCompare( SVersion? x, SVersion? y, bool useShortForm = true )
        {
            if( x is null )
            {
                return y is null ? 0 : -1;
            }
            return x.CSemVerCompareTo( y, useShortForm );
        }

        // Fun with Span and allocation-free string parsing.
        // Using this https://github.com/dotnet/runtime/pull/295 (not yet available)
        // would require to change the algorithm since we need to know the number of
        // split parts: we stack allocate a big enough array of Range and fills it
        // with the split parts.
        // The magic is: the algorithm is the same as the one with the strings!
        static int ComparePreRelease( ReadOnlySpan<char> x, ReadOnlySpan<char> y )
        {
            if( x.Length == 0 ) return y.Length == 0 ? 0 : 1;
            if( y.Length == 0 ) return -1;

            static Span<Range> StackSplit( ReadOnlySpan<char> x, Span<Range> store )
            {
                int count = 0, offset = 0, index = 0;
                do
                {
                    var next = x.Slice( offset );
                    var nextIdx = next.IndexOf( '.' );
                    index = nextIdx != -1 ? nextIdx : next.Length;
                    store[count++] = new Range( offset, offset += index++ );
                }
                while( ++offset < x.Length );
                return store.Slice( 0, count );
            }

            // var xParts = x.Split( '.' );
            // var yParts = y.Split( '.' );
            // ==> StackSplit replaces the string.Split() method.
            var xParts = StackSplit( x, stackalloc Range[1 + x.Length >> 1] );
            var yParts = StackSplit( y, stackalloc Range[1 + y.Length >> 1] );

            int commonParts = xParts.Length;
            int ultimateResult = -1;
            if( yParts.Length < xParts.Length )
            {
                commonParts = yParts.Length;
                ultimateResult = 1;
            }
            else if( yParts.Length == xParts.Length )
            {
                ultimateResult = 0;
            }
            for( int i = 0; i < commonParts; i++ )
            {
                // var xP = xParts[i];
                // var yP = yParts[i];
                // ==> Use the Range indexing to obtain the parts as ReadOnlySpan<char>/
                var xP = x[xParts[i]];
                var yP = y[yParts[i]];
                int r;
                if( int.TryParse( xP, out int xN ) )
                {
                    if( int.TryParse( yP, out int yN ) )
                    {
                        r = xN - yN;
                        if( r != 0 ) return r;
                    }
                    else return -1;
                }
                else
                {
                    if( int.TryParse( yP, out _ ) ) return 1;
                    // r = StringComparer.OrdinalIgnoreCase.Compare( xP, yP );
                    // ==> Replace the call to OrdinalIgnoreCase.Compare by the convenient
                    //     extension method on ReadOnlySpan<char>.
                    r = xP.CompareTo( yP, StringComparison.OrdinalIgnoreCase );
                    if( r != 0 ) return r;
                }
            }
            return ultimateResult;
        }

        /// <summary>
        /// Equality ignore this <see cref="BuildMetaData"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals( object? obj )
        {
            if( obj is null ) return false;
            if( ReferenceEquals( this, obj ) ) return true;
            return Equals( obj as SVersion );
        }

        /// <summary>
        /// Returns a hash code that ignores the <see cref="BuildMetaData"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Major * 31 + Minor) * 31 + Patch) * 31 + Prerelease.GetHashCode();
            }
        }

        /// <summary>
        /// Versions are equal if and only if <see cref="IsValid"/>, <see cref="Major"/>, <see cref="Minor"/>,
        /// <see cref="Patch"/> and <see cref="Prerelease"/> are equals. <see cref="BuildMetaData"/> is ignored.
        /// No other members are used for equality and comparison.
        /// </summary>
        /// <param name="other">Other version.</param>
        /// <returns>True if they are the same regardless of <see cref="BuildMetaData"/>.</returns>
        public bool Equals( SVersion? other )
        {
            if( other == null ) return false;
            if( ReferenceEquals( this, other ) ) return true;
            if( IsValid )
            {
                if( !other.IsValid ) return false;
            }
            else if( other.IsValid ) return false;
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   Prerelease == other.Prerelease;
        }

        /// <summary>
        /// Implements == operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if they are equal.</returns>
        static public bool operator ==( SVersion? x, SVersion? y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( x is object )
            {
                return x.Equals( y );
            }
            return false;
        }

        /// <summary>
        /// Implements &gt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is greater than y.</returns>
        static public bool operator >( SVersion? x, SVersion? y )
        {
            if( ReferenceEquals( x, y ) ) return false;
            if( x is object )
            {
                if( y is null ) return true;
                return x.CompareTo( y ) > 0;
            }
            return false;
        }

        /// <summary>
        /// Implements &lt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than y.</returns>
        static public bool operator >=( SVersion? x, SVersion? y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( x is object )
            {
                if( y is null ) return true;
                return x.CompareTo( y ) >= 0;
            }
            return false;
        }

        /// <summary>
        /// Implements != operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if they are not equal.</returns>
        static public bool operator !=( SVersion? x, SVersion? y ) => !(x == y);

        /// <summary>
        /// Implements &lt;= operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than or equal to y.</returns>
        static public bool operator <=( SVersion? x, SVersion? y ) => !(x > y);

        /// <summary>
        /// Implements &lt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than y.</returns>
        static public bool operator <( SVersion? x, SVersion? y ) => !(x >= y);
    }
}

