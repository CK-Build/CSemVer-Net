using FluentAssertions;
using NUnit.Framework;
using System;

namespace CSemVer.Tests;

[TestFixture]
public class SVersionTests
{
    [Test]
    public void the_Zero_SVersion_is_syntaxically_valid_and_greater_than_null()
    {
        Assert.That( SVersion.ZeroVersion.IsValid );
        Assert.That( SVersion.ZeroVersion > null );
        Assert.That( null < SVersion.ZeroVersion );
        Assert.That( SVersion.ZeroVersion >= null );
        Assert.That( null <= SVersion.ZeroVersion );

        var aZero = SVersion.Create( 0, 0, 0, "0" );
        Assert.That( aZero == SVersion.ZeroVersion );
        Assert.That( aZero >= SVersion.ZeroVersion );
        Assert.That( aZero <= SVersion.ZeroVersion );
    }

    [TestCase( "0.0.0" )]
    [TestCase( "0.0.0--" )]
    [TestCase( "0.0.0-a" )]
    [TestCase( "0.0.0-A" )]
    [TestCase( "1.0.0-beta2-19367-01" )]
    public void the_Zero_SVersion_is_lower_than_any_other_syntaxically_valid_SVersion( string version )
    {
        var v = SVersion.TryParse( version );
        Assert.That( v.IsValid );
        Assert.That( v > SVersion.ZeroVersion );
        Assert.That( v != SVersion.ZeroVersion );
    }

    [Test]
    public void SVersion_can_be_compared_with_operators()
    {
        Assert.That( SVersion.Create( 0, 0, 0 ) > SVersion.Create( 0, 0, 0, "a" ) );
        Assert.That( SVersion.Create( 0, 0, 0 ) >= SVersion.Create( 0, 0, 0, "a" ) );
        Assert.That( SVersion.Create( 0, 0, 0, "a" ) < SVersion.Create( 0, 0, 0 ) );
        Assert.That( SVersion.Create( 0, 0, 0, "a" ) <= SVersion.Create( 0, 0, 0 ) );
        Assert.That( SVersion.Create( 0, 0, 0, "a" ) != SVersion.Create( 0, 0, 0 ) );
    }

    [TestCase( "01.0.0" )]
    [TestCase( "0.01.0" )]
    [TestCase( "0.0.01" )]
    [TestCase( "12897798127391372937.0.0" )]
    [TestCase( "1.999999999999999999.0" )]
    [TestCase( "1.2.99999999999999999999999" )]
    [TestCase( "0.0" )]
    [TestCase( "0" )]
    [TestCase( null )]
    [TestCase( "not a version at all" )]
    [TestCase( "0.0.0-+" )]
    [TestCase( "0.0.0-." )]
    [TestCase( "0.0.0-.." )]
    [TestCase( "0.0.0-a..b" )]
    [TestCase( "0.0.0-01" )]
    [TestCase( "0.0.0-$" )]
    public void Syntaxically_invalid_SVersion_are_greater_than_null_and_lower_than_the_Zero_one( string invalid )
    {
        SVersion notV = SVersion.TryParse( invalid );
        Assert.That( !notV.IsValid );
        Assert.That( notV != SVersion.ZeroVersion );
        Assert.That( SVersion.ZeroVersion > notV );
        Assert.That( SVersion.ZeroVersion >= notV );
    }


    [TestCase( "0.0.0-alpha", '=', "0.0.0-a" )]
    [TestCase( null, '=', null )]
    [TestCase( null, '<', "invalid" )]
    [TestCase( "0.0.0-0", '>', "invalid" )]
    [TestCase( "0.0.0-0", '>', null )]
    [TestCase( "1.2.3-beta.1", '=', "1.2.3-b01" )]
    [TestCase( "1.2.3-rc.1.0.2", '>', "1.2.3-r01-00-01" )]
    [TestCase( "1.2.3-pre", '<', "1.2.3-prerelease.0.1" )]
    [TestCase( "1.2.3-prerelease", '>', "1.2.3-prea" )]
    [TestCase( "1.2.3-prerelease", '=', "1.2.3-pre" )]
    [TestCase( "1.2.3-prerelease", '=', "1.2.3-p" )]
    [TestCase( "1.2.3-beta", '>', "1.2.3-baa" )]
    public void CSemVerSafeCompare_in_action( string left, char op, string right )
    {
        SVersion? vL = left != null ? SVersion.TryParse( left ) : null;
        SVersion? vR = right != null ? SVersion.TryParse( right ) : null;
        switch( op )
        {
            case '>':
                SVersion.CSemVerSafeCompare( vL, vR ).Should().BePositive();
                SVersion.CSemVerSafeCompare( vR, vL ).Should().BeNegative();
                break;
            case '<':
                SVersion.CSemVerSafeCompare( vL, vR ).Should().BeNegative();
                SVersion.CSemVerSafeCompare( vR, vL ).Should().BePositive();
                break;
            case '=':
                SVersion.CSemVerSafeCompare( vL, vR ).Should().Be( 0 );
                SVersion.CSemVerSafeCompare( vR, vL ).Should().Be( 0 );
                break;
            default: throw new ArgumentException( "Unsupported operator.", nameof( op ) );
        }
    }

    [TestCase( "1.0.0-beta2 after", "1.0.0-beta2" )]
    [TestCase( "1.0.0-AZE,after", "1.0.0-AZE" )]
    [TestCase( "0.0.0,after", "0.0.0" )]
    [TestCase( "0.0.0-rc.1.2,after", "0.0.0-rc.1.2" )]
    public void parsing_works_on_prefix_and_ParsedText_covers_the_version( string t, string parsedText )
    {
        var head = t.AsSpan();
        var v = SVersion.TryParse( ref head );
        v.IsValid.Should().BeTrue();
        v.ErrorMessage.Should().BeNull();
        v.ParsedText.Should().Be( parsedText );
        t.Should().StartWith( v.ParsedText );
    }

    [TestCase( "1.2.3.4" )]
    [TestCase( "0.0.0.0" )]
    [TestCase( "1.2.3.4-alpha" )]
    public void parsing_fourth_part_should_be_positive_when_fourth_part_is_here( string sv )
    {
        var head = sv.AsSpan();
        var v = SVersion.TryParse( ref head );
        v.FourthPart.Should().BeGreaterThanOrEqualTo( 0 );
        v.AsCSVersion.Should().BeNull();

    }


    [TestCase( "1.2.3" )]
    [TestCase( "0.0.0" )]
    [TestCase( "1.2.3-alpha" )]
    public void parsing_fourth_part_should_be_negative_when_fourth_part_is_not_here( string sv )
    {
        var head = sv.AsSpan();
        var v = SVersion.TryParse( ref head );
        v.FourthPart.Should().BeNegative();
        v.AsCSVersion.Should().NotBeNull();

    }
}
