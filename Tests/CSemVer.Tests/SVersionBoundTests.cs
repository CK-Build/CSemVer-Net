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
        [Test]
        public void basic_partial_ordering_operations()
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

    }
}
