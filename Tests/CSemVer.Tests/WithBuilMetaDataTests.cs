using NUnit.Framework;

namespace CSemVer.Tests;

[TestFixture]
public class WithBuilMetaDataTests
{
    [Test]
    public void WithBuildMetaData_works_on_SVersion_and_CSVersion()
    {
        SVersion sv = SVersion.TryParse( "1.0.0-not.a.CSemVer.Version" );
        SVersion svc = SVersion.TryParse( "1.0.0-alpha" );
        SVersion svnc = SVersion.TryParse( "1.0.0-pre", handleCSVersion: false );

        Assert.That( sv, Is.Not.AssignableTo<CSVersion>() );
        Assert.That( sv.AsCSVersion, Is.Null );

        Assert.That( svc, Is.AssignableTo<CSVersion>() );
        Assert.That( svc.AsCSVersion, Is.SameAs( svc ) );

        Assert.That( svnc, Is.Not.AssignableTo<CSVersion>() );
        Assert.That( svnc.AsCSVersion, Is.Not.Null );

        SVersion svB = sv.WithBuildMetaData( "Test" );
        Assert.That( svB, Is.Not.AssignableTo<CSVersion>() );
        Assert.That( svB.AsCSVersion, Is.Null );
        Assert.That( svB.NormalizedText, Is.EqualTo( "1.0.0-not.a.CSemVer.Version+Test" ) );

        SVersion svcB = svc.WithBuildMetaData( "Test" );
        Assert.That( svcB, Is.AssignableTo<CSVersion>() );
        Assert.That( svcB.AsCSVersion, Is.SameAs( svcB ) );
        Assert.That( svcB.NormalizedText, Is.EqualTo( "1.0.0-alpha+Test" ) );

        SVersion svncB = svnc.WithBuildMetaData( "Test" );
        Assert.That( svncB, Is.Not.AssignableTo<CSVersion>() );
        Assert.That( svncB.AsCSVersion, Is.Not.Null );
        Assert.That( svncB.NormalizedText, Is.EqualTo( "1.0.0-pre+Test" ) );
        Assert.That( svncB.AsCSVersion.NormalizedText, Is.EqualTo( "1.0.0-preview+Test" ) );
        Assert.That( svncB.AsCSVersion.ToNormalizedForm().NormalizedText, Is.EqualTo( "1.0.0-p+Test" ) );

    }
}
