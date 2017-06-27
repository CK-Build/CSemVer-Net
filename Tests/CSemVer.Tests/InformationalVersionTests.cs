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
    public class InformationalVersionTests
    {
        [TestCase( null )]
        [TestCase( "" )]
        [TestCase( "not matched" )]
        [TestCase( "0.0.0-0 (0.Z.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
        [TestCase( "0.A.0-0 (0.2.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
        [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
        [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 1000000000000000000X00000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
        [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 1000000000000000000a00000000000000000000 - CommitDate: 01-01 00:00:00Z" )]
        [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 1000000000000000000a00000000000000000000 - CommitDate: 0001-01-01 00:00:00" )]
        public void parsing_invalid_InformationalVersion_carries_a_ParseErrorMessage( string v )
        {
            var i = new InformationalVersion( v );
            i.IsValidSyntax.Should().BeFalse();
            i.ParseErrorMessage.Should().NotBeNullOrWhiteSpace();
            Assert.Throws<ArgumentException>( () => InformationalVersion.Parse( v ) );
        }

        [TestCase( "1.2.3-prerelease (1.2.3-a) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 2017-06-27 08:27:35Z" )]
        [TestCase( "99.0.2 (1.0.0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 2017-06-27 08:27:35Z" )]
        public void NuGet_and_SemVer_versions_equivalence_are_not_checked( string v )
        {
            var i = new InformationalVersion( v );
            i.IsValidSyntax.Should().BeTrue();
            i.ParseErrorMessage.Should().BeNull();
            InformationalVersion.Parse( v );
        }
    }
}
