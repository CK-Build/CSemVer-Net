using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSemVer
{

    /// <summary>
    /// Encapsulates CSemVer-CI suffix formatting.
    /// </summary>
    public class CIBuildDescriptor
    {
        /// <summary>
        /// Defines the maximal build index.
        /// This is required to be able to pad it with a constant number of '0'.
        /// </summary>
        public const int MaxShortFormIndex = 9999;

        int _buildIndex;

        /// <summary>
        /// Gets or sets the build index. Must be greater or equal to 0.
        /// To be valid for NuGetV2, it must not exceed <see cref="MaxShortFormIndex"/>.
        /// </summary>
        public int BuildIndex 
        {
            get { return _buildIndex; } 
            set
            {
                if( _buildIndex < 0 ) throw new ArgumentException();
                _buildIndex = value;
            }
        }

        /// <summary>
        /// Gets or set the branch name to use.
        /// When null or empty, this descriptor is not applicable.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets whether this descriptor can be applied.
        /// </summary>
        public bool IsValid
        {
            get { return _buildIndex >= 0 && !string.IsNullOrWhiteSpace( BranchName ); }
        }

        /// <summary>
        /// Gets whether this descriptor can be applied for NuGetV2 special name case.
        /// </summary>
        public bool IsValidForShortForm
        {
            get { return IsValid && _buildIndex <= MaxShortFormIndex && BranchName.Length <= 8; }
        }

        /// <summary>
        /// Overridden to return "ci.<see cref="BuildIndex"/>.<see cref="BranchName"/>" when <see cref="IsValid"/> is true,
        /// the empty string otherwise.
        /// </summary>
        /// <returns>The long form like "ci.16.develop".</returns>
        public override string ToString()
        {
            return IsValid ? string.Format( "ci.{0}.{1}", BuildIndex, BranchName ) : string.Empty;
        }

        /// <summary>
        /// When <see cref="IsValidForShortForm"/> is true, returns "<see cref="BuildIndex"/>-<see cref="BranchName"/>" where 
        /// the index is padded with 0, the empty string otherwise.
        /// </summary>
        /// <returns>The short form like "0016-develop".</returns>
        public string ToStringForShortForm()
        {
            Debug.Assert( MaxShortFormIndex.ToString().Length == 4 );
            return IsValid ? string.Format( "{0:0000}-{1}", BuildIndex, BranchName ) : string.Empty;
        }

        /// <summary>
        /// Creates the ZeroTimed short form version string. It uses a base 36 alphabet (case insensitive) and consider the nunber
        /// of seconds from 1st of january 2015: this fits into 7 characters.
        /// </summary>
        /// <param name="ciBuildName">The BuildName string (typically "develop"). Must not be null, empty or longer than 8 characters.</param>
        /// <param name="timeRelease">The utc date time of the release.</param>
        /// <returns>A Short form version string like "O.O.O--009iJKg-develop".</returns>
        public static string CreateShortFormZeroTimed( string ciBuildName, DateTime timeRelease )
        {
            CheckCIBuildName( ciBuildName, true );
            DateTime baseTime = new DateTime( 2015, 1, 1, 0, 0, 0, DateTimeKind.Utc );
            if( timeRelease < baseTime ) throw new ArgumentException( $"Must be at least {baseTime}.", nameof( timeRelease ) );

            TimeSpan delta200 = timeRelease - baseTime;
            Debug.Assert( Math.Log( 1000 * 366 * 24 * 60 * (long)60, 36 ) < 7, "Using Base36: 1000 years in seconds on 7 chars!" );
            long second = (long)delta200.TotalSeconds;
            string b36 = ToBase36( second );
            string ver = new string( '0', 7 - b36.Length ) + b36;
            return string.Format( "0.0.0--{0}-{1}", ver, ciBuildName );
        }

        /// <summary>
        /// Creates the ZeroTimed SemVer version string. The <paramref name="baseVersion"/>, if not null, is appended 
        /// as a suffix (Build metadata).
        /// </summary>
        /// <param name="ciBuildName">The BuildName string (typically "develop").</param>
        /// <param name="timeRelease">The utc date time of the release.</param>
        /// <returns>A SemVer version string like "O.O.O--ci.2018-07-27T09-45-28-34.develop".</returns>
        public static string CreateSemVerZeroTimed( string ciBuildName, DateTime timeRelease )
        {
            CheckCIBuildName( ciBuildName, false );
            return string.Format( "0.0.0--ci.{0:yyyy-MM-ddTHH-mm-ss-ff}.{1}", timeRelease, ciBuildName );
        }

        static void CheckCIBuildName( string ciBuildName, bool shortForm )
        {
            if( string.IsNullOrWhiteSpace( ciBuildName ) ) throw new ArgumentException( "Must not be null, empty or whitespace.", nameof( ciBuildName ) );
            if( shortForm && ciBuildName.Length > 8 ) throw new ArgumentException( "Must not be longer than 8 characters", nameof( ciBuildName ) );
        }

        static string ToBase36( long number )
        {
            // Naïve implementation that does the job.
            var alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
            Debug.Assert( alphabet.Length == 36 );
            var n = number;
            long basis = 36;
            var ret = "";
            while( n > 0 )
            {
                long temp = n % basis;
                ret = alphabet[(int)temp] + ret;
                n = (n / basis);
            }
            return ret;
        }
    }
}
