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
    public class QualityAndLabelsTests
    {
        [TestCase( "Not a version", PackageQuality.None )]
        [TestCase( "0.0.0-0", PackageQuality.CI )]
        [TestCase( "1.2.3-not-a-CSemVer", PackageQuality.CI )]
        [TestCase( "9999999.2.3", PackageQuality.CI )]
        [TestCase( "0.0.0", PackageQuality.Release )]
        [TestCase( "0.0.1", PackageQuality.Release )]
        [TestCase( "0.0.0-alpha", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-alpha.1", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-alpha.1.1", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-beta", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-delta", PackageQuality.Exploratory )]
        [TestCase( "0.0.0-epsilon", PackageQuality.Preview )]
        [TestCase( "0.0.0-gamma", PackageQuality.Preview )]
        [TestCase( "0.0.0-kappa", PackageQuality.Preview )]
        [TestCase( "0.0.0-pre", PackageQuality.ReleaseCandidate )]
        [TestCase( "0.0.0-pre.56", PackageQuality.ReleaseCandidate )]
        [TestCase( "0.0.0-pre.56.2", PackageQuality.ReleaseCandidate )]
        [TestCase( "1.2.3-rc", PackageQuality.ReleaseCandidate )]
        [TestCase( "1.2.3-rc.56", PackageQuality.ReleaseCandidate )]
        [TestCase( "1.2.3-rc.56.2", PackageQuality.ReleaseCandidate )]
        public void version_to_quality_mapping( string version, PackageQuality q )
        {
            SVersion.TryParse( version ).PackageQuality.Should().Be( q );
        }

        [TestCase( PackageQuality.CI, "CI" )]
        [TestCase( PackageQuality.Exploratory, "Exploratory,CI" )]
        [TestCase( PackageQuality.Preview, "Preview,Exploratory,CI" )]
        [TestCase( PackageQuality.ReleaseCandidate, "Latest,Preview,Exploratory,CI" )]
        [TestCase( PackageQuality.Release, "Stable,Latest,Preview,Exploratory,CI" )]
        public void Quality_to_Labels_mappings( PackageQuality q, string labels )
        {
            var l = labels.Split( ',' ).Select( s => (PackageLabel)Enum.Parse( typeof( PackageLabel ), s ) );
            q.GetLabels().Should().BeEquivalentTo( l, o => o.WithStrictOrdering() );
        }




    }
}
