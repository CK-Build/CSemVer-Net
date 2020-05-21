using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSemVer
{
    /// <summary>
    /// CSemVer version follows [v|V]Major.Minor.Patch[-PreReleaseName[.PreReleaseNumber[.PreReleaseFix]]] pattern (see <see cref="IsLongForm"/>)
    /// or [v|V]Major.Minor.Patch[-PreReleaseNameShort[PreReleaseNumber[-PreReleaseFix]]] (short form).
    /// This is a semantic version, this is the version associated to a commit in the repository: a CI-Build version
    /// is a SemVer <see cref="SVersion"/> but not a CSemVer version.
    /// </summary>
    public sealed partial class CSVersion : SVersion, IEquatable<CSVersion>, IComparable<CSVersion>
    {
        // It has to be here because of static initialization order.
        static readonly Regex _rRelaxed = new Regex( @"^(?<1>a(lpha)?|b(eta)?|d(elta)?|e(psilon)?|g(amma)?|k(appa)?|p(re(release)?)?|rc?)(\.|-)?((?<2>[0-9]?[0-9])((\.|-)?(?<3>[0-9]?[0-9]))?)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase );

        /// <summary>
        /// Gets the standard pre release name among <see cref="StandardPrereleaseNames"/>.
        /// <see cref="string.Empty"/> when this is not a pre release version.
        /// </summary>
        public string PrereleaseName => IsPrerelease ? _standardNames[PrereleaseNameIdx] : string.Empty;

        /// <summary>
        /// Gets whether this is a pre release.
        /// </summary>
        public bool IsPrerelease => PrereleaseNameIdx >= 0;

        /// <summary>
        /// When <see cref="IsPrerelease"/> is true, this is between 0 ('alpha') and <see cref="MaxPreReleaseNameIdx"/> ('rc')
        /// otherwise this is -1.
        /// </summary>
        public readonly int PrereleaseNameIdx;

        /// <summary>
        /// Meaningful only if <see cref="IsPrerelease"/> is true (0 when not in prerelease). Between 0 and <see cref="MaxPreReleaseNumber"/>. 
        /// </summary>
        public readonly int PrereleaseNumber;

        /// <summary>
        /// When <see cref="IsPreReleasePatch"/>, a number between 1 and <see cref="MaxPreReleasePatch"/>, otherwise 0. 
        /// </summary>
        public readonly int PrereleasePatch;

        /// <summary>
        /// Long form uses <see cref="StandardPrereleaseNames"/> and dotted numbers instead of <see cref="StandardPreReleaseNamesShort"/>
        /// and dashed separated 0 padded numbers.
        /// Long form eventually appeared to be less readable than the short (historically NuGet V2 compatible) form: the default is now the short form. 
        /// </summary>
        public readonly bool IsLongForm;

        /// <summary>
        /// Gets whether this is a pre release patch (<see cref="IsPrerelease"/> is necessarily true): <see cref="PrereleasePatch"/> number is greater than 0.
        /// </summary>
        public bool IsPreReleasePatch => PrereleasePatch > 0;

        /// <summary>
        /// Gets whether this is a patch: either <see cref="SVersion.Patch"/> or <see cref="PrereleasePatch"/> are greater than 0.
        /// </summary>
        public bool IsPatch => PrereleasePatch > 0 || Patch > 0;

        /// <summary>
        /// Gets whether this <see cref="CSVersion"/> is marked with 'invalid' <see cref="SVersion.BuildMetaData"/>.
        /// This is the strongest form for a version: a +invalid marked version MUST annihilate any same version
        /// when they both appear on a commit.
        /// </summary>
        public bool IsMarkedInvalid => StringComparer.OrdinalIgnoreCase.Equals( BuildMetaData, "invalid" );

        /// <summary>
        /// Gets the strength of this version: an invalid version has a strength of 0, valid ones have 1
        /// and ultimately, a <see cref="IsMarkedInvalid"/> wins with 2.
        /// </summary>
        public int DefinitionStrength => IsValid ? (IsMarkedInvalid ? 2 : 1) : 0;

        /// <summary>
        /// Gets the empty array singleton.
        /// </summary>
        public static readonly CSVersion[] EmptyArray = Array.Empty<CSVersion>();

        CSVersion( int major, int minor, int patch, string buildMetaData,
                   int preReleaseNameIdx, int preReleaseNumber, int preReleasePatch,
                   bool longForm, long number = 0, string? parsedText = null )
            : base( parsedText, major, minor, patch, ComputeStandardPreRelease( preReleaseNameIdx, preReleaseNumber, preReleasePatch, longForm ), buildMetaData, null )
        {
            PrereleaseNameIdx = preReleaseNameIdx;
            PrereleaseNumber = preReleaseNumber;
            PrereleasePatch = preReleasePatch;
            IsLongForm = longForm;
            _orderedVersion = new SOrderedVersion() {
                Number = number != 0 ? number : ComputeOrderedVersion( major, minor, patch, preReleaseNameIdx, preReleaseNumber, preReleasePatch )
            };
            InlineAssertInvariants( this );
        }

        CSVersion( string error, string? parsedText )
            : base( error, parsedText )
        {
            PrereleaseNameIdx = -1;
        }

        CSVersion( CSVersion other, string buildMetaData )
            : base( other, buildMetaData, null )
        {
            PrereleaseNameIdx = other.PrereleaseNameIdx;
            PrereleaseNumber = other.PrereleaseNumber;
            PrereleasePatch = other.PrereleasePatch;
            _orderedVersion = other._orderedVersion;
            IsLongForm = other.IsLongForm;
            InlineAssertInvariants( this );
        }

#if DEBUG
        [ThreadStatic]
        static bool _alreadyInCheck;
#endif

        [Conditional( "DEBUG" )]
        static void InlineAssertInvariants( CSVersion v )
        {
#if DEBUG
            if( !_alreadyInCheck && v.IsValid )
            {
                _alreadyInCheck = true;
                try
                {
                    if( v.IsLongForm )
                    {
                        Debug.Assert( v.NormalizedText == ComputeLongFormVersion( v.Major, v.Minor, v.Patch, v.PrereleaseNameIdx, v.PrereleaseNumber, v.PrereleasePatch, v.BuildMetaData ) );
                    }
                    else
                    {
                        Debug.Assert( v.NormalizedText == ComputeShortFormVersion( v.Major, v.Minor, v.Patch, v.PrereleaseNameIdx, v.PrereleaseNumber, v.PrereleasePatch, v.BuildMetaData ) );
                    }
                    //// Systematically checks that a valid CSVersion can be parsed back in Long or Short form.
                    Debug.Assert( SVersion.TryParse( v.ToString( CSVersionFormat.Normalized ) ).Equals( v.ToNormalizedForm() ) );
                    Debug.Assert( SVersion.TryParse( v.ToString( CSVersionFormat.LongForm ) ).Equals( v.ToLongForm() ) );
                }
                finally
                {
                    _alreadyInCheck = false;
                }
            }
#endif
        }

        /// <summary>
        /// Returns either this versions or the same version but expressed in long form.
        /// Note that when <see cref="SVersion.IsValid"/> is false, this is returned unchanged.
        /// </summary>
        /// <returns>This version expressed in long form.</returns>
        public CSVersion ToLongForm()
        {
            if( IsLongForm || !IsValid ) return this;
            return new CSVersion( Major, Minor, Patch, BuildMetaData, PrereleaseNameIdx, PrereleaseNumber, PrereleasePatch, true, OrderedVersion );
        }

        /// <summary>
        /// Returns either this versions or the same version but expressed in short form (that is the default, normalized, form).
        /// Note that when <see cref="SVersion.IsValid"/> is false, this is returned unchanged.
        /// </summary>
        /// <returns>This version expressed in short form.</returns>
        public CSVersion ToNormalizedForm()
        {
            if( IsLongForm && IsValid ) return new CSVersion( Major, Minor, Patch, BuildMetaData, PrereleaseNameIdx, PrereleaseNumber, PrereleasePatch, false, OrderedVersion );
            return this;
        }

        /// <summary>
        /// Returns a new <see cref="CSVersion"/> with a potentialy new <see cref="SVersion.BuildMetaData"/>.
        /// </summary>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <returns>The version.</returns>
        public new CSVersion WithBuildMetaData( string buildMetaData ) => (CSVersion)base.WithBuildMetaData( buildMetaData );

        /// <summary>
        /// Hidden overridable implementation.
        /// </summary>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <returns>The new version.</returns>
        protected override SVersion DoWithBuildMetaData( string buildMetaData ) => new CSVersion( this, buildMetaData );

        /// <summary>
        /// Computes the next possible ordered versions, from the closest one to the biggest possible bump.
        /// If <see cref="SVersion.IsValid"/> is false, the list is empty.
        /// </summary>
        /// <param name="patchesOnly">True to obtain only patches to this version. False to generate the full list of valid successors (up to 43 successors).</param>
        /// <returns>Next possible versions.</returns>
        public IEnumerable<CSVersion> GetDirectSuccessors( bool patchesOnly = false )
        {
            Debug.Assert( _standardNames[0] == "alpha" );
            if( IsValid )
            {
                if( IsPrerelease )
                {
                    int nextPrereleasePatch = PrereleasePatch + 1;
                    if( nextPrereleasePatch <= MaxPreReleasePatch )
                    {
                        yield return new CSVersion( Major, Minor, Patch, BuildMetaData, PrereleaseNameIdx, PrereleaseNumber, nextPrereleasePatch, IsLongForm );
                    }
                    if( !patchesOnly )
                    {
                        int nextPrereleaseNumber = PrereleaseNumber + 1;
                        if( nextPrereleaseNumber <= MaxPreReleaseNumber )
                        {
                            yield return new CSVersion( Major, Minor, Patch, BuildMetaData, PrereleaseNameIdx, nextPrereleaseNumber, 0, IsLongForm );
                        }
                        int nextPrereleaseNameIdx = PrereleaseNameIdx + 1;
                        if( nextPrereleaseNameIdx <= CSVersion.MaxPreReleaseNameIdx )
                        {
                            yield return new CSVersion( Major, Minor, Patch, BuildMetaData, nextPrereleaseNameIdx, 0, 0, IsLongForm );
                            while( ++nextPrereleaseNameIdx <= MaxPreReleaseNameIdx )
                            {
                                yield return new CSVersion( Major, Minor, Patch, BuildMetaData, nextPrereleaseNameIdx, 0, 0, IsLongForm );
                            }
                        }
                        yield return new CSVersion( Major, Minor, Patch, BuildMetaData, -1, 0, 0, IsLongForm );
                    }
                }
                if( !IsPrerelease || Major == 0 )
                {
                    // A pre release version can not reach the next patch... Except the 0 major.
                    int nextPatch = Patch + 1;
                    if( nextPatch <= MaxPatch )
                    {
                        for( int i = 0; i <= MaxPreReleaseNameIdx; ++i )
                        {
                            yield return new CSVersion( Major, Minor, nextPatch, BuildMetaData, i, 0, 0, IsLongForm );
                        }
                        yield return new CSVersion( Major, Minor, nextPatch, BuildMetaData, -1, 0, 0, IsLongForm );
                    }
                }
                if( !patchesOnly )
                {
                    int nextMinor = Minor + 1;
                    if( nextMinor <= MaxMinor && (!IsPrerelease || Patch != 0 || Major == 0) )
                    {
                        yield return new CSVersion( Major, nextMinor, 0, BuildMetaData, 0, 0, 0, IsLongForm );
                        if( !patchesOnly )
                        {
                            for( int i = 1; i <= MaxPreReleaseNameIdx; ++i )
                            {
                                yield return new CSVersion( Major, nextMinor, 0, BuildMetaData, i, 0, 0, IsLongForm );
                            }
                        }
                        yield return new CSVersion( Major, nextMinor, 0, BuildMetaData, -1, 0, 0, IsLongForm );
                    }

                    int nextMajor = Major + 1;
                    if( nextMajor <= MaxMajor && (!IsPrerelease || (Minor != 0 || Patch != 0) || Major == 0) )
                    {
                        yield return new CSVersion( nextMajor, 0, 0, BuildMetaData, 0, 0, 0, IsLongForm );
                        if( !patchesOnly )
                        {
                            for( int i = 1; i <= MaxPreReleaseNameIdx; ++i )
                            {
                                yield return new CSVersion( nextMajor, 0, 0, BuildMetaData, i, 0, 0, IsLongForm );
                            }
                        }
                        yield return new CSVersion( nextMajor, 0, 0, BuildMetaData, -1, 0, 0, IsLongForm );
                    }
                }
            }
        }

        /// <summary>
        /// Computes whether the given version belongs to the set of predecessors.
        /// This currently does no more than calling <see cref="GetDirectSuccessors(bool)"/> and checking the existence of
        /// the given <paramref name="previous"/> version. If this need an optimized implementation this can be done but
        /// for the moment, this is the safest (and easiest) way to do this.
        /// </summary>
        /// <param name="previous">Previous version. Can be null.</param>
        /// <returns>True if previous is actually a direct predecessor.</returns>
        public bool IsDirectPredecessor( CSVersion? previous )
        {
            if( !IsValid ) return false;
            long num = _orderedVersion.Number;
            if( previous == null ) return FirstPossibleVersions.Contains( this );
            if( previous._orderedVersion.Number >= num ) return false;
            if( previous._orderedVersion.Number == num - 1L ) return true;

            // Major bump greater than 1: previous can not be a direct predecessor.
            if( Major > previous.Major + 1 ) return false;

            foreach( var succ in previous.GetDirectSuccessors() )
            {
                var delta = succ._orderedVersion.Number - _orderedVersion.Number;
                if( delta == 0 ) return true;
                if( delta > 0 ) break;
            }
            return false;
        }

        /// <summary>
        /// This static version handles null <paramref name="version"/> (the next versions are always <see cref="FirstPossibleVersions"/>).
        /// If the version is not valid or it it is <see cref="VeryLastVersion"/>, the list is empty.
        /// </summary>
        /// <param name="version">Any version (can be null).</param>
        /// <param name="patchesOnly">True to obtain only patches to the version. False to generate the full list of valid successors (up to 43 successors).</param>
        /// <returns>The direct successors.</returns>
        public static IEnumerable<CSVersion> GetDirectSuccessors( bool patchesOnly, CSVersion? version = null )
        {
            if( version == null )
            {
                return FirstPossibleVersions;
            }
            return version.GetDirectSuccessors( patchesOnly );
        }
    }
}
