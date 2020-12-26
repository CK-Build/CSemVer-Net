using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSemVer.Tests
{
    [TestFixture]
    public class SVersionBoundTests
    {
        static readonly SVersion V100 = SVersion.Create( 1, 0, 0 );
        static readonly SVersion V101 = SVersion.Create( 1, 0, 1 );
        static readonly SVersion V110 = SVersion.Create( 1, 1, 0 );
        static readonly SVersion V111 = SVersion.Create( 1, 1, 1 );
        static readonly SVersion V120 = SVersion.Create( 1, 2, 0 );
        static readonly SVersion V121 = SVersion.Create( 1, 2, 1 );
        static readonly SVersion V200 = SVersion.Create( 2, 0, 0 );
        static readonly SVersion V201 = SVersion.Create( 2, 0, 1 );
        static readonly SVersion V210 = SVersion.Create( 2, 1, 0 );
        static readonly SVersion V211 = SVersion.Create( 2, 1, 1 );

        [Test]
        public void basic_union_operations()
        {
            SVersionBound.None.Union( SVersionBound.None ).Should().Be( SVersionBound.None );
            SVersionBound.None.Union( SVersionBound.All ).Should().Be( SVersionBound.All );
            SVersionBound.All.Union( SVersionBound.None ).Should().Be( SVersionBound.All );

            var b1 = new SVersionBound( CSVersion.VeryFirstVersion, SVersionLock.None, PackageQuality.None );
            var b2 = new SVersionBound( CSVersion.VeryLastVersion, SVersionLock.None, PackageQuality.None );

            SVersionBound.None.Union( b1 ).Should().Be( b1 );
            b1.Union( SVersionBound.None ).Should().Be( b1 );

            SVersionBound.None.Union( b2 ).Should().Be( b2 );
            b2.Union( SVersionBound.None ).Should().Be( b2 );

            b1.Contains( b2 ).Should().BeTrue( "VeryFirstVersion bound contains VeryLastVersion bound." );
            b2.Contains( b1 ).Should().BeFalse( "VeryLastVersion bound doen't contain VeryFirstVersion." );

            b1.Union( b2 ).Should().Be( b1 );
            b2.Union( b1 ).Should().Be( b1 );
        }

        [Test]
        public void basic_intersect_operations()
        {
            SVersionBound.None.Intersect( SVersionBound.None ).Should().Be( SVersionBound.None );
            SVersionBound.None.Intersect( SVersionBound.All ).Should().Be( SVersionBound.None );
            SVersionBound.All.Intersect( SVersionBound.None ).Should().Be( SVersionBound.None );

            var b1 = new SVersionBound( CSVersion.VeryFirstVersion, SVersionLock.None, PackageQuality.None );
            var b2 = new SVersionBound( CSVersion.VeryLastVersion, SVersionLock.None, PackageQuality.None );

            b1.Intersect( b1 ).Should().Be( b1 );
            SVersionBound.None.Intersect( b1 ).Should().Be( SVersionBound.None );
            b1.Intersect( SVersionBound.None ).Should().Be( SVersionBound.None );
            b1.Intersect( SVersionBound.All ).Should().Be( b1 );
            SVersionBound.All.Intersect( b1 ).Should().Be( b1 );

            b2.Intersect( b2 ).Should().Be( b2 );
            SVersionBound.None.Intersect( b2 ).Should().Be( SVersionBound.None );
            b2.Intersect( SVersionBound.None ).Should().Be( SVersionBound.None );
            b2.Intersect( SVersionBound.All ).Should().Be( b2 );
            SVersionBound.All.Intersect( b2 ).Should().Be( b2 );

            b1.Intersect( b2 ).Should().Be( b2 );
            b2.Intersect( b1 ).Should().Be( b2 );
        }

        [Test]
        public void partial_ordering_only()
        {
            var b1 = new SVersionBound( V100, SVersionLock.None, PackageQuality.Preview );
            var b11 = new SVersionBound( V110, SVersionLock.None, PackageQuality.None );

            b1.Contains( b11 ).Should().BeFalse( "b1 only accepts preview and b11 accepts everything." );
            b11.Contains( b1 ).Should().BeFalse( "b11.Base version is greater than b1.Base version." );

            var u = b1.Union( b11 );
            b11.Union( b1 ).Should().Be( u );

            u.Contains( b1 ).Should().BeTrue();
            u.Contains( b11 ).Should().BeTrue();

            var i = b1.Intersect( b11 );
            b11.Intersect( b1 ).Should().Be( i );
            i.Contains( b1 ).Should().BeFalse();
            i.Contains( b11 ).Should().BeFalse();
        }

        [Test]
        public void SVersionLock_tests()
        {
            var b1LockMinor = new SVersionBound( V100, SVersionLock.LockMinor, PackageQuality.None );
            b1LockMinor.Satisfy( V100 ).Should().BeTrue( "Same as the base version." );
            b1LockMinor.Satisfy( V101 ).Should().BeTrue( "The patch can increase." );
            b1LockMinor.Satisfy( V110 ).Should().BeFalse( "The minor is locked." );
            b1LockMinor.Satisfy( V200 ).Should().BeFalse( "Major is of course also locked." );

            var b11 = new SVersionBound( V110, SVersionLock.LockMajor, PackageQuality.None );
            b11.Satisfy( V100 ).Should().BeFalse( "Cannot downgrade minor." );
            b11.Satisfy( V110 ).Should().BeTrue();
            b11.Satisfy( V111 ).Should().BeTrue();
            b11.Satisfy( V200 ).Should().BeFalse( "Cannot upgrade major." );

            var b1LockMajor = b1LockMinor.SetLock( SVersionLock.LockMajor );
            b1LockMajor.Contains( b1LockMinor ).Should().BeTrue();
            b1LockMajor.Contains( b11 ).Should().BeTrue( "Same major is locked." );

            var b2 = new SVersionBound( V200, SVersionLock.Lock, PackageQuality.None );
            b1LockMinor.Contains( b2 ).Should().BeFalse();
            b1LockMajor.Contains( b2 ).Should().BeFalse();
        }

        [Test]
        public void union_with_lock_and_MinQuality()
        {
            var b10 = new SVersionBound( V100, SVersionLock.LockMinor, PackageQuality.None );
            var b11 = new SVersionBound( V110, SVersionLock.LockMajor, PackageQuality.Stable );

            b10.Contains( b11 ).Should().BeFalse( "The 1.0 minor is locked." );
            b11.Contains( b10 ).Should().BeFalse( "The 1.1 base version is greater than the 1.0 base version." );

            var u = b10.Union( b11 );
            u.Should().Be( b11.Union( b10 ) );

            u.Base.Should().Be( SVersion.Create( 1, 0, 0 ) );
            u.Lock.Should().Be( SVersionLock.LockMajor );
            u.MinQuality.Should().Be( PackageQuality.CI );

            var b21 = new SVersionBound( V210, SVersionLock.LockMajor, PackageQuality.Exploratory );

            var u2 = b21.Union( b11 );
            u2.Should().Be( b11.Union( b21 ) );

            u2.Base.Should().Be( SVersion.Create( 1, 1, 0 ) );
            u2.Lock.Should().Be( SVersionLock.LockMajor );
            u2.MinQuality.Should().Be( PackageQuality.Exploratory );
        }

        // Based on: https://github.com/npm/node-semver#advanced-range-syntax.
        // See also: https://semver.npmjs.com/.

        // Syntax: "1.2.3 - 2.3" ==> ">=1.2.3 <2.4.0-0".
        //          We approximate this with 1.2.3. 
        [TestCase( "1.2.3 - 2.3.4", "includePrerelease", "1.2.3", "Approx" )]
        [TestCase( "1.2.3 - 2.3.4", "", "1.2.3[Stable]", "Approx" )]

        [TestCase( "1.2 - 2.3.4", "", "1.2.0[Stable]", "Approx" )]

        // Syntax: "*" or "" is >=0.0.0 (Any version satisfies). 
        //         No approximation here (when includePrerelease is true). 
        [TestCase( "*", "", "0.0.0[Stable]", "Approx" )]
        [TestCase( "", "includePrerelease", "0.0.0", "" )]

        // Syntax: "1.x" is ">=1.0.0 <2.0.0-0" (Matching major version).
        //         No approximation here (when includePrerelease is true). 
        [TestCase( "1.x", "", "1.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "1.X", "", "1.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "1.2.x", "", "1.2.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "1.2.X", "", "1.2.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "1.X", "includePrerelease", "1.0.0[LockMajor,CI]", "" )]
        [TestCase( "1.2.X", "includePrerelease", "1.2.0[LockMinor,CI]", "" )]

        // Syntax: "A partial version range is treated as an X-Range, so the special character is in fact optional."
        //         No approximation here (when includePrerelease is true). 
        [TestCase( "1", "", "1.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "1.2", "", "1.2.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "1", "includePrerelease", "1.0.0[LockMajor,CI]", "" )]
        [TestCase( "1.2", "includePrerelease", "1.2.0[LockMinor,CI]", "" )]

        // Syntax: Tilde Ranges
        //         Allows patch-level changes if a minor version is specified on the comparator. Allows minor-level changes if not.
        //
        //         "~1.2.3" is ">=1.2.3 <1.(2+1).0", that is ">=1.2.3 <1.3.0-0"
        //         This is not an approximation (when includePrerelease is true): this locks the minor.
        [TestCase( "~1.2.3", "", "1.2.3[LockMinor,Stable]", "Approx" )]
        [TestCase( "~1.2.3", "includePrerelease", "1.2.3[LockMinor,CI]", "" )]

        //         "~1.2" is ">=1.2.0 <1.(2+1).0", that is ">=1.2.0 <1.3.0-0"
        //         This is not an approximation (when includePrerelease is true): this locks the minor.
        [TestCase( "~1.2", "", "1.2.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "~1.2", "includePrerelease", "1.2.0[LockMinor,CI]", "" )]

        //         "~1" is ">=1.0.0 <(1+1).0.0" that is ">=1.0.0 <2.0.0-0" (Same as 1.x)
        //         This is not an approximation: this locks the major.
        [TestCase( "~1", "", "1.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "~1", "includePrerelease", "1.0.0[LockMajor,CI]", "" )]

        //         "~0.2.3" is ">=0.2.3 <0.(2+1).0" that is ">=0.2.3 <0.3.0-0"
        //         This is not an approximation (when includePrerelease is true): this locks the minor.
        [TestCase( "~0.2.3", "", "0.2.3[LockMinor,Stable]", "Approx" )]
        [TestCase( "~0.2.3", "includePrerelease", "0.2.3[LockMinor,CI]", "" )]

        //         "~0.2" is ">=0.2.0 <0.(2+1).0" that is ">=0.2.0 <0.3.0-0" (Same as 0.2.x)
        //         This is not an approximation: this locks the minor.
        [TestCase( "~0.2", "", "0.2.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "~0.2", "includePrerelease", "0.2.0[LockMinor,CI]", "" )]

        //         "~0" is ">=0.0.0 <(0+1).0.0" that is ">=0.0.0 <1.0.0-0" (Same as 0.x)
        //         This is not an approximation (when includePrerelease is true): this locks the major.
        [TestCase( "~0", "", "0.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "~0", "includePrerelease", "0.0.0[LockMajor,CI]", "" )]

        //         "~1.2.3-beta.2" is ">=1.2.3-beta.2 <1.3.0-0"
        //         This is NEVER an approximation!
        //         Even if for npm:
        //            "For example, the range >1.2.3-alpha.3 would be allowed to match the version 1.2.3-alpha.7, but it
        //             would not be satisfied by 3.4.5-alpha.9, even though 3.4.5-alpha.9 is technically "greater than"
        //             1.2.3-alpha.3 according to the SemVer sort rules. The version range only accepts prerelease tags
        //             on the 1.2.3 version. The version 3.4.5 would satisfy the range, because it does not have a prerelease
        //             flag, and 3.4.5 is greater than 1.2.3-alpha.7."
        //         
        //         We lock the patch and the MinQuality is automatically set to CI (any prerelease satisfies)
        //         even if includePrerelease is not specified: this is exactly the npm way of working.
        //
        [TestCase( "~1.2.3-beta.2", "", "1.2.3-beta.2[LockPatch,CI]", "" )]
        [TestCase( "~1.2.3-beta.2", "includePrerelease", "1.2.3-beta.2[LockPatch,CI]", "" )]

        // Syntax: Caret Ranges.
        //
        //         "^1.2.3" is ">=1.2.3 <2.0.0-0"
        //         This is not an approximation (when includePrerelease is true): this locks the major.
        [TestCase( "^1.2.3", "", "1.2.3[LockMajor,Stable]", "Approx" )]
        [TestCase( "^1.2.3", "includePrerelease", "1.2.3[LockMajor,CI]", "" )]

        //         "^0.2.3" is ">=0.2.3 <0.3.0-0"
        //         This is not an approximation (when includePrerelease is true): this locks the minor (because the major is 0).
        [TestCase( "^0.2.3", "", "0.2.3[LockMinor,Stable]", "Approx" )]
        [TestCase( "^0.2.3", "includePrerelease", "0.2.3[LockMinor,CI]", "" )]

        //         "^0.0.3" is ">=0.0.3 <0.0.4-0"
        //         This is NEVER an approximation: this locks the whole version OR authorizes prerelease (when includePrerelease is true).
        [TestCase( "^0.0.3", "", "0.0.3[Lock]", "" )]
        [TestCase( "^0.0.3", "includePrerelease", "0.0.3[LockPatch,CI]", "" )]

        //        "^1.2.3-beta.2" is ">=1.2.3-beta.2 <2.0.0-0"
        //         This is not an approximation (when includePrerelease is true): this locks the major and allows CI build, but
        //         this is still an approximation when includePrerelease is false because prereleases for a different [major, minor, patch]
        //         are fobidden by npm.
        [TestCase( "^1.2.3-beta.2", "", "1.2.3-beta.2[LockMajor,CI]", "Approx" )]
        [TestCase( "^1.2.3-beta.2", "includePrerelease", "1.2.3-beta.2[LockMajor,CI]", "" )]

        //        "^0.0.3-beta" is ">=0.0.3-beta <0.0.4-0"
        //         This is NEVER an approximation.
        [TestCase( "^0.0.3-beta", "", "0.0.3-beta[LockPatch,CI]", "" )]
        [TestCase( "^0.0.3-beta", "includePrerelease", "0.0.3-beta[LockPatch,CI]", "" )]

        //        "^1.2.x" is ">=1.2.0 <2.0.0-0"
        //         This is not an approximation (when includePrerelease is true).
        [TestCase( "^1.2.x", "", "1.2.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "^1.2.x", "includePrerelease", "1.2.0[LockMajor,CI]", "" )]

        //        "^0.0.x" is ">=0.0.0 <0.1.0-0"
        [TestCase( "^0.0.x", "", "0.0.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "^0.0.x", "includePrerelease", "0.0.0[LockMinor,CI]", "" )]

        //        "^0.0" is ">=0.0.0 <0.1.0-0"
        [TestCase( "^0.0", "", "0.0.0[LockMinor,Stable]", "Approx" )]
        [TestCase( "^0.0", "includePrerelease", "0.0.0[LockMinor,CI]", "" )]

        //        "^1.x" is ">=1.0.0 <2.0.0-0"
        [TestCase( "^1.x", "", "1.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "^1.x", "includePrerelease", "1.0.0[LockMajor,CI]", "" )]

        //        "^0.x" is ">=0.0.0 <1.0.0-0"
        [TestCase( "^0.x", "", "0.0.0[LockMajor,Stable]", "Approx" )]
        [TestCase( "^0.x", "includePrerelease", "0.0.0[LockMajor,CI]", "" )]

        // Syntax: ">=1.2.9 <2.0.0" is approximated.
        [TestCase( ">=1.2.9 <2.0.0", "", "1.2.9[Stable]", "Approx" )]
        [TestCase( ">=1.2.9 <2.0.0", "includePrerelease", "1.2.9", "Approx" )]

        // Syntax: "1.2.7 || >=1.1.9 <2.0.0" is approximated.
        [TestCase( "1.2.7 || >=1.1.9 <2.0.0", "", "1.1.9[Stable]", "Approx" )]
        [TestCase( "1.2.7 || >=1.1.9 <2.0.0", "includePrerelease", "1.1.9", "Approx" )]

        // Syntax: "<1.2.7" is ignored.
        [TestCase( "<1.2.7", "", "0.0.0-0[Stable]", "Approx" )]
        [TestCase( "<1.2.7", "includePrerelease", "0.0.0-0", "Approx" )]

        // Syntax: "<=1.2.7" is like "=1.2.7".
        [TestCase( "<=1.2.7", "", "1.2.7[Lock]", "Approx" )]
        [TestCase( "<=1.2.7", "includePrerelease", "1.2.7[Lock]", "Approx" )]

        public void parse_npm_syntax( string p, string includePrerelease, string expected, string approximate )
        {
            var r = SVersionBound.NpmTryParse( p, includePrerelease == "includePrerelease" );
            r.Error.Should().BeNull();
            r.Result.ToString().Should().Be( expected );
            r.IsApproximated.Should().Be( approximate == "Approx" );
        }

        [TestCase( "nimp" )]
        [TestCase( "<" )]
        [TestCase( "<=>" )]
        [TestCase( "  <2.0.5 || >" )]
        public void parse_npm_syntax_error( string p )
        {
            var r = SVersionBound.NpmTryParse( p );
            r.IsValid.Should().BeFalse();
            r.Result.Should().Be( SVersionBound.None );
        }

        [TestCase( "1.2.3.4 - 2.0.0-0", "1.2.3" )]
        [TestCase( "1.2.3.4-alpha - 3", "1.2.3" )]
        [TestCase( "9.8.7.6-alpha || 5.0", "5.0.0[LockMinor,CI]" )]
        public void parse_npm_with_fourth_part_skips_parts_and_prerelease( string p, string expected )
        {
            ReadOnlySpan<char> head = p;
            var r = SVersionBound.NpmTryParse( ref head );
            r.Error.Should().BeNull();
            r.Result.ToString().Should().Be( expected );
            r.FourthPartLost.Should().BeTrue();
            head.Length.Should().Be( 0 );
        }


        // Syntax from: https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges.

        // 1.0 -- x ≥ 1.0 -- Minimum version, inclusive
        //      Basic version: any greater version statisfies.
        [TestCase( "1", "1.0.0", "" )]
        [TestCase( "1.0", "1.0.0", "" )]
        [TestCase( "1.0.0", "1.0.0", "" )]

        // (1.0,) -- x > 1.0 -- Minimum version, exclusive
        //      We can only approximate this by ignoring the exclusive bound.
        //      We ignore the notion of "exclusive lower bound" (see below): this is not an approximation.

        [TestCase( "(1.0.0,)", "1.0.0", "" )]
        [TestCase( " ( 1.0.0, ) ", "1.0.0", "" )]

        // [1.0] -- x == 1.0 -- Exact version match
        //      This is a locked version.
        [TestCase( "[1.0.0]", "1.0.0[Lock]", "" )]

        // (,1.0] -- x ≤ 2.0 -- Maximum version, inclusive
        //      We (badly) approximate this with the lower bound... here it's the very first SemVer version.
        [TestCase( "(,2.0]", "0.0.0-0", "Approx" )]

        // (,1.0) -- x < 2.0 -- Maximum version, exclusive
        //      Same as above since we ignore the notion of "exclusive lower bound" (see below).
        [TestCase( "(,2.0)", "0.0.0-0", "Approx" )]

        // [1.0,2.0] -- 1.0 ≤ x ≤ 2.0 -- Exact range, inclusive
        //      We approximate this with the lower bound.
        //      
        [TestCase( "[1.0,2.0]", "1.0.0", "Approx" )]

        // (1.0,2.0) -- 1.0 < x < 2.0 -- Exact range, exclusive
        //      We approximate this with the lower bound.
        //      
        [TestCase( "(6,8)", "6.0.0", "Approx" )]

        // [1.0,2.0) -- 1.0 ≤ x < 2.0 -- Mixed inclusive minimum and exclusive maximum version
        //      We generally approximate this with the lower bound, but in this special case,
        //      we can capture the intent of the user by locking the major, the minor or the patch.
        //
        //      Note that Nuget is somehow buggy (https://github.com/NuGet/Home/issues/6434#issuecomment-546423937) since
        //      this allows 1.0.0-pre to be satisfied!
        //      The workaround is to use 1.0.0-0 as the upper bound... BUT beware: nuget.org forbids the -0 suffix :).
        //      To overcome this, if CSemVer is used (or the first prerelease always used is a[lpha]), one can use 1.0.0-a as the upper bound.
        //
        //      Here we consider that the answer IS to lock parts...
        //
        [TestCase( "[1,2)", "1.0.0[LockMajor,CI]", "" )]
        [TestCase( "[1.2,1.3)", "1.2.0[LockMinor,CI]", "" )]
        [TestCase( "[1.2.3,1.2.4)", "1.2.3[LockPatch,CI]", "" )]
        [TestCase( "[1.2.3,2)", "1.2.3[LockMajor,CI]", "" )]

        //       To be consistent, if the upper bound is a -0 (oar -a) prerelease, we do the same (and, at least for -0,
        //       this is perfect projection).
        [TestCase( "[1.2.3,1.2.4-0)", "1.2.3[LockPatch,CI]", "" )]
        [TestCase( "[1.2.3,2.0.0-a)", "1.2.3[LockMajor,CI]", "" )]
        [TestCase( "[1.2.3,2.0.0-A)", "1.2.3[LockMajor,CI]", "" )]

        //      About exclusive lower bound: this doesn't make a lot of sense... That would mean that yo release a package
        //      that depends on a package "A" (so you necessarily use a given verision of it: "vBase") and say: "I can't work with the
        //      package "A" in version "vBase". I need a future version... Funny isn't it?
        //      ==> We decide to consider '(' as being '[': all that applies before works and we consider that this is NOT an approximation. 
        //      
        [TestCase( "(1,2)", "1.0.0[LockMajor,CI]", "" )]
        [TestCase( "(1.2,1.3)", "1.2.0[LockMinor,CI]", "" )]
        [TestCase( "(1.2.3,1.2.4)", "1.2.3[LockPatch,CI]", "" )]
        [TestCase( "(1.2.3,2)", "1.2.3[LockMajor,CI]", "" )]
        [TestCase( "(1.2.3,1.2.4-0)", "1.2.3[LockPatch,CI]", "" )]
        [TestCase( "(1.2.3,2.0.0-a)", "1.2.3[LockMajor,CI]", "" )]
        [TestCase( "(1.2.3,2.0.0-A)", "1.2.3[LockMajor,CI]", "" )]

        //      When no lower bound is specified and the upper bound is 1.0.0, this is not an approximation.
        [TestCase( "(,1)", "0.0.0-0[LockMajor,CI]", "" )]
        [TestCase( " [ , 1 ) ", "0.0.0-0[LockMajor,CI]", "" )]

        // However, when a prerelease is specified on the upper bound, we cannot be clever anymore...
        [TestCase( "[1.2.3,2.0.0-alpha)", "1.2.3", "Approx" )]

        public void parse_nuget_syntax( string p, string expected, string approximate )
        {
            var r = SVersionBound.NugetTryParse( p );
            r.Error.Should().BeNull();
            r.Result.ToString().Should().Be( expected );
            r.IsApproximated.Should().Be( approximate == "Approx" );
            r.FourthPartLost.Should().BeFalse();
        }

        // (1.0) is invalid
        [TestCase( "(1.0)" )]
        [TestCase( "[ 1.0," )]
        [TestCase( "(" )]
        [TestCase( "(," )]
        [TestCase( "(,)" )]
        [TestCase( "()" )]
        [TestCase( "[]" )]
        [TestCase( "" )]
        public void parse_nuget_syntax_error( string p )
        {
            var r = SVersionBound.NugetTryParse( p );
            r.IsValid.Should().BeFalse();
            r.Error.Should().NotBeNull();
        }


        [TestCase( "[1.2.3.4]", "1.2.3[Lock]" )]
        [TestCase( "1.2.3.4-alpha", "1.2.3" )]
        [TestCase( "[1.2.3.4-alpha,2)", "1.2.3[LockMajor,CI]" )]
        public void parse_nuget_with_fourth_part( string p, string expected )
        {
            var r = SVersionBound.NugetTryParse( p );
            r.Error.Should().BeNull();
            r.Result.ToString().Should().Be( expected );
            r.IsApproximated.Should().BeFalse();
            r.FourthPartLost.Should().BeTrue();
        }

        [TestCase( "v1.0.0-mmm", "1.0.0-mmm" )]
        [TestCase( "v1.0.0-x[]", "1.0.0-x" )]
        [TestCase( "v1.0.0-x[CI,LockedPatch]", "1.0.0-x[LockPatch,CI]" )]
        [TestCase( "v1.2.3-xx [ LockMinor ] ", "1.2.3-xx[LockMinor,CI]" )]
        [TestCase( " v1.2.3 [ Stable , LockMajor ] ", "1.2.3[LockMajor,Stable]" )]
        [TestCase( " v1.2.3-xxx [ Preview , LockMajor ] ", "1.2.3-xxx[LockMajor,Preview]" )]
        [TestCase( " v1.2.3-AAA [ Preview , Lock ] ", "1.2.3-AAA[Lock]" )]
        [TestCase( " v1.2.3-AAA [ None , Locked ] ", "1.2.3-AAA[Lock]" )]
        [TestCase( " v1.2.3-AAA [ None , None ] ", "1.2.3-AAA" )]
        public void parse_SVersionBound( string p, string expected )
        {
            SVersionBound.TryParse( p, out var b ).Should().BeTrue();
            b.ToString().Should().Be( expected );
        }

        [TestCase( "v1.0.0-mmm", "1.0.0-mmm[LockMinor,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[]", "1.0.0-mmm[LockMinor,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[None]", "1.0.0-mmm[LockMinor,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[None,None]", "1.0.0-mmm[LockMinor,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[None,CI]", "1.0.0-mmm[LockMinor,CI]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[CI]", "1.0.0-mmm[LockMinor,CI]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[CI,None]", "1.0.0-mmm[LockMinor,CI]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[LockPatch]", "1.0.0-mmm[LockPatch,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[LockPatch,Exploratory]", "1.0.0-mmm[LockPatch,Exploratory]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[LockPatch,None]", "1.0.0-mmm[LockPatch,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        [TestCase( "v1.0.0-mmm[None,LockPatch]", "1.0.0-mmm[LockPatch,Preview]", SVersionLock.LockMinor, PackageQuality.Preview )]
        public void parse_SVersionBound_with_defaults( string p, string expected, SVersionLock defL, PackageQuality defQ )
        {
            SVersionBound.TryParse( p, out var b, defL, defQ ).Should().BeTrue();
            b.ToString().Should().Be( expected );
        }

    }
}
