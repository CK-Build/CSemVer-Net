using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;
using System.IO;
using CSemVer;

namespace CSemVer.Tests
{
    [TestFixture]
    public class CIBuildNameTests
    {

        [Explicit]
        [TestCase( "v0.4.1-rc.2.1", "0.4.1-rc.2.2, 0.4.1-rc.3, 0.4.2" )]
        [TestCase( "v3.2.1-rc.1", "3.2.1-rc.1.1, 3.2.1-rc.2, 3.2.2" )]
        [TestCase( "v3.2.1-beta", "3.2.1-beta.1, 3.2.1-beta.0.1, 3.2.1-chi, 3.2.1-beta.5, 3.2.2" )]
        [TestCase( "v1.2.3", "1.2.4-alpha, 1.2.4-alpha.0.1, 1.2.4" )]
        public void display_versions_and_CI_version( string version, string after )
        {
            var buildInfo = new CIBuildDescriptor() { BranchName = "develop", BuildIndex = 15 };
            CSVersion v = CSVersion.TryParse( version );
            string vCI = v.ToString( CSVersionFormat.Normalized, buildInfo );
            CSVersion vNext = CSVersion.Create( v.OrderedVersion + 1 );

            Console.WriteLine( "Version = {0}, CI = {1}, Next = {2}", v, vCI, vNext );

            var vSemVer = SVersion.Parse( v.ToString( CSVersionFormat.Normalized ) );
            var vCISemVer = SVersion.Parse( vCI );
            var vNextSemVer = SVersion.Parse( vNext.ToString( CSVersionFormat.Normalized ) );
            Assert.That( vSemVer < vCISemVer, "{0} < {1}", vSemVer, vCISemVer );
            Assert.That( vCISemVer < vNextSemVer, "{0} < {1}", vCISemVer, vNextSemVer );

            foreach( var vAfter in after.Split( ',' ).Select( s => SVersion.Parse( s.Trim() ) ) )
            {
                Assert.That( vAfter.CompareTo( vCISemVer ) > 0, "{0} > {1}", vAfter, vCISemVer );
            }
        }


        [TestCase( "1.0.0" )]
        [TestCase( "1.0.0-a" )]
        [TestCase( "1.0.0-a000-01" )]
        [TestCase( "1.0.0-a001" )]
        [TestCase( "1.0.0-a003" )]
        [TestCase( "1.0.0-a003-04" )]
        [TestCase( "1.0.0-p004-05" )]
        [TestCase( "1.0.0-r199-99" )]
        [TestCase( "1.0.1" )]
        [TestCase( "1.0.9999" )]
        public void CIBuildVersion_LastReleaseBased_are_correctely_ordered( string tag )
        {
            var t = CSVersion.Parse( tag );
            t.IsLongForm.Should().BeFalse();
            var v = t.ToLongForm();
            v.NormalizedText.Should().Be( SVersion.Parse( t.ToString( CSVersionFormat.LongForm ) ).NormalizedText );

            var tNext = CSVersion.Create( t.OrderedVersion + 1 );
            var vNext = tNext.ToLongForm();
            var tPrev = CSVersion.Create( t.OrderedVersion - 1 );
            var vPrev = tPrev.ToLongForm();

            void CheckLower( SVersion v1, SVersion v2 )
            {
                Assert.That( v1 < v2, "{0} < {1}", v1, v2 );
                Assert.That( v2 > v1, "{0} > {1}", v2, v1 );

                SVersion v1low = SVersion.Parse( v1.NormalizedText.ToLowerInvariant() );
                SVersion v2low = SVersion.Parse( v2.NormalizedText.ToLowerInvariant() );
                Assert.That( v1low < v2low, "{0} < {1} (lowercase)", v1low, v2low );
                Assert.That( v2low > v1low, "{0} > {1} (lowercase)", v2low, v1low );

                SVersion v1up = SVersion.Parse( v1.NormalizedText.ToUpperInvariant() );
                SVersion v2up = SVersion.Parse( v2.NormalizedText.ToUpperInvariant() );
                Assert.That( v1up < v2up, "{0} < {1} (uppercase)", v1up, v2up );
                Assert.That( v2up > v1up, "{0} > {1} (uppercase)", v2up, v1up );
            }

            CheckLower( vPrev, v );
            CheckLower( v, vNext );

            var sNuGet = t.ToString( CSVersionFormat.Normalized );
            var sNuGetPrev = tPrev.ToString( CSVersionFormat.Normalized );
            var sNuGetNext = tNext.ToString( CSVersionFormat.Normalized );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetPrev, sNuGet ) < 0, "{0} < {1}", sNuGetPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetNext ) < 0, "{0} < {1}", sNuGet, sNuGetNext );


            CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = "dev", BuildIndex = 1 };

            string sCI = t.ToString( CSVersionFormat.LongForm, ci );
            SVersion vCi = SVersion.Parse( sCI );
            CheckLower( v, vCi );
            CheckLower( vCi, vNext );

            var sNuGetCI = t.ToString( CSVersionFormat.Normalized, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetCI ) < 0, "{0} < {1}", sNuGet, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCI, sNuGetNext ) < 0, "{0} < {1}", sNuGetCI, sNuGetNext );

            string sCiNext = tNext.ToString( CSVersionFormat.LongForm, ci );
            SVersion vCiNext = SVersion.Parse( sCiNext );
            CheckLower( vCi, vCiNext );
            CheckLower( vNext, vCiNext );

            var sNuGetCINext = tNext.ToString( CSVersionFormat.Normalized, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetCI ) > 0, "{0} > {1}", sNuGetCINext, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetNext ) > 0, "{0} > {1}", sNuGetCINext, sNuGetNext );

            string sCiPrev = tPrev.ToString( CSVersionFormat.LongForm, ci );
            SVersion vCiPrev = SVersion.Parse( sCiPrev );
            CheckLower( vPrev, vCiPrev );
            CheckLower( vCiPrev, v );
            CheckLower( vCiPrev, vCiNext );

            var sNuGetCIPrev = tPrev.ToString( CSVersionFormat.Normalized, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetPrev ) > 0, "{0} > {1}", sNuGetCIPrev, sNuGetPrev );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGet ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetCINext ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGetCINext );
        }

        [Test]
        public void testing_cibuild_timebased()
        {
            var now = DateTime.UtcNow;
            var more = now.AddSeconds( 1 );
            {
                var sV = CIBuildDescriptor.CreateLongFormZeroTimed( "develop", now );
                var v = SVersion.Parse( sV );
                v.AsCSVersion.Should().BeNull();

                var vMore = SVersion.Parse( CIBuildDescriptor.CreateLongFormZeroTimed( "develop", more ) );
                vMore.Should().BeGreaterThan( v );
            }
            {
                var sV = CIBuildDescriptor.CreateShortFormZeroTimed( "develop", now );
                var v = SVersion.Parse( sV );
                v.AsCSVersion.Should().BeNull();

                var vMore = SVersion.Parse( CIBuildDescriptor.CreateShortFormZeroTimed( "develop", more ) );
                vMore.Should().BeGreaterThan( v );
            }
        }
    }
}
