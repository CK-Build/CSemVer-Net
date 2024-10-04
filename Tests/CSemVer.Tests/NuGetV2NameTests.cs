using NUnit.Framework;
using System;
using System.Linq;

namespace CSemVer.Tests;

[TestFixture]
public class NuGetV2NameTests
{
    [Explicit]
    [TestCase( "0.0.0-alpha" )]
    [TestCase( "0.0.0-alpha.1" )]
    [TestCase( "0.0.0-alpha.2" )]
    [TestCase( "0.0.0-alpha.0.1" )]
    [TestCase( "1.0.0-rc" )]
    [TestCase( "99999.49999.9999" )]
    public void display_name_and_successors_samples( string v )
    {
        CSVersion t = CSVersion.TryParse( v );
        var succ = t.GetDirectSuccessors( false );

        Console.WriteLine( " -> - found {0} successors for '{1}' (NuGetV2 = {2}, Ordered Version = {3}, File = {4}.{5}.{6}.{7}):",
                            succ.Count(),
                            t,
                            t.ToString(),
                            t.OrderedVersion,
                            t.OrderedVersionMajor,
                            t.OrderedVersionMinor,
                            t.OrderedVersionBuild,
                            t.OrderedVersionRevision
                            );
        Console.WriteLine( "      " + string.Join( ", ", succ.Select( s => s.ToString() ) ) );
    }

    [Explicit]
    [TestCase( "0.0.0-alpha, 0.0.0-alpha.1, 0.0.0-alpha.2, 1.0.0-prerelease, 0.0.0-prerelease.99.99, 5.0.0", 10 )]
    [TestCase( "99999.49999.9999-rc.99.99, 99999.49999.9999", 10 )]
    public void display_name_for_CI_build_and_check_20_characters_limit( string versions, int range )
    {
        var buildInfo = new CIBuildDescriptor() { BranchName = "develop", BuildIndex = 21 };
        foreach( var v in versions.Split( ',' ).Select( s => s.Trim() ) )
        {
            CSVersion t = CSVersion.TryParse( v );
            Console.WriteLine( t );
            for( int i = -range; i <= range; ++i )
            {
                var num = t.OrderedVersion + i;
                if( num > 0m && num <= CSVersion.VeryLastVersion.OrderedVersion )
                {
                    CSVersion tD = CSVersion.Create( num );
                    DumpVersionInfo( buildInfo, tD );
                }
            }
        }
    }

    static void DumpVersionInfo( CIBuildDescriptor buildInfo, CSVersion t )
    {
        var nugetV2Build = t.ToString( CSVersionFormat.Normalized, buildInfo );
        int nugetV2BuildSNLen = SVersion.Parse( nugetV2Build ).Prerelease.Length;
        Console.WriteLine( "{0}, CI = {1}, NuGet = {2}, NuGet CI = {3}, NugetV2Build.SpecialName.Length = {4}",
                            t,
                            t.ToString( CSVersionFormat.Normalized, buildInfo ),
                            t.ToString( CSVersionFormat.Normalized ),
                            nugetV2Build,
                            nugetV2BuildSNLen
                            );
        Assert.That( nugetV2BuildSNLen, Is.LessThanOrEqualTo( 20 ) );
    }

    [TestCase( "0.0.0-alpha", "0.0.0-a", false )]
    [TestCase( "3.0.1-beta.12", "3.0.1-b12", false )]
    [TestCase( "3.0.1-delta.1", "3.0.1-d01", false )]
    [TestCase( "3.0.1-epsilon.18", "3.0.1-e18", false )]
    [TestCase( "3.0.1-epsilon.18.1", "3.0.1-e18-01", true )]
    [TestCase( "3.0.1-gamma.19", "3.0.1-g19", false )]
    [TestCase( "3.0.1-kappa.21", "3.0.1-k21", false )]
    [TestCase( "3.0.1-preview.24", "3.0.1-p24", false )]
    [TestCase( "3.0.1-preview.24.99", "3.0.1-p24-99", true )]
    [TestCase( "99999.49999.9999-rc.99", "99999.49999.9999-r99", false )]
    [TestCase( "99999.49999.9999-rc.99.99", "99999.49999.9999-r99-99", true )]
    public void pre_release_with_standard_names_nugetV2_mappings( string tag, string nuget, bool isPrereleasePatch )
    {
        CSVersion fromShortForm = CSVersion.Parse( nuget );
        CSVersion t = CSVersion.TryParse( tag );
        Assert.That( t, Is.EqualTo( fromShortForm ) );

        Assert.That( t.IsValid );
        Assert.That( t.IsPrerelease );
        Assert.That( t.IsPreReleasePatch, Is.EqualTo( isPrereleasePatch ) );
        Assert.That( t.ToString( CSVersionFormat.LongForm ), Is.EqualTo( tag ) );
        Assert.That( t.ToString( CSVersionFormat.Normalized ), Is.EqualTo( nuget ) );
        Assert.That( SVersion.Parse( nuget ).Prerelease.Length, Is.LessThanOrEqualTo( 20 ) );

    }

    [TestCase( "0.0.0-alpha.0.1", "0.0.0-a00-01" )]
    [TestCase( "3.0.1-beta.12.8", "3.0.1-b12-08" )]
    [TestCase( "3.0.1-delta.1.99", "3.0.1-d01-99" )]
    [TestCase( "3.0.1-epsilon.18.2", "3.0.1-e18-02" )]
    [TestCase( "3.0.1-gamma.19.4", "3.0.1-g19-04" )]
    [TestCase( "3.0.1-kappa.1.5", "3.0.1-k01-05" )]
    [TestCase( "3.0.1-preview.0.1", "3.0.1-p00-01" )]
    [TestCase( "99999.49999.9999-rc.99.99", "99999.49999.9999-r99-99" )]
    public void pre_release_with_standard_names_and_fix_number_normalized_mappings( string longF, string shortF )
    {
        CSVersion tS = CSVersion.Parse( shortF );
        CSVersion tL = CSVersion.TryParse( longF );
        Assert.That( tL, Is.EqualTo( tS.ToLongForm() ) );
        Assert.That( tS, Is.EqualTo( tL.ToNormalizedForm() ) );

        Assert.That( tL.IsValid );
        Assert.That( tL.IsPrerelease );
        Assert.That( tL.IsPreReleasePatch );
        Assert.That( tL.PrereleasePatch, Is.GreaterThan( 0 ) );
        Assert.That( tL.ToString( CSVersionFormat.Normalized ), Is.EqualTo( shortF ) );
        Assert.That( tS.ToString( CSVersionFormat.LongForm ), Is.EqualTo( longF ) );
        Assert.That( tS.NormalizedText, Is.EqualTo( shortF ) );
        Assert.That( tL.NormalizedText, Is.EqualTo( longF ) );
        var buildInfo = tS.ToString( CSVersionFormat.Normalized, new CIBuildDescriptor() { BuildIndex = CIBuildDescriptor.MaxBuildIndex, BranchName = "ABCDEFGH" } );
        Assert.That( SVersion.Parse( buildInfo ).Prerelease.Length, Is.LessThanOrEqualTo( 20 ) );

    }

}

