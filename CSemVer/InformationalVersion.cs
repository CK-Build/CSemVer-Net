using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSemVer
{
    /// <summary>
    /// Parses a standard informational version in order to extract the two <see cref="SVersion"/> (the short and long forms), 
    /// the <see cref="CommitSha"/> and the <see cref="CommitDate"/> if possible.
    /// Syntax check is very strict (the <see cref="Zero"/> string is a sample) and should remain strict. 
    /// What is missing in the equivalence check between NuGet and SemVer version: this requires a parse
    /// of the NuGet version and it has yet to be done.
    /// </summary>
    public class InformationalVersion
    {
        static Regex _r = new Regex( @"^(?<1>.*?) \((?<2>.*?)\) - SHA1: (?<3>.*?) - CommitDate: (?<4>.*?)$" );

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
        /// The Zero standard informational version is "0.0.0-0 (0.0.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z".
        /// <para>
        /// These default values may be set in a csproj:
        /// <code>
        ///     &lt;Version&gt;0.0.0-0&lt;/Version&gt;
        ///     &lt;AssemblyVersion&gt;0.0.0&lt;/AssemblyVersion&gt;
        ///     &lt;FileVersion&gt;0.0.0.0&lt;/FileVersion&gt;
        ///     &lt;InformationalVersion&gt;0.0.0-0 (0.0.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z&lt;/InformationalVersion&gt;
        /// </code>
        /// </para>
        /// </summary>
        static public readonly string ZeroInformationalVersion = "0.0.0-0 (0.0.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z";


        /// <summary>
        /// Initializes a new <see cref="InformationalVersion"/> by parsing a string.
        /// </summary>
        /// <param name="informationalVersion">Informational version. Can be null.</param>
        public InformationalVersion( string informationalVersion )
        {
            if( (OriginalInformationalVersion = informationalVersion) != null )
            {
                Match m = _r.Match( informationalVersion );
                if( m.Success )
                {
                    RawSemVersion = m.Groups[1].Value;
                    RawNuGetVersion = m.Groups[2].Value;
                    CommitSha = m.Groups[3].Value;
                    SemVersion = SVersion.TryParse( RawSemVersion );
                    NuGetVersion = SVersion.TryParse( RawNuGetVersion );
                    DateTime t;
                    if( DateTime.TryParseExact( m.Groups[4].Value, "u", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal, out t ) )
                    {
                        CommitDate = t;
                        if( t.Kind != DateTimeKind.Utc ) ParseErrorMessage = $"The CommitDate must be Utc: {m.Groups[4].Value} must be {DateTime.SpecifyKind( t, DateTimeKind.Utc ).ToString("u")}.";
                        else if( !SemVersion.IsValidSyntax ) ParseErrorMessage = "The SemVersion is invalid: " + SemVersion.ParseErrorMessage;
                        else if( !NuGetVersion.IsValidSyntax ) ParseErrorMessage = "The NuGetVersion is invalid: " + NuGetVersion.ParseErrorMessage;
                        else if( CommitSha.Length != 40 || !CommitSha.All( IsHexDigit ) ) ParseErrorMessage = "The CommitSha is invalid (must be 40 hex digit).";
                        else IsValidSyntax = true;
                    }
                    else ParseErrorMessage = "The CommitDate is invalid.It must be a UTC DateTime in \"u\" format.";
                }
                else ParseErrorMessage = "The String to parse does not match the standard CSemVer informational version pattern.";
            }
            else ParseErrorMessage = "String to parse is null.";
        }

        InformationalVersion()
        {
            OriginalInformationalVersion = ZeroInformationalVersion;
            NuGetVersion = SemVersion = SVersion.ZeroVersion;
            RawNuGetVersion = RawSemVersion = SemVersion.Text;
            CommitSha = ZeroCommitSha;
            CommitDate = ZeroCommitDate;
            IsValidSyntax = true;
        }

        /// <summary>
        /// Gets whether <see cref="OriginalInformationalVersion"/> has been sucessfully parsed:
        /// both <see cref="SemVersion"/> and <see cref="NuGetVersion"/> are syntaxically valid <see cref="SVersion"/>,
        /// the <see cref="CommitSha"/> is a 40 hexadecimal string and <see cref="CommitDate"/> has been successfully parsed.
        /// </summary>
        public bool IsValidSyntax { get; }

        /// <summary>
        /// Gets an error message whenever <see cref="IsValidSyntax"/> is true.
        /// Null otherwise.
        /// </summary>
        public string ParseErrorMessage { get; }

        /// <summary>
        /// Gets the original informational (can be null).
        /// </summary>
        public string OriginalInformationalVersion { get; }

        /// <summary>
        /// Gets the semantic version string extracted from <see cref="OriginalInformationalVersion"/>. 
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public string RawSemVersion { get; }

        /// <summary>
        /// Gets the parsed <see cref="RawSemVersion"/> (that may be not <see cref="SVersion.IsValidSyntax"/>) 
        /// or null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public SVersion SemVersion { get; }

        /// <summary>
        /// Gets the NuGet version extracted from the <see cref="OriginalInformationalVersion"/>.
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public string RawNuGetVersion { get; }

        /// <summary>
        /// Gets the parsed <see cref="RawNuGetVersion"/> (that may be not <see cref="SVersion.IsValidSyntax"/>) 
        /// or null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public SVersion NuGetVersion { get; }

        /// <summary>
        /// Gets the SHA1 extracted from the <see cref="OriginalInformationalVersion"/>.
        /// Null if the OriginalInformationalVersion attribute was not standard.
        /// </summary>
        public string CommitSha { get; }

        /// <summary>
        /// Gets the commit date  extracted from the <see cref="InformationalVersion"/>.
        /// <see cref="DateTime.MinValue"/> if the OriginalInformationalVersion attribute was not standard.
        /// This date is required to be in Utc in "u" DateTime format.
        /// </summary>
        public DateTime CommitDate { get; }

        /// <summary>
        /// Overridden to return the <see cref="ParseErrorMessage"/> or the <see cref="OriginalInformationalVersion"/>.
        /// </summary>
        /// <returns>The textual representation.</returns>
        public override string ToString() => ParseErrorMessage ?? OriginalInformationalVersion;

        /// <summary>
        /// Parses the given string. Throws an <see cref="ArgumentException"/> if the syntax is invalid.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>A <see cref="IsValidSyntax"/> informational version.</returns>
        static public InformationalVersion Parse( string s )
        {
            var i = new InformationalVersion( s );
            if( !i.IsValidSyntax ) throw new ArgumentException( i.ParseErrorMessage, nameof( s ) );
            return i;
        }


        /// <summary>
        /// Builds a standard Informational version string.
        /// </summary>
        /// <param name="semVer">The semantic version. Must be not null nor empty (no syntaxic validation is done).</param>
        /// <param name="nugetVer">The nuget version. Must be not null nor empty (no syntaxic validation is done).</param>
        /// <param name="commitSha">The SHA1 of the commit (must be 40 hex digits).</param>
        /// <param name="commitDateUtc">The commit date (must be in UTC).</param>
        /// <returns>The informational version.</returns>
        static public string BuildInformationalVersion( string semVer, string nugetVer, string commitSha, DateTime commitDateUtc )
        {
            if( string.IsNullOrWhiteSpace( semVer ) ) throw new ArgumentException( nameof( semVer ) );
            if( string.IsNullOrWhiteSpace( nugetVer ) ) throw new ArgumentException( nameof( nugetVer ) );
            if( commitSha == null || commitSha.Length != 40 || !commitSha.All( IsHexDigit ) ) throw new ArgumentException( "Must be a 40 hex digits string.", nameof( commitSha ) );
            if( commitDateUtc.Kind != DateTimeKind.Utc ) throw new ArgumentException( "Must be a UTC date.", nameof( commitDateUtc ) );
            return $"{semVer} ({nugetVer}) - SHA1: {commitSha} - CommitDate: {commitDateUtc.ToString( "u" )}";
        }

        static bool IsHexDigit( char c ) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    }

}
