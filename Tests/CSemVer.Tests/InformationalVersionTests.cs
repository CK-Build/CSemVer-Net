using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace CSemVer.Tests;

[TestFixture]
public class InformationalVersionTests
{
    [TestCase( null )]
    [TestCase( "" )]
    [TestCase( "not matched" )]
    [TestCase( "0.0.0-0 (0.Z.0-0) - SHA1: 0000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
    [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 000000000000000000000000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
    [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 1000000000000000000X00000000000000000000 - CommitDate: 0001-01-01 00:00:00Z" )]
    [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 1000000000000000000a00000000000000000000 - CommitDate: 01-01 00:00:00Z" )]
    [TestCase( "1.0.0-0 (0.3.0-0) - SHA1: 1000000000000000000a00000000000000000000 - CommitDate: 0001-01-01 00:00:00" )]
    [TestCase( "A.0.0-0+1000000000000000000000000000000000000000/2017-06-27 08:27:35Z" )]
    [TestCase( "1.0.0+00000000000000000/2017-06-27 08:27:35Z" )]
    [TestCase( "1.0.0+1000000000000000000000000000000000000000" )]
    [TestCase( "1.0.0+1000000000000000000000000000000000000000/2017-06-27" )]
    [TestCase( "1.0.0+1000000000000000000000000000000000000000/2017-06-27 08:27:35" )]
    public void parsing_invalid_InformationalVersion_carries_a_ParseErrorMessage( string v )
    {
        var i = new InformationalVersion( v );
        i.IsValidSyntax.Should().BeFalse();
        i.ParseErrorMessage.Should().NotBeNullOrWhiteSpace();
        Assert.Throws<ArgumentException>( () => InformationalVersion.Parse( v ) );
    }

    [Test]
    public void this_assembly_has_a_valid_AssemblyInformationalVersionAttribute()
    {
        var info = InformationalVersion.ReadFromAssembly( System.Reflection.Assembly.GetExecutingAssembly() );
        info.IsValidSyntax.Should().BeTrue();
        info.ParseErrorMessage.Should().BeNull();
    }
    [Test]
    public void InformationalVersion_ReadFromAssembly_only_throws_if_assembly_is_null()
    {
        Assert.Throws<ArgumentNullException>( () => InformationalVersion.ReadFromAssembly( null! ) );
        var info = InformationalVersion.ReadFromAssembly( typeof( string ).Assembly );
        info.IsValidSyntax.Should().BeFalse();
        info.ParseErrorMessage.Should().NotBeNull();
    }

    [Test]
    public void this_assembly_has_a_valid_FileVersionInfo_ProductVersion()
    {
        var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var info = InformationalVersion.ReadFromFile( path );
        info.IsValidSyntax.Should().BeTrue();
        info.ParseErrorMessage.Should().BeNull();
    }

    [Test]
    public void InformationalVersion_ReadFromFile_only_throws_if_path_is_null_or_empty()
    {
        Assert.Throws<ArgumentNullException>( () => InformationalVersion.ReadFromFile( null! ) );
        Assert.Throws<ArgumentNullException>( () => InformationalVersion.ReadFromFile( "" ) );
        Assert.Throws<ArgumentNullException>( () => InformationalVersion.ReadFromFile( " \t " ) );
        {
            var info = InformationalVersion.ReadFromFile( "no way this can be a file." );
            info.IsValidSyntax.Should().BeFalse();
            info.ParseErrorMessage.Should().NotBeNull();
        }
        {
            string path = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText( path, "Just a text." );
            var info = InformationalVersion.ReadFromFile( path );
            info.IsValidSyntax.Should().BeFalse();
            info.ParseErrorMessage.Should().NotBeNull();
            System.IO.File.Delete( path );
        }
    }

}
