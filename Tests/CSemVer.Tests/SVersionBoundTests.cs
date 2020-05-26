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
        static readonly SVersion V100 = SVersion.Create( 1, 0, 0 );
        static readonly SVersion V101 = SVersion.Create( 1, 0, 1 );
        static readonly SVersion V110 = SVersion.Create( 1, 1, 0 );
        static readonly SVersion V111 = SVersion.Create( 1, 1, 1 );
        static readonly SVersion V120 = SVersion.Create( 1, 2, 0 );
        static readonly SVersion V121 = SVersion.Create( 1, 2, 1 );
        static readonly SVersion V200 = SVersion.Create( 2, 0, 0 );
        static readonly SVersion V201 = SVersion.Create( 2, 0, 1 );
        static readonly SVersion V210 = SVersion.Create( 2, 1, 0 );
        static readonly SVersion V211 = SVersion.Create( 2, 1, 1 );

        [Test]
        public void basic_union_operations()
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

        [Test]
        public void partial_ordering_only()
        {
            var b1 = new SVersionBound( V100, SVersionLock.None, PackageQuality.Preview );
            var b11 = new SVersionBound( V110, SVersionLock.None, PackageQuality.None );

            b1.Contains( b11 ).Should().BeFalse( "b1 only accepts preview and b11 accepts everything." );
            b11.Contains( b1 ).Should().BeFalse( "b11.Base version is greater than b1.Base version." );

            var bound = b1.Union( b11 );
            bound.Contains( b1 ).Should().BeTrue();
            bound.Contains( b11 ).Should().BeTrue();

            b11.Union( b1 ).Should().Be( bound );
        }

        [Test]
        public void SVersionLock_tests()
        {
            var b1LockMinor = new SVersionBound( V100, SVersionLock.LockedMinor, PackageQuality.None );
            b1LockMinor.Satisfy( V100 ).Should().BeTrue();
            b1LockMinor.Satisfy( V101 ).Should().BeTrue();
            b1LockMinor.Satisfy( V110 ).Should().BeFalse();
            b1LockMinor.Satisfy( V200 ).Should().BeFalse();

            var b11 = new SVersionBound( V110, SVersionLock.LockedMajor, PackageQuality.None );
            b11.Satisfy( V100 ).Should().BeFalse();
            b11.Satisfy( V110 ).Should().BeTrue();
            b11.Satisfy( V111 ).Should().BeTrue();
            b11.Satisfy( V200 ).Should().BeFalse();

            b1LockMinor.Contains( b11 ).Should().BeFalse( "The 1.0 minor is locked." );

            var b1LockMajor = b1LockMinor.SetLock( SVersionLock.LockedMajor );
            b1LockMajor.Contains( b1LockMinor ).Should().BeTrue();
            b1LockMajor.Contains( b11 ).Should().BeTrue( "Same major is locked." );

            var b2 = new SVersionBound( V200, SVersionLock.Locked, PackageQuality.None );
            b1LockMinor.Contains( b2 ).Should().BeFalse();
            b1LockMajor.Contains( b2 ).Should().BeFalse();
        }

    }
}
