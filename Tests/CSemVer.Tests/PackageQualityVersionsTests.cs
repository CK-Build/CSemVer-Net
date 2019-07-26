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
        [TestCase( "1.0.0-alpha, 1.0.1-alpha", "1.0.1-alpha" )]
        [TestCase( "1.0.1-rc, 1.0.0-rc", "1.0.1-rc" )]
        [TestCase( "1.0.0-epsilon, 1.0.0-gamma", "1.0.0-gamma" )]
        [TestCase( "1.0.0-delta, 1.0.0-alpha, 1.0.0-beta", "1.0.0-delta" )]
        public void collecting_best_version( string versions, string result )
        {
            var v = versions.Split( ',' ).Select( x => SVersion.Parse( x.Trim() ) ).ToArray();
            var q = new PackageQualityVersions(v, false);
            q.ToString().Should().Be( result );
        }

        [TestCase( "1.0.0-alpha, 0.0.1-rc.2.1, 0.0.1-rc, 0.0.1-rc.2, 1.0.0-delta, 1.0.0-beta",
                   "1.0.0-delta / 0.0.1-rc.2.1" )]

        [TestCase( "1.0.0-alpha, 0.1.1-rc.2.1, 0.1.1-rc, 0.1.1-rc.2, 0.1.0, 1.0.0-delta, 1.0.0-beta",
                   "1.0.0-delta / 0.1.1-rc.2.1 / 0.1.0" )]

        [TestCase( "1.0.0-alpha, 0.5.0-epsilon, 0.4.1-rc.2.1, 0.4.1-rc, 0.4.1-rc.2, 0.1.0, 0.5.0-kappa, 1.0.0-delta, 1.0.0-beta",
                   "1.0.0-delta / 0.5.0-kappa / 0.4.1-rc.2.1 / 0.1.0" )]

        [TestCase( "4.8.0-anything.is.CI, 4.8.1-ci.another, 4.8.1-ze.best.ci, 1.0.0-alpha, 0.5.0-epsilon, 0.4.1-rc.2.1, 0.4.1-rc, 0.4.1-rc.2, 0.1.0, 0.5.0-kappa, 1.0.0-delta, 1.0.0-beta",
                   "4.8.1-ze.best.ci / 1.0.0-delta / 0.5.0-kappa / 0.4.1-rc.2.1 / 0.1.0" )]
        public void collecting_multiple_versions( string versions, string result )
        {
            var v = versions.Split( ',' ).Select( x => SVersion.Parse( x.Trim() ) ).ToArray();
            var q = new PackageQualityVersions(v, false);
            q.ToString().Should().Be( result );
        }


    }
}
