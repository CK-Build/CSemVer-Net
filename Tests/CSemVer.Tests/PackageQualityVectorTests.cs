using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace CSemVer.Tests;

[TestFixture]
public class PackageQualityVectorTests
{
    [TestCase( "1.0.0, 1.0.1", "1.0.1" )]
    [TestCase( "1.0.0-a, 1.0.1-a", "1.0.1-a" )]
    [TestCase( "1.0.1-r, 1.0.0-r", "1.0.1-r" )]
    [TestCase( "1.0.0-ex, 1.0.0-ez", "1.0.0-ez" )]
    [TestCase( "1.0.0-alpha.2, 1.0.0-a, 1.0.0-alpha.0.1", "1.0.0-alpha.2" )]
    public void collecting_best_version( string versions, string result )
    {
        var v = versions.Split( ',' ).Select( x => SVersion.Parse( x.Trim() ) ).ToArray();
        var q = new PackageQualityVector( v, false );
        q.ToString().Should().Be( result );
        q.IsValid.Should().Be( result.Length > 0 );
        q.ActualCount.Should().Be( result.Length > 0 ? 1 : 0 );
    }

    [Test]
    public void no_version()
    {
        var q = new PackageQualityVector( Enumerable.Empty<SVersion>(), false );
        q.ToString().Should().Be( "" );
        q.IsValid.Should().BeFalse();
        q.ActualCount.Should().Be( 0 );
    }

    [TestCase( "1.0.0-a, 0.0.1-r02-01, 0.0.1-r, 0.0.1-r02, 1.0.0-b01, 1.0.0-b",
               "1.0.0-b01 / 0.0.1-r02-01" )]

    [TestCase( "1.0.0-a, 0.1.1-r02-01, 0.1.1-r, 0.1.1-r02, 1.0.0-ci, 1.0.0-b",
               "1.0.0-ci / 1.0.0-b / 0.1.1-r02-01" )]

    [TestCase( "1.0.0-a, 0.5.0-e, 0.4.1-r02-01, 0.4.1-r, 0.4.1-r02, 0.1.0, 0.5.0-p, 1.0.0-b",
               "1.0.0-b / 0.5.0-p / 0.4.1-r02-01 / 0.1.0" )]

    [TestCase( "4.8.0-anything.is.CI, 4.8.1-ci.another, 4.8.1-ze.best.ci, 1.0.0-a, 0.5.0-e, 0.4.1-r02-01, 0.4.1-r, 0.4.1-r02, 0.1.0, 0.5.0-p, 1.0.0-beta.1, 1.0.0-b",
               "4.8.1-ze.best.ci / 1.0.0-beta.1 / 0.5.0-p / 0.4.1-r02-01 / 0.1.0" )]
    public void collecting_multiple_versions( string versions, string result )
    {
        var v = versions.Split( ',' ).Select( x => SVersion.Parse( x.Trim() ) ).ToArray();
        var q = new PackageQualityVector( v, false );
        q.ToString().Should().Be( result );
        q.ActualCount.Should().Be( result.Count( c => c == '/' ) + 1 );
    }


}
