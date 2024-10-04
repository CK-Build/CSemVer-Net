using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSemVer;

/// <summary>
/// Defines standard informational version (usually stored in the <see cref="FileVersionInfo.ProductVersion"/>):
/// the two <see cref="SVersion"/> (the short and long forms), the <see cref="CommitSha"/> and the <see cref="CommitDate"/>.
/// <para>
/// The constructor can be used directly on any string, or <see cref="Parse"/> can be called (and throws an
/// <see cref="ArgumentException"/> if the result is not <see cref="IsValidSyntax"/>), or the informational version
/// can be extracted directly from a file thanks to <see cref="ReadFromFile"/>.
/// </para>
/// <para>
/// Syntax check is very strict (the <see cref="Zero"/> string is a sample) and should remain strict. 
/// What is missing in the equivalence check between NuGet and SemVer version: this requires a parse
/// of the NuGet version and it has yet to be done.
/// </para>
/// </summary>
public class InformationalVersion
{
    static readonly Regex _rOld = new Regex( @"^(?<1>.*?) \((?<2>.*?)\) - SHA1: (?<3>.*?) - CommitDate: (?<4>.*?)$" );
    // v6 format was ambiguous with build meta data: using / instead of + fix the issue.
    static readonly Regex _rV6 = new Regex( @"^(?<2>.*?)\+(?<3>.*?)/(?<4>.*?)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant );
    static readonly Regex _rV7 = new Regex( @"^(?<2>.*?)/(?<3>.*?)/(?<4>.*?)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant );

    /// <summary>
    /// The zero <see cref="InformationalVersion"/>.
    /// See <see cref="ZeroInformationalVersion"/>.
    /// </summary>
    static public InformationalVersion Zero = new InformationalVersion();

    /// <summary>
    /// The zero assembly version is "0.0.0".
    /// </summary>
    static public readonly string ZeroAssemblyVersion = "0.0.0";

    /// <summary>
    /// The zero file version is "0.0.0.0".
    /// </summary>
    static public readonly string ZeroFileVersion = "0.0.0.0";

    /// <summary>
    /// The zero SHA1 is "0000000000000000000000000000000000000000".
    /// </summary>
    static public readonly string ZeroCommitSha = "0000000000000000000000000000000000000000";

    /// <summary>
    /// The zero commit date is <see cref="DateTime.MinValue"/> in <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    static public readonly DateTime ZeroCommitDate = DateTime.SpecifyKind( DateTime.MinValue, DateTimeKind.Utc );

    /// <summary>
    /// The Zero standard informational version is "0.0.0-0/0000000000000000000000000000000000000000/0001-01-01 00:00:00Z".
    /// <para>
    /// These default values may be set in a csproj:
    /// <code>
    ///     &lt;Version&gt;0.0.0-0&lt;/Version&gt;
    ///     &lt;AssemblyVersion&gt;0.0.0&lt;/AssemblyVersion&gt;
    ///     &lt;FileVersion&gt;0.0.0.0&lt;/FileVersion&gt;
    ///     &lt;InformationalVersion&gt;0.0.0-0/0000000000000000000000000000000000000000/0001-01-01 00:00:00Z&lt;/InformationalVersion&gt;
    /// </code>
    /// </para>
    /// </summary>
    static public readonly string ZeroInformationalVersion = "0.0.0-0/0000000000000000000000000000000000000000/0001-01-01 00:00:00Z";


    /// <summary>
    /// Initializes a new <see cref="InformationalVersion"/> by parsing a string.
    /// This never throws: <see cref="IsValidSyntax"/> may be false and <see cref="ParseErrorMessage"/> exposes
    /// the error message.
    /// </summary>
    /// <param name="informationalVersion">Informational version. Can be null.</param>
    public InformationalVersion( string? informationalVersion )
    {
        if( (OriginalInformationalVersion = informationalVersion) != null )
        {
            Match m = _rV7.Match( informationalVersion );
            if( !m.Success ) m = _rV6.Match( informationalVersion );
            if( !m.Success ) m = _rOld.Match( informationalVersion );
            if( m.Success )
            {
                RawVersion = m.Groups[2].Value;
                CommitSha = m.Groups[3].Value;
                var v = SVersion.TryParse( RawVersion );
                Version = v.AsCSVersion != null ? v.AsCSVersion.ToNormalizedForm() : v;
                if( DateTime.TryParseExact( m.Groups[4].Value, "u", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime t ) )
                {
                    CommitDate = t;
                    if( t.Kind != DateTimeKind.Utc ) ParseErrorMessage = $"The CommitDate must be Utc: {m.Groups[4].Value} must be {DateTime.SpecifyKind( t, DateTimeKind.Utc ):u}.";
                    else if( !Version.IsValid ) ParseErrorMessage = "The SemVersion is invalid: " + Version.ErrorMessage;
                    else if( CommitSha.Length != 40 || !CommitSha.All( IsHexDigit ) ) ParseErrorMessage = "The CommitSha is invalid (must be 40 hex digit).";
                    else IsValidSyntax = true;
                }
                else ParseErrorMessage = "The CommitDate is invalid.It must be a UTC DateTime in \"u\" format.";
            }
            else ParseErrorMessage = "The String to parse does not match the standard CSemVer informational version pattern.";
        }
        else ParseErrorMessage = "String to parse is null.";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage( "Style", "IDE0060:Remove unused parameter", Justification = "This makes the role of this fake parameter explicit." )]
    InformationalVersion( string parseErrorMessage, bool forPrivateError )
    {
        ParseErrorMessage = parseErrorMessage;
    }

    InformationalVersion()
    {
        OriginalInformationalVersion = ZeroInformationalVersion;
        Version = SVersion.ZeroVersion;
        RawVersion = Version.NormalizedText;
        CommitSha = ZeroCommitSha;
        CommitDate = ZeroCommitDate;
        IsValidSyntax = true;
    }

    /// <summary>
    /// Gets whether <see cref="OriginalInformationalVersion"/> has been successfully parsed:
    /// <see cref="Version"/> is syntactically valid, the <see cref="CommitSha"/> is a 40 hexadecimal string
    /// and <see cref="CommitDate"/> has been successfully parsed.
    /// </summary>
    public bool IsValidSyntax { get; }

    /// <summary>
    /// Gets an error message whenever <see cref="IsValidSyntax"/> is true.
    /// Null otherwise.
    /// </summary>
    public string? ParseErrorMessage { get; }

    /// <summary>
    /// Gets the original informational version (can be null).
    /// </summary>
    public string? OriginalInformationalVersion { get; }

    /// <summary>
    /// Gets the semantic version string extracted from <see cref="OriginalInformationalVersion"/>. 
    /// Null if the OriginalInformationalVersion attribute was not standard.
    /// </summary>
    public string? RawVersion { get; }

    /// <summary>
    /// Gets the parsed <see cref="RawVersion"/> (that may be not <see cref="SVersion.IsValid"/>) 
    /// or null if the <see cref="OriginalInformationalVersion"/> attribute was not standard.
    /// Note that it is the <see cref="CSVersion.ToNormalizedForm()"/> if the version happens to be a <see cref="CSVersion"/>:
    /// long forms will be transformed into short forms.
    /// </summary>
    public SVersion? Version { get; }

    /// <summary>
    /// Gets the SHA1 extracted from the <see cref="OriginalInformationalVersion"/>.
    /// Null if the OriginalInformationalVersion attribute was not standard.
    /// </summary>
    public string? CommitSha { get; }

    /// <summary>
    /// Gets the commit date extracted from the <see cref="InformationalVersion"/>.
    /// <see cref="DateTime.MinValue"/> if the OriginalInformationalVersion attribute was not standard.
    /// This date is required to be in Utc in "u" DateTime format.
    /// </summary>
    public DateTime CommitDate { get; }

    /// <summary>
    /// Overridden to return the <see cref="ParseErrorMessage"/> or the <see cref="OriginalInformationalVersion"/>.
    /// </summary>
    /// <returns>The textual representation.</returns>
    public override string ToString() => ParseErrorMessage ?? OriginalInformationalVersion!;

    /// <summary>
    /// Parses the given string. Throws an <see cref="ArgumentException"/> if the syntax is invalid.
    /// To avoid exception, simply use the <see cref="InformationalVersion"/> constructor.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>A <see cref="IsValidSyntax"/> informational version.</returns>
    static public InformationalVersion Parse( string? s )
    {
        var i = new InformationalVersion( s );
        if( !i.IsValidSyntax ) throw new ArgumentException( i.ParseErrorMessage, nameof( s ) );
        return i;
    }

    /// <summary>
    /// Reads the <see cref="InformationalVersion"/> from a file, using <see cref="FileVersionInfo.GetVersionInfo"/>.
    /// This does not throw, instead the returned <see cref="IsValidSyntax"/> is false and <see cref="ParseErrorMessage"/>
    /// contains the error description.
    /// </summary>
    /// <param name="filePath">The path to the file. Must not be null or empty.</param>
    /// <returns>The informational version that may be invalid.</returns>
    static public InformationalVersion ReadFromFile( string filePath )
    {
        if( String.IsNullOrWhiteSpace( filePath ) )
        {
            throw new ArgumentNullException( nameof( filePath ) );
        }
        if( !File.Exists( filePath ) )
        {
            return new InformationalVersion( "File not found.", true );
        }
        try
        {
            var p = FileVersionInfo.GetVersionInfo( filePath )?.ProductVersion;
            return p != null
                    ? new InformationalVersion( p )
                    : new InformationalVersion( "The file has no FileVersionInfo.", true );
        }
        catch( Exception ex )
        {
            return new InformationalVersion( "Exception:" + ex.Message, true );
        }
    }

    /// <summary>
    /// reads the <see cref="InformationalVersion"/> from a loaded assembly: its <see cref="AssemblyInformationalVersionAttribute"/>
    /// is used.
    /// </summary>
    /// <param name="a">The assembly. Must not be null.</param>
    /// <returns>The informational version that may be invalid.</returns>
    static public InformationalVersion ReadFromAssembly( Assembly a )
    {
        if( a == null ) throw new ArgumentNullException( nameof( a ) );
        try
        {
            var attr = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute( a, typeof( AssemblyInformationalVersionAttribute ) );
            return attr != null
                    ? new InformationalVersion( attr.InformationalVersion )
                    : new InformationalVersion( "Unable to find AssemblyInformationalVersionAttribute.", true );
        }
        catch( Exception ex )
        {
            return new InformationalVersion( "Exception:" + ex.Message, true );
        }
    }


    /// <summary>
    /// Builds a standard Informational version string.
    /// </summary>
    /// <param name="version">The version. Must not be null nor invalid.</param>
    /// <param name="commitSha">The SHA1 of the commit (must be 40 hex digits).</param>
    /// <param name="commitDateUtc">The commit date (must be in UTC).</param>
    /// <returns>The informational version.</returns>
    static public string BuildInformationalVersion( SVersion version, string commitSha, DateTime commitDateUtc )
    {
        if( version == null || !version.IsValid ) throw new ArgumentException( nameof( version ) );
        if( commitSha == null || commitSha.Length != 40 || !commitSha.All( IsHexDigit ) ) throw new ArgumentException( "Must be a 40 hex digits string.", nameof( commitSha ) );
        if( commitDateUtc.Kind != DateTimeKind.Utc ) throw new ArgumentException( "Must be a UTC date.", nameof( commitDateUtc ) );
        return $"{version.ToNormalizedString()}/{commitSha}/{commitDateUtc:u}";
    }

    static bool IsHexDigit( char c ) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

}
