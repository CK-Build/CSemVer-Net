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
    public class PackageQualityVersionsTests
    {
        [TestCase( "1.0.0, 1.0.1", "1.0.1" )]
        [TestCase( "1.0.0-a, 1.0.1-a", "1.0.1-a" )]
        [TestCase( "1.0.1-r, 1.0.0-r", "1.0.1-r" )]
        [TestCase( "1.0.0-e, 1.0.0-g", "1.0.0-g" )]
        [TestCase( "1.0.0-d, 1.0.0-a, 1.0.0-b", "1.0.0-d" )]
        public void collecting_best_version( string versions, string result )
        {
            var v = versions.Split( ',' ).Select( x => SVersion.Parse( x.Trim() ) ).ToArray();
            var q = new PackageQualityVersions(v, false);
            q.ToString().Should().Be( result );
        }

        [TestCase( "1.0.0-a, 0.0.1-r02-01, 0.0.1-r, 0.0.1-r02, 1.0.0-d, 1.0.0-b",
                   "1.0.0-d / 0.0.1-r02-01" )]

        [TestCase( "1.0.0-a, 0.1.1-r02-01, 0.1.1-r, 0.1.1-r02, 0.1.0, 1.0.0-d, 1.0.0-b",
                   "1.0.0-d / 0.1.1-r02-01 / 0.1.0" )]

        [TestCase( "1.0.0-a, 0.5.0-e, 0.4.1-r02-01, 0.4.1-r, 0.4.1-r02, 0.1.0, 0.5.0-k, 1.0.0-d, 1.0.0-b",
                   "1.0.0-d / 0.5.0-k / 0.4.1-r02-01 / 0.1.0" )]

        [TestCase( "4.8.0-anything.is.CI, 4.8.1-ci.another, 4.8.1-ze.best.ci, 1.0.0-a, 0.5.0-e, 0.4.1-r02-01, 0.4.1-r, 0.4.1-r02, 0.1.0, 0.5.0-k, 1.0.0-d, 1.0.0-b",
                   "4.8.1-ze.best.ci / 1.0.0-d / 0.5.0-k / 0.4.1-r02-01 / 0.1.0" )]
        public void collecting_multiple_versions( string versions, string result )
        {
            var v = versions.Split( ',' ).Select( x => SVersion.Parse( x.Trim() ) ).ToArray();
            var q = new PackageQualityVersions(v, false);
            q.ToString().Should().Be( result );
        }


    }
}
