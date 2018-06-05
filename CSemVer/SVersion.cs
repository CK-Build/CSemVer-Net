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
    public class SVersion : IEquatable<SVersion>, IComparable<SVersion>
    {
        // This checks a SVersion.
        static Regex _regExSVersion =
            new Regex( @"^v?(?<1>0|[1-9][0-9]*)\.(?<2>0|[1-9][0-9]*)\.(?<3>0|[1-9][0-9]*)(\-(?<4>[0-9A-Za-z\-\.]+))?(\+(?<5>[0-9A-Za-z\-\.]+))?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );

        // This applies to PreRelease and BuildMetaData.
        static Regex _regexDottedPart =
            new Regex( @"^(?<1>0|[1-9][0-9]*|[0-9A-Za-z\-]+)(\.(?<1>0|[1-9][0-9]*|[0-9A-Za-z\-]+))*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );

        readonly CSVersion _csVersion;

        /// <summary>
        /// The zero version is "0.0.0-0". It is syntaxically valid and 
        /// its precedence is greater than null and lower than any other syntaxically valid <see cref="SVersion"/>.
        /// </summary>
        static public readonly SVersion ZeroVersion = new SVersion( null, 0, 0, 0, "0", String.Empty, null );

        /// <summary>
        /// Protected straight constructor for valid versions.
        /// No checks are done here.
        /// </summary>
        /// <param name="parsedText">The parsed text. Null if not parsed.</param>
        /// <param name="major">The major.</param>
        /// <param name="minor">The minor.</param>
        /// <param name="patch">The patch.</param>
        /// <param name="prerelease">The prerelease. Can be null (normalized to the empty string).</param>
        /// <param name="buildMetaData">The build meta data. Can be null (normalized to the empty string).</param>
        /// <param name="csVersion">Companion CSVersion.</param>
        protected SVersion( string parsedText, int major, int minor, int patch, string prerelease, string buildMetaData, CSVersion csVersion )
        {
            if( buildMetaData == null ) buildMetaData = String.Empty;
            if( buildMetaData.Length > 0 && buildMetaData[0] == '+' ) throw new ArgumentException( "Must not start with '+'.", nameof( buildMetaData ) );
            _csVersion = csVersion ?? (this as CSVersion);
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease ?? String.Empty;
            BuildMetaData = buildMetaData;
            ParsedText = parsedText;
            NormalizedText = ComputeNormalizedText( major, minor, patch, prerelease );
            NormalizedTextWithBuildMetaData = buildMetaData.Length > 0 ? NormalizedText + '+' + buildMetaData : NormalizedText;
        }

        static string ComputeNormalizedText( int major, int minor, int patch, string prerelease )
        {
            var t = String.Format( CultureInfo.InvariantCulture, "{0}.{1}.{2}", major, minor, patch );
            if( prerelease.Length > 0 ) t += '-' + prerelease;
            return t;
        }

        /// <summary>
        /// Protected constructor for invalid constructor.
        /// </summary>
        /// <param name="error">Error message. Must not be null nor empty.</param>
        /// <param name="parsedText">Can be null.</param>
        protected SVersion( string error, string parsedText )
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
        protected SVersion( SVersion other, string buildMetaData, CSVersion csVersion )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            if( buildMetaData == null ) throw new ArgumentNullException( nameof(buildMetaData) );
            if( !other.IsValid ) throw new InvalidOperationException( "Version must be valid." );
            _csVersion = csVersion ?? (this as CSVersion);
            Major = other.Major;
            Minor = other.Minor;
            Patch = other.Patch;
            Prerelease = other.Prerelease;
            BuildMetaData = buildMetaData;
            NormalizedText = ComputeNormalizedText( Major, Minor, Patch, Prerelease );
            NormalizedTextWithBuildMetaData = buildMetaData.Length > 0 ? NormalizedText + '+' + buildMetaData : NormalizedText;
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
        /// Gets the pre-release version.
        /// Normalized to the empty string when this is an Official release or when <see cref="IsValid"/> is false.
        /// </summary>
        public string Prerelease { get; }

        /// <summary>
        /// Gets the build meta data (without the leading '+').
        /// Never null, always normalized to the empty string.
        /// </summary>
        public string BuildMetaData { get; }

        /// <summary>
        /// An error message that describes the error if <see cref="IsValid"/> is false. Null otherwise.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets whether this <see cref="SVersion"/> has no <see cref="ErrorMessage"/>.
        /// </summary>
        public bool IsValid => ErrorMessage == null;

        /// <summary>
        /// Gets whether this version is a <see cref="ZeroVersion"/>.
        /// </summary>
        public bool IsZeroVersion => Major == 0 && Minor == 0 && Patch == 0 && Prerelease == "0";

        /// <summary>
        /// Gets the normalized version as a string. It does not contain the <see cref="BuildMetaData"/>.
        /// Null if <see cref="IsValid"/> is false.
        /// </summary>
        public string NormalizedText { get; }

        /// <summary>
        /// Gets the normalized version as a string, including the +<see cref="BuildMetaData"/>.
        /// Null if <see cref="IsValid"/> is false.
        /// </summary>
        public string NormalizedTextWithBuildMetaData { get; }

        /// <summary>
        /// Gets the parsed text. Available even if <see cref="IsValid"/> is false.
        /// It is null if the original parsed string was null or this version has been explicitely created and not parsed.
        /// </summary>
        public string ParsedText { get; }

        /// <summary>
        /// Gets this <see cref="SVersion"/> as a <see cref="CSVersion"/> if the version happens to
        /// be a valid CSemVer compliant version. Null otherwise.
        /// </summary>
        /// <remarks>
        /// If this version is CSemVer compliant, it can be this object (usual case) or another object
        /// if this SVersion has been built without the CSVersion lookup (parameter handleCSVersion of create or
        /// parse methods sets to false).
        /// </remarks>
        public CSVersion AsCSVersion => _csVersion;

        /// <summary>
        /// Returns a new <see cref="SVersion"/> with a potentially new <see cref="BuildMetaData"/>.
        /// <see cref="IsValid"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <returns>The version.</returns>
        public SVersion WithBuildMetaData( string buildMetaData )
        {
            if( buildMetaData == null ) buildMetaData = String.Empty;
            return buildMetaData == BuildMetaData ? this : DoWithBuildMetaData( buildMetaData );
        }

        /// <summary>
        /// Hidden overridable implementation.
        /// </summary>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <returns>The new version.</returns>
        protected virtual SVersion DoWithBuildMetaData( string buildMetaData )
        {
            Debug.Assert( buildMetaData != null );
            Debug.Assert( _csVersion != this, "Virtual/override routing did its job." );
            return new SVersion( this, buildMetaData, _csVersion != null ? _csVersion.WithBuildMetaData( buildMetaData ) : null );
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
        /// will be a <see cref="SVersion"/> even if it is a valid <see cref="CSVersion"/>.
        /// This should be used in rare scenario where the normalization of a <see cref="CSVersion"/> (standardization
        /// of prerelease names) must not be done.
        /// </param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        /// <returns>The <see cref="SVersion"/>.</returns>
        public static SVersion Create( int major, int minor, int patch, string prerelease = null, string buildMetaData = null, bool handleCSVersion = true, bool checkBuildMetaDataSyntax = true )
        {
            return DoCreate( null, major, minor, patch, prerelease ?? String.Empty, buildMetaData ?? String.Empty, handleCSVersion, checkBuildMetaDataSyntax );
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
            Match m = _regExSVersion.Match( s );
            if( !m.Success ) return new SVersion( "Pattern not matched.", s );
            string sMajor = m.Groups[1].Value;
            string sMinor = m.Groups[2].Value;
            string sPatch = m.Groups[3].Value;
            int major, minor, patch;
            if( !int.TryParse( sMajor, out major ) ) return new SVersion( "Invalid Major.", s );
            if( !int.TryParse( sMinor, out minor ) ) return new SVersion( "Invalid Major.", s );
            if( !int.TryParse( sPatch, out patch ) ) return new SVersion( "Invalid Patch.", s );
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

        static SVersion DoCreate( string parsedText, int major, int minor, int patch, string prerelease, string buildMetaData, bool handleCSVersion, bool checkBuildMetaDataSyntax )
        {
            Debug.Assert( prerelease != null && buildMetaData != null );
            if( major < 0 || minor < 0 || patch < 0 ) return new SVersion( "Major, minor and patch must positive or 0.", parsedText );

            if( buildMetaData.Length > 0 && checkBuildMetaDataSyntax )
            {
                var error = ValidateDottedIdentifiers( buildMetaData, "build metadata" );
                if( error != null ) return new SVersion( error, parsedText );
            }
            // Try CSVersion first.
            CSVersion c = CSVersion.FromSVersion( parsedText, major, minor, patch, prerelease, buildMetaData );
            if( handleCSVersion  && c != null ) return c;
            // If it is not a CSVersion, validate the prerelease.
            // An OfficialVersion is not necessarily a CSVersion (too big Major/Minor/Patch).
            if( c == null && prerelease.Length > 0 )
            {
                var error = ValidateDottedIdentifiers( prerelease, "pre-release" );
                if( error != null ) return new SVersion( error, parsedText );
            }
            return new SVersion( parsedText, major, minor, patch, prerelease, buildMetaData, c );
        }

        static string ValidateDottedIdentifiers( string s, string partName )
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
        public override string ToString() => ErrorMessage ?? NormalizedText;

        /// <summary>
        /// Returns the <see cref="ErrorMessage"/> if not null or the <see cref="CSVersion.ToString(CSVersionFormat, CIBuildDescriptor, bool)"/>
        /// with <see cref="CSVersionFormat.NuGetPackage"/> format if <see cref="AsCSVersion"/> is not null
        /// or the <see cref="NormalizedText"/>.
        /// </summary>
        /// <returns>The version to use for NuGet package.</returns>
        public string ToNuGetPackageString() => ErrorMessage
                                                ?? _csVersion?.ToString(CSVersionFormat.NuGetPackage)
                                                ?? NormalizedText;

        /// <summary>
        /// Compares this with another <see cref="SVersion"/>.
        /// </summary>
        /// <param name="other">The other version to compare with this instance.</param>
        /// <returns>
        /// </returns>
        public int CompareTo( SVersion other )
        {
            if( ReferenceEquals( other, null ) ) return 1;
            if( IsValid )
            {
                if( !other.IsValid ) return 1;
            }
            else if( other.IsValid ) return -1;

            var r = Major - other.Major;
            if( r != 0 ) return r;

            r = Minor - other.Minor;
            if( r != 0 ) return r;

            r = Patch - other.Patch;
            if( r != 0 ) return r;

            return ComparePreRelease( Prerelease, other.Prerelease );
        }

        static int ComparePreRelease( string x, string y )
        {
            if( x.Length == 0 ) return y.Length == 0 ? 0 : 1;
            if( y.Length == 0 ) return -1;

            var xParts = x.Split( '.' );
            var yParts = y.Split( '.' );

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
                var xP = xParts[i];
                var yP = yParts[i];
                int xN, yN, r;
                if( int.TryParse( xP, out xN ) )
                {
                    if( int.TryParse( yP, out yN ) )
                    {
                        r = xN - yN;
                        if( r != 0 ) return r;
                    }
                    else return -1;
                }
                else
                {
                    if( int.TryParse( yP, out yN ) ) return 1;
                    r = String.CompareOrdinal( xP, yP );
                    if( r != 0 ) return r;
                }
            }
            return ultimateResult;
        }

        /// <summary>
        /// Equality ignore ths <see cref="BuildMetaData"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals( object obj )
        {
            if( ReferenceEquals( obj, null ) ) return false;
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
        public bool Equals( SVersion other )
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
        static public bool operator ==( SVersion x, SVersion y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( !ReferenceEquals( x, null ) )
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
        static public bool operator >( SVersion x, SVersion y )
        {
            if( ReferenceEquals( x, y ) ) return false;
            if( !ReferenceEquals( x, null ) )
            {
                if( ReferenceEquals( y, null ) ) return true;
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
        static public bool operator >=( SVersion x, SVersion y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( !ReferenceEquals( x, null ) )
            {
                if( ReferenceEquals( y, null ) ) return true;
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
        static public bool operator !=( SVersion x, SVersion y ) => !(x == y);

        /// <summary>
        /// Implements &lt;= operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than or equal to y.</returns>
        static public bool operator <=( SVersion x, SVersion y ) => !(x > y);

        /// <summary>
        /// Implements &lt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than y.</returns>
        static public bool operator <( SVersion x, SVersion y ) => !(x >= y);
    }
}

