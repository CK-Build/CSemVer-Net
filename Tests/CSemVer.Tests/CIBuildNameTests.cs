using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
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
        [TestCase( "1.0.0-alpha" )]
        [TestCase( "1.0.0-alpha.0.1" )]
        [TestCase( "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha.3" )]
        [TestCase( "1.0.0-alpha.3.4" )]
        [TestCase( "1.0.0-epsilon.4.5" )]
        [TestCase( "1.0.0-rc.99.99" )]
        [TestCase( "1.0.1" )]
        [TestCase( "1.0.9999" )]
        public void CIBuildVersion_LastReleaseBased_are_correctely_ordered( string tag )
        {
            var t = CSVersion.TryParse( tag );
            var v = SVersion.Parse( t.ToString( CSVersionFormat.Normalized ) );
            var tNext = CSVersion.Create( t.OrderedVersion + 1 );
            var vNext = SVersion.Parse( tNext.ToString( CSVersionFormat.Normalized ) );
            var tPrev = CSVersion.Create( t.OrderedVersion - 1 );
            var vPrev = SVersion.Parse( tPrev.ToString( CSVersionFormat.Normalized ) );

            void CheckLower( SVersion v1, SVersion v2 )
            {
                Assert.That( v1 < v2, "{0} < {1}", v1, v2 );
                Assert.That( v2 > v1, "{0} > {1}", v2, v1 );

                SVersion v1low = SVersion.Parse( v1.ParsedText.ToLowerInvariant() );
                SVersion v2low = SVersion.Parse( v2.ParsedText.ToLowerInvariant() );
                Assert.That( v1low < v2low, "{0} < {1} (lowercase)", v1low, v2low );
                Assert.That( v2low > v1low, "{0} > {1} (lowercase)", v2low, v1low );

                SVersion v1up = SVersion.Parse( v1.ParsedText.ToUpperInvariant() );
                SVersion v2up = SVersion.Parse( v2.ParsedText.ToUpperInvariant() );
                Assert.That( v1up < v2up, "{0} < {1} (uppercase)", v1up, v2up );
                Assert.That( v2up > v1up, "{0} > {1} (uppercase)", v2up, v1up );
            }

            CheckLower( vPrev, v );
            CheckLower( v, vNext );

            var sNuGet = t.ToString( CSVersionFormat.NuGetPackage );
            var sNuGetPrev = tPrev.ToString( CSVersionFormat.NuGetPackage );
            var sNuGetNext = tNext.ToString( CSVersionFormat.NuGetPackage );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetPrev, sNuGet ) < 0, "{0} < {1}", sNuGetPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetNext ) < 0, "{0} < {1}", sNuGet, sNuGetNext );


            CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = "dev", BuildIndex = 1 };

            string sCI = t.ToString( CSVersionFormat.Normalized, ci );
            SVersion vCi = SVersion.Parse( sCI );
            CheckLower( v, vCi );
            CheckLower( vCi, vNext );

            var sNuGetCI = t.ToString( CSVersionFormat.NuGetPackage, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetCI ) < 0, "{0} < {1}", sNuGet, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCI, sNuGetNext ) < 0, "{0} < {1}", sNuGetCI, sNuGetNext );

            string sCiNext = tNext.ToString( CSVersionFormat.Normalized, ci );
            SVersion vCiNext = SVersion.Parse( sCiNext );
            CheckLower( vCi, vCiNext );
            CheckLower( vNext, vCiNext );

            var sNuGetCINext = tNext.ToString( CSVersionFormat.NuGetPackage, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetCI ) > 0, "{0} > {1}", sNuGetCINext, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetNext ) > 0, "{0} > {1}", sNuGetCINext, sNuGetNext );

            string sCiPrev = tPrev.ToString( CSVersionFormat.Normalized, ci );
            SVersion vCiPrev = SVersion.Parse( sCiPrev );
            CheckLower( vPrev, vCiPrev );
            CheckLower( vCiPrev, v );
            CheckLower( vCiPrev, vCiNext );

            var sNuGetCIPrev = tPrev.ToString( CSVersionFormat.NuGetPackage, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetPrev ) > 0, "{0} > {1}", sNuGetCIPrev, sNuGetPrev );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGet ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetCINext ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGetCINext );
        }

    }
}
