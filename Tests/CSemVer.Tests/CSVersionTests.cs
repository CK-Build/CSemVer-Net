using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
//using Semver;
using CSemVer;
using FluentAssertions;

namespace CSemVer.Tests
{
    [TestFixture]
    public class CSVersionTests
    {

        [Explicit]
        [TestCase( "v0.0.0-alpha.1" )]
        [TestCase( "v0.0.0-alpha.2" )]
        [TestCase( "v0.0.0-alpha.0.1" )]
        [TestCase( "v1.0.0" )]
        [TestCase( "v1.0.1" )]
        [TestCase( "v1.1.0" )]
        [TestCase( "v1.1.0" )]
        [TestCase( "v2.0.0-rc" )]
        [TestCase( "v1.2.3" )]
        [TestCase( "v1.2.3-alpha" )]
        [TestCase( "v1.2.3-delta.5" )]
        [TestCase( "v1.2.3-prerelease.2.3" )]
        [TestCase( "v1.2.3-rc" )]
        public void display_successors_samples( string v )
        {
            CSVersion t = CSVersion.TryParse( v );
            var succ = t.GetDirectSuccessors( false );

            Console.WriteLine( " -> - found {0} successors for '{1}' (Ordered Version = {2}, File = {3}):",
                                succ.Count(),
                                t,
                                t.OrderedVersion,
                                t.ToStringFileVersion( false ) );
            Console.WriteLine( "      " + string.Join( ", ", succ.Select( s => s.ToString() ) ) );

            var closest = t.GetDirectSuccessors( true ).Select( s => s.ToString() ).ToList();
            Console.WriteLine( "    - {0} next fixes:", closest.Count, t );
            Console.WriteLine( "      " + string.Join( ", ", closest ) );
        }

        [TestCase( "0.0.0" )]
        [TestCase( "3.0.1" )]
        [TestCase( "3.0.1" )]
        [TestCase( "99999.49999.9999" )]
        public void parsing_valid_release( string tag )
        {
            CSVersion t = CSVersion.Parse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsLongForm, Is.False );
            Assert.That( t.IsPrerelease, Is.False );
            Assert.That( t.IsPreReleasePatch, Is.False );
            Assert.That( t.ToString( CSVersionFormat.LongForm ), Is.EqualTo( tag ) );
            Assert.That( t.ToString( CSVersionFormat.LongFormWithBuildMetaData ), Is.EqualTo( tag ) );
            Assert.That( t.ToString(), Is.EqualTo( tag ) );
            Assert.That( t.NormalizedText, Is.EqualTo( tag ) );
            Assert.That( t.NormalizedTextWithBuildMetaData, Is.EqualTo( tag ) );
        }

        [TestCase( "0.0.0", false )]
        [TestCase( "3.0.1", false )]
        [TestCase( "1.0.0-a", false )]
        [TestCase( "1.0.0-a01", false )]
        [TestCase( "1.0.0-a55-66", false )]
        [TestCase( "1.0.0-alpha", true )]
        [TestCase( "1.0.0-alpha.1", true )]
        [TestCase( "1.0.0-alpha.55.66", true )]
        public void CSVersion_Parse_with_long_forms( string tag, bool isLongForm )
        {
            CSVersion t = CSVersion.Parse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsLongForm, Is.EqualTo( isLongForm ) );
        }

        [TestCase( "1.0.0-a", "1.0.0-a" )]
        [TestCase( "1.0.0-a01", "1.0.0-a01" )]
        [TestCase( "1.0.0-a.01", "1.0.0-a01" )]
        [TestCase( "1.0.0-a-01", "1.0.0-a01" )]
        [TestCase( "1.0.0-a-1", "1.0.0-a01" )]
        [TestCase( "1.0.0-a.1", "1.0.0-a01" )]
        [TestCase( "1.0.0-a55-06", "1.0.0-a55-06" )]
        [TestCase( "1.0.0-a-55.6", "1.0.0-a55-06" )]
        [TestCase( "1.0.0-a.55.06", "1.0.0-a55-06" )]
        [TestCase( "1.0.0-a.55.6", "1.0.0-a55-06" )]
        [TestCase( "1.0.0-alpha", "1.0.0-alpha" )]
        [TestCase( "1.0.0-alpha01", "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha.01", "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha-01", "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha-1", "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha.1", "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha55-06", "1.0.0-alpha.55.6" )]
        [TestCase( "1.0.0-alpha-55.6", "1.0.0-alpha.55.6" )]
        [TestCase( "1.0.0-alpha.55.06", "1.0.0-alpha.55.6" )]
        [TestCase( "1.0.0-alpha.55.6", "1.0.0-alpha.55.6" )]
        public void parsing_allows_slightly_deviant_forms( string tag, string finalForm )
        {
            CSVersion.Parse( tag ).NormalizedText.Should().Be( finalForm );
        }

        [TestCase( "v0.0.0-alpha", 0, 0, 0, 1 )]
        [TestCase( "v0.0.0-alpha.0.1", 0, 0, 0, 2 )]
        [TestCase( "v0.0.0-alpha.0.2", 0, 0, 0, 3 )]
        [TestCase( "v0.0.0-alpha.1", 0, 0, 0, 101 )]
        [TestCase( "v0.0.0-beta", 0, 0, 0, 100 * 99 + 101 )]
        public void version_ordering_starts_at_1_for_the_very_first_possible_version( string tag, int oMajor, int oMinor, int oBuild, int oRevision )
        {
            var t = CSVersion.TryParse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.OrderedVersionMajor, Is.EqualTo( oMajor ) );
            Assert.That( t.OrderedVersionMinor, Is.EqualTo( oMinor ) );
            Assert.That( t.OrderedVersionBuild, Is.EqualTo( oBuild ) );
            Assert.That( t.OrderedVersionRevision, Is.EqualTo( oRevision ) );
            long vf = t.OrderedVersion << 1;
            Assert.That( t.ToStringFileVersion( false ),
                    Is.EqualTo( string.Format( "{0}.{1}.{2}.{3}", vf >> 48, (vf >> 32) & 0xFFFF, (vf >> 16) & 0xFFFF, vf & 0xFFFF ) ) );
            vf |= 1;
            Assert.That( t.ToStringFileVersion( true ),
                    Is.EqualTo( string.Format( "{0}.{1}.{2}.{3}", vf >> 48, (vf >> 32) & 0xFFFF, (vf >> 16) & 0xFFFF, vf & 0xFFFF ) ) );
        }

        [TestCase( "0", 0, "Invalid are always 0." )]
        [TestCase( "0.0.0-prerelease", 1, "Normal = 1." )]
        [TestCase( "0.0.0", 1, "Normal = 1" )]
        [TestCase( "0.0.0-gamma", 1, "Normal = 1" )]
        [TestCase( "88.88.88-rc+Invalid", 2, "Invalid = 2" )]
        [TestCase( "88.88.88+Invalid", 2, "Marked Invalid = 2" )]
        public void equal_release_tags_can_have_different_definition_strengths( string tag, int level, string message )
        {
            var t = CSVersion.TryParse( tag );
            Assert.That( t.DefinitionStrength, Is.EqualTo( level ), message );
        }

        [TestCase( "0.0.0-alpha", false, 0 )]
        [TestCase( "0.0.0-alpha.0.1", false, 1 )]
        [TestCase( "0.0.0-alpha.0.2", false, 2 )]
        [TestCase( "0.0.0-alpha.99.99", false, 100 * 99 + 100 - 1 )]
        [TestCase( "0.0.0-beta", false, 100 * 99 + 100 )]
        [TestCase( "0.0.0-delta", false, 2 * (100 * 99 + 100) )]
        [TestCase( "0.0.0-rc", false, 7 * (100 * 99 + 100) )]
        [TestCase( "0.0.0-rc.99.99", false, 7 * (100 * 99 + 100) + 100 * 99 + 99 )]
        [TestCase( "0.0.0", false, 8 * 100 * 100 )]

        [TestCase( "0.0.1-alpha", false, (8 * 100 * 100) + 1 )]
        [TestCase( "0.0.1-alpha.0.1", false, ((8 * 100 * 100) + 1) + 1 )]
        [TestCase( "0.0.1-alpha.0.2", false, ((8 * 100 * 100) + 1) + 2 )]
        [TestCase( "0.0.1-alpha.99.99", false, ((8 * 100 * 100) + 1) + 100 * 99 + 100 - 1 )]
        [TestCase( "0.0.1-beta", false, ((8 * 100 * 100) + 1) + 100 * 99 + 100 )]
        [TestCase( "0.0.1-delta", false, ((8 * 100 * 100) + 1) + 2 * (100 * 99 + 100) )]
        [TestCase( "0.0.1-epsilon", false, ((8 * 100 * 100) + 1) + 3 * (100 * 99 + 100) )]
        [TestCase( "0.0.1-rc", false, ((8 * 100 * 100) + 1) + 7 * (100 * 99 + 100) )]
        [TestCase( "0.0.1-rc.99.99", false, ((8 * 100 * 100) + 1) + 7 * (100 * 99 + 100) + 100 * 99 + 99 )]
        [TestCase( "0.0.1", false, ((8 * 100 * 100) + 1) + 8 * 100 * 100 )]

        [TestCase( "99999.49999.9998", true, (8 * 100 * 100) + 1 )]
        [TestCase( "99999.49999.9999-prerelease", true, 2 * (100 * 99 + 100) )]
        [TestCase( "99999.49999.9999-prerelease.99.99", true, 100 * 99 + 100 + 1 )]
        [TestCase( "99999.49999.9999-rc", true, 100 * 99 + 100 )]
        [TestCase( "99999.49999.9999-rc.99.98", true, 2 )]
        [TestCase( "99999.49999.9999-rc.99.99", true, 1 )]
        [TestCase( "99999.49999.9999", true, 0 )]
        public void checking_extreme_version_ordering( string tag, bool atEnd, int expectedRank )
        {
            var tOrigin = CSVersion.TryParse( tag );
            Check( tOrigin );
            Check( tOrigin.IsLongForm ? tOrigin.ToNormalizedForm() : tOrigin.ToLongForm() );

            void Check( CSVersion t )
            {
                if( atEnd )
                {
                    Assert.That( t.OrderedVersion - (CSVersion.VeryLastVersion.OrderedVersion - expectedRank), Is.EqualTo( 0 ) );
                }
                else
                {
                    Assert.That( t.OrderedVersion - (CSVersion.VeryFirstVersion.OrderedVersion + expectedRank), Is.EqualTo( 0 ) );
                }
                var t2 = CSVersion.Create( t.OrderedVersion, t.IsLongForm );
                Assert.That( t2.ToString(), Is.EqualTo( t.ToString() ) );
                Assert.That( t.Equals( t2 ) );
            }
        }


        [Test]
        public void checking_version_ordering()
        {
            var orderedTags = new[]
            {
                    "0.0.0-alpha",
                    "0.0.0-alpha.0.1",
                    "0.0.0-alpha.0.2",
                    "0.0.0-alpha.1",
                    "0.0.0-alpha.1.1",
                    "0.0.0-beta",
                    "0.0.0-beta.1",
                    "0.0.0-beta.1.1",
                    "0.0.0-gamma",
                    "0.0.0-gamma.0.1",
                    "0.0.0-gamma.50",
                    "0.0.0-gamma.50.20",
                    "0.0.0-prerelease",
                    "0.0.0-prerelease.0.1",
                    "0.0.0-prerelease.2",
                    "0.0.0-rc",
                    "0.0.0-rc.0.1",
                    "0.0.0-rc.2",
                    "0.0.0-rc.2.58",
                    "0.0.0-rc.3",
                    "0.0.0",
                    "0.0.1",
                    "0.0.2",
                    "1.0.0-alpha",
                    "1.0.0-alpha.1",
                    "1.0.0-alpha.2",
                    "1.0.0-alpha.2.1",
                    "1.0.0-alpha.3",
                    "1.0.0",
                    "99999.49999.0",
                    "99999.49999.9999-alpha.99",
                    "99999.49999.9999-alpha.99.99",
                    "99999.49999.9999-rc",
                    "99999.49999.9999-rc.0.1",
                    "99999.49999.9999"
                };
            var releasedTags = orderedTags
                                        .Select( ( tag, idx ) => new { Tag = tag, Index = idx, ReleasedTag = CSVersion.TryParse( tag ) } )
                                        .Select( s => { Assert.That( s.ReleasedTag.IsValid, s.Tag ); return s; } );
            var orderedByFileVersion = releasedTags
                                        .OrderBy( s => s.ReleasedTag.OrderedVersion );
            var orderedByFileVersionParts = releasedTags
                                            .OrderBy( s => s.ReleasedTag.OrderedVersionMajor )
                                            .ThenBy( s => s.ReleasedTag.OrderedVersionMinor )
                                            .ThenBy( s => s.ReleasedTag.OrderedVersionBuild )
                                            .ThenBy( s => s.ReleasedTag.OrderedVersionRevision );

            Assert.That( orderedByFileVersion.Select( ( s, idx ) => s.Index - idx ).All( delta => delta == 0 ) );
            Assert.That( orderedByFileVersionParts.Select( ( s, idx ) => s.Index - idx ).All( delta => delta == 0 ) );
        }

        // A Major.0.0 can be reached from any major version below.
        // One can jump to any prerelease of it.
        [TestCase( "4.0.0, 4.0.0-alpha, 4.0.0-rc", true, "3.0.0, 3.5.44, 3.0.0-alpha, 3.49999.9999-rc.87, 3.0.3-rc.99.99, 3.0.3-alpha.54.99, 3.999.999" )]
        [TestCase( "4.1.0, 4.1.0-alpha, 4.1.0-rc", false, "3.0.0, 3.5.44, 3.0.0-alpha, 3.49999.9999-rc.87, 3.0.3-rc.99.99, 3.0.3-alpha.54.99, 3.999.999" )]

        // Same for a minor bump of 1.
        [TestCase( "4.3.0, 4.3.0-alpha, 4.3.0-rc", true, "4.2.0, 4.2.0-alpha, 4.2.44, 4.2.3-rc.87, 4.2.3-rc.99.99, 4.2.3-rc.5.8, 4.2.3-alpha, 4.2.3-alpha.54.99, 4.2.9999" )]
        [TestCase( "4.3.0, 4.3.0-rc", true, "4.3.0-alpha, 4.3.0-beta.99.99, 4.3.0-prerelease.99.99" )]

        // Patch differs: 
        [TestCase( "4.3.2", true, "4.3.1, 4.3.2-alpha, 4.3.2-rc, 4.3.2-rc.99.99" )]
        [TestCase( "4.3.2", false, "4.3.1-alpha, 4.3.1-rc, 4.3.1-rc.99.99" )]
        public void checking_some_versions_predecessors( string targets, bool previous, string candidates )
        {
            var targ = targets.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .Select( v => CSVersion.TryParse( v ) );
            var prev = candidates.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .Select( v => CSVersion.TryParse( v ) );
            foreach( var vTarget in targ )
            {
                foreach( var p in prev )
                {
                    Assert.That( vTarget.IsDirectPredecessor( p ), Is.EqualTo( previous ), p.ToString() + (previous ? " is a previous of " : " is NOT a previous of ") + vTarget.ToString() );
                }
            }
        }


        [TestCase( "0.0.0-a", "0.0.0-a00-01" )]
        [TestCase( "0.0.0-a-00-01", "0.0.0-a00-02" )]
        [TestCase( "0.0.0-r99", "0.0.0-r99-01" )]
        [TestCase( "0.0.0-r01-99", "" )]
        [TestCase( "0.0.0", "0.0.1-a, 0.0.1-b, 0.0.1-d, 0.0.1-e, 0.0.1-g, 0.0.1-k, 0.0.1-p, 0.0.1-r, 0.0.1" )]
        public void checking_next_fixes_and_predecessors( string start, string nextVersions )
        {
            var next = nextVersions.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .ToArray();
            var rStart = CSVersion.TryParse( start );
            Assert.That( rStart != null && rStart.IsValid );
            // Checks successors (and that they are ordered).
            var cNext = rStart.GetDirectSuccessors( true ).Select( v => v.ToString() ).ToArray();
            CollectionAssert.AreEqual( next, cNext, start + " => " + string.Join( ", ", cNext ) );
            Assert.That( rStart.GetDirectSuccessors( true ), Is.Ordered );
            // For each successor, check that the start is a predecessor.
            foreach( var n in rStart.GetDirectSuccessors( true ) )
            {
                Assert.That( n.IsDirectPredecessor( rStart ), "{0} < {1}", rStart, n );
            }
        }


        [TestCase( 1, 2, 1000 )]
        [TestCase( -1, 2, 1000 ), Description( "Random seed version." )]
        public void randomized_checking_of_ordered_versions_mapping_and_extended_successors_and_predecessors( int seed, int count, int span )
        {
            Random r = seed >= 0 ? new Random( seed ) : new Random();
            while( --count > 0 )
            {
                long start = (long)decimal.Ceiling( r.NextDecimal() * (CSVersion.VeryLastVersion.OrderedVersion + 1) + 1 );
                CSVersion rStart = CheckMapping( start );
                Assert.That( rStart, Is.Not.Null );
                CSVersion rCurrent;
                for( int i = 1; i < span; ++i )
                {
                    rCurrent = CheckMapping( start + i );
                    if( rCurrent == null ) break;
                    Assert.That( rStart < rCurrent );
                }
                for( int i = 1; i < span; ++i )
                {
                    rCurrent = CheckMapping( start - i );
                    if( rCurrent == null ) break;
                    Assert.That( rStart > rCurrent );
                }
            }
            //Console.WriteLine( "Greatest successors count = {0}.", _greatersuccessorCount );
        }

        //static int _greatersuccessorCount = 0;

        CSVersion CheckMapping( long v )
        {
            if( v < 0 || v > CSVersion.VeryLastVersion.OrderedVersion )
            {
                Assert.Throws<ArgumentException>( () => CSVersion.Create( v ) );
                return null;
            }
            var t = CSVersion.Create( v );
            Assert.That( (v == 0) == !t.IsValid );
            Assert.That( t.OrderedVersion, Is.EqualTo( v ) );
            var sSemVer = t.NormalizedText;
            var tSemVer = CSVersion.TryParse( sSemVer );
            var tNormalized = CSVersion.TryParse( t.ToString( CSVersionFormat.Normalized ) );
            Assert.That( tSemVer.OrderedVersion, Is.EqualTo( v ) );
            Assert.That( tNormalized.OrderedVersion, Is.EqualTo( v ) );
            Assert.That( tNormalized.Equals( t ) );
            Assert.That( tSemVer.Equals( t ) );
            Assert.That( tNormalized.Equals( (object)t ) );
            Assert.That( tSemVer.Equals( (object)t ) );
            Assert.That( tNormalized.CompareTo( t ) == 0 );
            Assert.That( tSemVer == t );
            Assert.That( tSemVer.ToString(), Is.EqualTo( t.ToString() ) );
            Assert.That( tNormalized.ToString(), Is.EqualTo( t.ToString() ) );
            // Successors/Predecessors check.
            var vSemVer = SVersion.Parse( sSemVer );
            int count = 0;
            foreach( var succ in t.GetDirectSuccessors( false ) )
            {
                ++count;
                Assert.That( succ.IsDirectPredecessor( t ) );
                var vSemVerSucc = SVersion.Parse( succ.NormalizedText );
                Assert.That( vSemVer < vSemVerSucc, "{0} < {1}", vSemVer, vSemVerSucc );
            }
            //if( count > _greatersuccessorCount )
            //{
            //    Console.WriteLine( " -> - found {0} successors for '{1}':", count, t );
            //    Console.WriteLine( "      " + string.Join( ", ", t.GetDirectSuccessors( false ).Select( s => s.ToString() ) ) );
            //    var closest = t.GetDirectSuccessors( true ).Select( s => s.ToString() ).ToList();
            //    Console.WriteLine( "    - {0} closest successors:", closest.Count, t );
            //    Console.WriteLine( "      " + string.Join( ", ", closest ) );
            //    _greatersuccessorCount = count;
            //}
            return t;
        }

        [Test]
        public void check_first_possible_versions()
        {
            string firstPossibleVersions = @"
                        0.0.0-a, 0.0.0-b, 0.0.0-d, 0.0.0-e, 0.0.0-g, 0.0.0-k, 0.0.0-p, 0.0.0-r, 
                        0.0.0, 
                        0.1.0-a, 0.1.0-b, 0.1.0-d, 0.1.0-e, 0.1.0-g, 0.1.0-k, 0.1.0-p, 0.1.0-r, 
                        0.1.0, 
                        1.0.0-a, 1.0.0-b, 1.0.0-d, 1.0.0-e, 1.0.0-g, 1.0.0-k, 1.0.0-p, 1.0.0-r, 
                        1.0.0";
            var next = firstPossibleVersions.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .ToArray();
            CollectionAssert.AreEqual( next, CSVersion.FirstPossibleVersions.Select( v => v.ToString() ).ToArray() );
        }

        [Test]
        public void operators_overloads()
        {
            // Two variables to avoid Compiler Warning (level 3) CS1718
            CSVersion null2 = null;
            CSVersion null1 = null;

            Assert.That( null1 == null2 );
            Assert.That( null1 >= null2 );
            Assert.That( null1 <= null2 );

            Assert.That( null1 != null2, Is.False );
            Assert.That( null1 > null2, Is.False );
            Assert.That( null1 < null2, Is.False );

            NullIsAlwaysSmaller( CSVersion.VeryFirstVersion );
            NullIsAlwaysSmaller( CSVersion.TryParse( "1.0.0" ) );
            NullIsAlwaysSmaller( CSVersion.TryParse( "bug" ) );
        }

        private static void NullIsAlwaysSmaller( CSVersion v )
        {
            Assert.That( null != v );
            Assert.That( null == v, Is.False );
            Assert.That( null >= v, Is.False );
            Assert.That( null <= v );
            Assert.That( null > v, Is.False );
            Assert.That( null < v );

            Assert.That( v != null );
            Assert.That( v == null, Is.False );
            Assert.That( v >= null );
            Assert.That( v <= null, Is.False );
            Assert.That( v > null );
            Assert.That( v < null, Is.False );
        }
    }
}

