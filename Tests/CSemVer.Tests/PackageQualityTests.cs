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
        [TestCase( "9999999.2.3", PackageQuality.Stable )]
        [TestCase( "99999999.999999999.999999999", PackageQuality.Stable )]
        [TestCase( "0.0.0", PackageQuality.Stable )]
        [TestCase( "0.0.1", PackageQuality.Stable )]
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

        [TestCase( " Stable ", "Stable-Stable", " " )]
        [TestCase( " Stable - X", "Stable-Stable", " - X" )]
        [TestCase( " - Stable", "CI-Stable", "" )]
        [TestCase( "-CI", "CI-CI", "" )]
        [TestCase( "-ReleaseCandidate", "CI-ReleaseCandidate", "" )]
        [TestCase( "stable - RC-y", "ReleaseCandidate-Stable", "-y" )]
        [TestCase( " stable - rc", "ReleaseCandidate-Stable", "" )]
        [TestCase( " ci -nimp", "CI-Stable", " -nimp" )]
        [TestCase( "Preview-ExploratoryZ", "Exploratory-Preview", "Z" )]

        [TestCase( " nop", "invalid", " nop" )]
        [TestCase( " -nop", "invalid", " -nop" )]
        public void PackageQualityFilter_tests( string form1, string form2, string remainder )
        {
            ReadOnlySpan<char> head = form1;
            var startHead = head;
            bool match = PackageQualityFilter.TryParse( ref head, out var f1 );

            if( form2 == "invalid" )
            {
                match.Should().BeFalse();
                (head == startHead).Should().BeTrue();
            }
            else
            {
                match.Should().BeTrue();
                PackageQualityFilter.TryParse( form2, out var f2 ).Should().BeTrue();
                f1.Should().Be( f2 );
            }
            head.ToString().Should().Be( remainder );
        }

        [Test]
        public void PackageQuality_GetAllQualities()
        {
            PackageQuality.None.GetAllQualities().Should().BeEquivalentTo( Array.Empty<PackageQuality>() );
            PackageQuality.CI.GetAllQualities().Should().BeEquivalentTo( PackageQuality.CI );
            PackageQuality.Exploratory.GetAllQualities().Should().BeEquivalentTo( PackageQuality.Exploratory, PackageQuality.CI );
            PackageQuality.Preview.GetAllQualities().Should().BeEquivalentTo( PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI );
            PackageQuality.ReleaseCandidate.GetAllQualities().Should().BeEquivalentTo( PackageQuality.ReleaseCandidate, PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI );
            PackageQuality.Stable.GetAllQualities().Should().BeEquivalentTo( PackageQuality.Stable, PackageQuality.ReleaseCandidate, PackageQuality.Preview, PackageQuality.Exploratory, PackageQuality.CI );
        }


    }
}
