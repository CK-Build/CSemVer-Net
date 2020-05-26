using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CSemVer
{
    /// <summary>
    /// Handles 5 potentially different versions associated to <see cref="PackageQuality"/>.
    /// When all 5 versions are null then <see cref="IsValid"/> is false: as long as at least a <see cref="CI"/> version
    /// is specified, this is valid.
    /// </summary>
    public readonly struct PackageQualityVersions : IEnumerable<SVersion>
    {
        readonly SVersion? _sta;
        readonly SVersion? _lat;
        readonly SVersion? _pre;
        readonly SVersion? _exp;
        readonly SVersion? _ci;

        /// <summary>
        /// Initializes a new <see cref="PackageQualityVersions"/> from a set of versions.
        /// </summary>
        /// <param name="versions">Set of available versions.</param>
        /// <param name="versionsAreOrdered">True to shortcut the work as soon as a <see cref="PackageQuality.StableRelease"/> has been met.</param>
        public PackageQualityVersions( IEnumerable<SVersion> versions, bool versionsAreOrdered = false )
        {
            _ci = _exp = _pre = _lat = _sta = null;
            foreach( var v in versions )
            {
                Apply( v, ref _ci, ref _exp, ref _pre, ref _lat, ref _sta );
                if( versionsAreOrdered && v.PackageQuality == PackageQuality.StableRelease ) break;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="PackageQualityVersions"/> with known best versions.
        /// (this is a low level constructor that does not test/ensure anything).
        /// </summary>
        /// <param name="ci">The current best CI version.</param>
        /// <param name="exp">The current best Exploratory version.</param>
        /// <param name="pre">The current best Preview version.</param>
        /// <param name="lat">The current best Latest version.</param>
        /// <param name="sta">The current best Stable version.</param>
        public PackageQualityVersions( SVersion? ci, SVersion? exp, SVersion? pre, SVersion? lat, SVersion? sta )
        {
            _ci = ci;
            _exp = exp;
            _pre = pre;
            _lat = lat;
            _sta = sta;
        }

        /// <summary>
        /// Low level method that applies a new version to a vector of best versions.
        /// </summary>
        /// <param name="v">The new version.</param>
        /// <param name="ci">The current best CI version.</param>
        /// <param name="exp">The current best Exploratory version.</param>
        /// <param name="pre">The current best Preview version.</param>
        /// <param name="lat">The current best Latest version.</param>
        /// <param name="sta">The current best Stable version.</param>
        public static void Apply( SVersion v, [AllowNull]ref SVersion ci, ref SVersion? exp, ref SVersion? pre, ref SVersion? lat, ref SVersion? sta )
        {
            if( v != null && v.IsValid )
            {
                switch( v.PackageQuality )
                {
                    case PackageQuality.StableRelease: if( v > sta ) sta = v; goto case PackageQuality.ReleaseCandidate;
                    case PackageQuality.ReleaseCandidate: if( v > lat ) lat = v; goto case PackageQuality.Preview;
                    case PackageQuality.Preview: if( v > pre ) pre = v; goto case PackageQuality.Exploratory;
                    case PackageQuality.Exploratory: if( v > exp ) exp = v; goto default;
                    default: if( v > ci ) ci = v; break;
                }
            }
        }

        PackageQualityVersions( PackageQualityVersions q, SVersion v )
        {
            Debug.Assert( v?.IsValid ?? false, "v must be not null and valid." );
            _ci = q.CI;
            _exp = q.Exploratory;
            _pre = q.Preview;
            _lat = q.Latest;
            _sta = q.Stable;
            Apply( v, ref _ci, ref _exp, ref _pre, ref _lat, ref _sta );
        }

        /// <summary>
        /// Gets whether this <see cref="PackageQualityVersions"/> is valid: at least <see cref="CI"/>
        /// is available.
        /// </summary>
        public bool IsValid => CI != null;

        /// <summary>
        /// Gets the best version for a given quality or null if no such version exists.
        /// </summary>
        /// <param name="quality">The minimal required quality.</param>
        /// <returns>The best version or null if not found.</returns>
        public SVersion? GetVersion( PackageQuality quality )
        {
            return quality switch
            {
                PackageQuality.StableRelease => Stable,
                PackageQuality.ReleaseCandidate => Latest,
                PackageQuality.Preview => Preview,
                PackageQuality.Exploratory => Exploratory,
                _ => CI,
            };
        }

        /// <summary>
        /// Gets the best stable version or null if no such version exists.
        /// </summary>
        public SVersion? Stable => _sta;

        /// <summary>
        /// Gets the best latest compatible version or null if no such version exists.
        /// </summary>
        public SVersion? Latest => _lat;

        /// <summary>
        /// Gets the best preview compatible version or null if no such version exists.
        /// </summary>
        public SVersion? Preview => _pre;

        /// <summary>
        /// Gets the best exploratory compatible version or null if no such version exists.
        /// </summary>
        public SVersion? Exploratory => _exp;

        /// <summary>
        /// Gets the best version or null if <see cref="IsValid"/> is false.
        /// </summary>
        public SVersion? CI => _ci;

        /// <summary>
        /// Retuns this <see cref="PackageQualityVersions"/> or a new one that combines a new version.
        /// </summary>
        /// <param name="v">Version to handle. May be null or invalid.</param>
        /// <returns>The QualityVersions.</returns>
        public PackageQualityVersions WithVersion( SVersion v )
        {
            if( v == null || !v.IsValid ) return this;
            return IsValid ? new PackageQualityVersions( this, v ) : new PackageQualityVersions( new[] { v } );
        }

        /// <summary>
        /// Retuns this <see cref="PackageQualityVersions"/> or a new one combined with another one.
        /// </summary>
        /// <param name="other">Other versions to be combined.</param>
        /// <returns>The resulting QualityVersions.</returns>
        public PackageQualityVersions With( PackageQualityVersions other )
        {
            if( !IsValid ) return other;
            if( !other.IsValid ) return this;
            return new PackageQualityVersions( other.Concat( this ) );
        }

        /// <summary>
        /// Overridden to return the non null versions separated by /.
        /// </summary>
        /// <returns>A readable string.</returns>
        public override string ToString()
        {
            if( CI == null ) return String.Empty;
            var b = new StringBuilder();
            b.Append( CI.ToString() );
            if( Exploratory != null && Exploratory != CI )
            {
                b.Append( " / " ).Append( Exploratory.ToString() );
            }
            if( Preview != null && Preview != Exploratory )
            {
                b.Append( " / " ).Append( Preview.ToString() );
            }
            if( Latest != null && Latest != Preview )
            {
                b.Append( " / " ).Append( Latest.ToString() );
            }
            if( Stable != null && Stable != Latest )
            {
                b.Append( " / " ).Append( Stable.ToString() );
            }
            return b.ToString();
        }

        /// <summary>
        /// Returns the distinct CI, Exploratory, Preview, Latest, Stable (in this order) as long as they are not null.
        /// </summary>
        /// <returns>The set of distinct versions (empty if <see cref="IsValid"/> is false).</returns>
        public IEnumerator<SVersion> GetEnumerator()
        {
            if( CI != null )
            {
                yield return CI;
                if( Exploratory != null )
                {
                    if( Exploratory != CI )  yield return Exploratory;
                    if( Preview != null )
                    {
                        if( Preview != Exploratory ) yield return Preview;
                        if( Latest != null )
                        {
                            if( Latest != Preview ) yield return Latest;
                            if( Stable != null && Stable != Latest )
                            {
                                yield return Stable;
                            }
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
