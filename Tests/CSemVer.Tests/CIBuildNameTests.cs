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
            Assert.That( vPrev < v, "{0} < {1}", vPrev, v );
            Assert.That( v < vNext, "{0} < {1}", v, vNext );

            var sNuGet = t.ToString( CSVersionFormat.NuGetPackage );
            var sNuGetPrev = tPrev.ToString( CSVersionFormat.NuGetPackage );
            var sNuGetNext = tNext.ToString( CSVersionFormat.NuGetPackage );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetPrev, sNuGet ) < 0, "{0} < {1}", sNuGetPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetNext ) < 0, "{0} < {1}", sNuGet, sNuGetNext );


            CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = "dev", BuildIndex = 1 };

            string sCI = t.ToString( CSVersionFormat.Normalized, ci );
            SVersion vCi = SVersion.Parse( sCI );
            Assert.That( v < vCi, "{0} < {1}", v, vCi );
            Assert.That( vCi < vNext, "{0} < {1}", vCi, vNext );

            var sNuGetCI = t.ToString( CSVersionFormat.NuGetPackage, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetCI ) < 0, "{0} < {1}", sNuGet, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCI, sNuGetNext ) < 0, "{0} < {1}", sNuGetCI, sNuGetNext );

            string sCiNext = tNext.ToString( CSVersionFormat.Normalized, ci );
            SVersion vCiNext = SVersion.Parse( sCiNext );
            Assert.That( vCiNext > vCi, "{0} > {1}", vCiNext, vCi );
            Assert.That( vCiNext > vNext, "{0} > {1}", vCiNext, vNext );

            var sNuGetCINext = tNext.ToString( CSVersionFormat.NuGetPackage, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetCI ) > 0, "{0} > {1}", sNuGetCINext, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetNext ) > 0, "{0} > {1}", sNuGetCINext, sNuGetNext );

            string sCiPrev = tPrev.ToString( CSVersionFormat.Normalized, ci );
            SVersion vCiPrev = SVersion.Parse( sCiPrev );
            Assert.That( vCiPrev > vPrev, "{0} > {1}", vCiPrev, vPrev );
            Assert.That( vCiPrev < v, "{0} < {1}", vCiPrev, v );
            Assert.That( vCiPrev < vCiNext, "{0} < {1}", vCiPrev, vCiNext );

            var sNuGetCIPrev = tPrev.ToString( CSVersionFormat.NuGetPackage, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetPrev ) > 0, "{0} > {1}", sNuGetCIPrev, sNuGetPrev );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGet ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetCINext ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGetCINext );
        }

    }
}
