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
    public class PackageQualityTests
    {
        [TestCase( "Not a version", PackageQuality.None )]
        [TestCase( "0.0.0-0", PackageQuality.CI )]
        [TestCase( "1.2.3-not-a-CSemVer", PackageQuality.CI )]
        [TestCase( "9999999.2.3", PackageQuality.StableRelease )]
        [TestCase( "99999999.999999999.999999999", PackageQuality.StableRelease )]
        [TestCase( "0.0.0", PackageQuality.StableRelease )]
        [TestCase( "0.0.1", PackageQuality.StableRelease )]
        [TestCase( "0.0.0-alpha", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-alpha.1", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-alpha.1.1", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-beta", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-pre", PackageQuality.Preview )]
        [TestCase( "0.0.0-preview", PackageQuality.Preview )]
        [TestCase( "0.0.0-prerelease", PackageQuality.Preview )]
        [TestCase( "0.0.0-prewtf.if-prefixed-by.pre", PackageQuality.Preview )]
        [TestCase( "0.0.0-pre.56", PackageQuality.Preview )]
        [TestCase( "0.0.0-pre.56.2", PackageQuality.Preview )]
        [TestCase( "1.2.3-rc", PackageQuality.ReleaseCandidate )]
        [TestCase( "1.2.3-rc.56", PackageQuality.ReleaseCandidate )]
        [TestCase( "1.2.3-rc.56.2", PackageQuality.ReleaseCandidate )]
        public void version_to_quality_mapping( string version, PackageQuality q )
        {
            SVersion.TryParse( version ).PackageQuality.Should().Be( q );
        }

        [TestCase( "Release", "Release-Release" )]
        [TestCase( "Release-", "Release-Release" )]
        [TestCase( "-Release", "CI-Release" )]
        [TestCase( "-CI", "CI-CI" )]
        [TestCase( "-ReleaseCandidate", "CI-ReleaseCandidate" )]
        [TestCase( "Preview-Exploratory", "Exploratory-Preview" )]
        [TestCase( "-", "None-None" )]
        [TestCase( "", "None-None" )]
        public void PackageQualityFilter_tests( string form1, string form2 )
        {
            PackageQualityFilter.TryParse( form1, out var f1 ).Should().BeTrue();
            PackageQualityFilter.TryParse( form2, out var f2 ).Should().BeTrue();
            f1.Should().Be( f2 );
        }


    }
}
