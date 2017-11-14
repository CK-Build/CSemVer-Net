using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Push;
using Cake.Common.Tools.NUnit;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeCake
{
    /// <summary>
    /// Standard build "script".
    /// </summary>
    [AddPath( "CodeCakeBuilder/Tools" )]
    [AddPath( "packages/**/tools*" )]
    public class Build : CodeCakeHost
    {
        public Build()
        {
            const string solutionName = "CSemVer-Net";
            const string solutionFileName = solutionName + ".sln";

            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );

            var projects = Cake.ParseSolution( solutionFileName )
                                       .Projects
                                       .Where( p => !(p is SolutionFolder)
                                                    && p.Name != "CodeCakeBuilder" );

            // We do not publish .Tests projects for this solution.
            var projectsToPublish = projects
                                        .Where( p => !p.Path.Segments.Contains( "Tests" ) );

            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();

            // Configuration is either "Debug" or "Release".
            string configuration = "Debug";

            Task( "Check-Repository" )
                .Does( () =>
                 {
                     if( !gitInfo.IsValid )
                     {
                         if( Cake.IsInteractiveMode()
                             && Cake.ReadInteractiveOption( "Repository is not ready to be published. Proceed anyway?", 'Y', 'N' ) == 'Y' )
                         {
                             Cake.Warning( "GitInfo is not valid, but you choose to continue..." );
                         }
                         else if( !Cake.AppVeyor().IsRunningOnAppVeyor ) throw new Exception( "Repository is not ready to be published." );
                     }

                     if( gitInfo.IsValidRelease
                         && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc") )
                     {
                         configuration = "Release";
                     }

                     Cake.Information( "Publishing {0} projects with version={1} and configuration={2}: {3}",
                         projectsToPublish.Count(),
                         gitInfo.SafeSemVersion,
                         configuration,
                         string.Join( ", ", projectsToPublish.Select( p => p.Name ) ) );
                 } );

            Task( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                 {
                     Cake.CleanDirectories( projects.Select( p => p.Path.GetDirectory().Combine( "bin" ) ) );
                     Cake.CleanDirectories( releasesDir );
                     Cake.DeleteFiles( "Tests/**/TestResult*.xml" );
                 } );

            Task( "Build" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                 {
                     Cake.DotNetCoreBuild( "CodeCakeBuilder/CoreBuild.proj",
                         new DotNetCoreBuildSettings().AddVersionArguments( gitInfo, s =>
                         {
                             s.Configuration = configuration;
                         } ) );
                 } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .Does( () =>
                 {
                     var net461Path = "Tests/CSemVer.Tests/bin/" + configuration + "/net461/CSemVer.Tests.dll";
                     Cake.Information( $"Testing: CSemVer.Tests (net461)" );
                     Cake.NUnit( net461Path, new NUnitSettings() { Framework = "v4.5" } );

                     var netCorePath = "Tests/CSemVer.NetCore.Tests/bin/" + configuration + "/netcoreapp2.0/CSemVer.NetCore.Tests.dll";
                     Cake.Information( "Testing: CSemVer.NetCore.Tests (netcoreapp2.0)" );
                     Cake.DotNetCoreExecute( netCorePath );
                 } );

            Task( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                 {
                     Cake.CreateDirectory( releasesDir );
                     foreach( SolutionProject p in projectsToPublish )
                     {
                         var s = new DotNetCorePackSettings();
                         s.ArgumentCustomization = args => args.Append( "--include-symbols" );
                         s.NoBuild = true;
                         s.Configuration = configuration;
                         s.OutputDirectory = releasesDir;
                         s.AddVersionArguments( gitInfo );
                         Cake.DotNetCorePack( p.Path.GetDirectory().FullPath, s );
                     }
                 } );


            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                 {
                     IEnumerable<FilePath> nugetPackages = Cake.GetFiles( releasesDir.Path + "/*.nupkg" );
                     if( Cake.IsInteractiveMode() )
                     {
                         var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                         if( localFeed != null )
                         {
                             Cake.Information( "LocalFeed directory found: {0}", localFeed );
                             if( Cake.ReadInteractiveOption( "Do you want to publish to LocalFeed?", 'Y', 'N' ) == 'Y' )
                             {
                                 Cake.CopyFiles( nugetPackages, localFeed );
                             }
                         }
                     }
                     if( gitInfo.IsValidRelease )
                     {
                         if( gitInfo.PreReleaseName == ""
                             || gitInfo.PreReleaseName == "prerelease"
                             || gitInfo.PreReleaseName == "rc" )
                         {
                             PushNuGetPackages( "NUGET_API_KEY", "https://www.nuget.org/api/v2/package", nugetPackages );
                         }
                         else
                         {
                            // An alpha, beta, delta, epsilon, gamma, kappa goes to invenietis-preview.
                            PushNuGetPackages( "MYGET_PREVIEW_API_KEY", "https://www.myget.org/F/invenietis-preview/api/v2/package", nugetPackages );
                         }
                     }
                     else
                     {
                         Debug.Assert( gitInfo.IsValidCIBuild );
                         PushNuGetPackages( "MYGET_CI_API_KEY", "https://www.myget.org/F/invenietis-ci/api/v2/package", nugetPackages );
                     }
                     if( Cake.AppVeyor().IsRunningOnAppVeyor )
                     {
                         Cake.AppVeyor().UpdateBuildVersion( gitInfo.SafeNuGetVersion );
                     }
                 } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-NuGet-Packages" );

        }

        void PushNuGetPackages( string apiKeyName, string pushUrl, IEnumerable<FilePath> nugetPackages )
        {
            // Resolves the API key.
            var apiKey = Cake.InteractiveEnvironmentVariable( apiKeyName );
            if( string.IsNullOrEmpty( apiKey ) )
            {
                Cake.Information( "Could not resolve {0}. Push to {1} is skipped.", apiKeyName, pushUrl );
            }
            else
            {
                var settings = new NuGetPushSettings
                {
                    Source = pushUrl,
                    ApiKey = apiKey,
                    Verbosity = NuGetVerbosity.Detailed
                };

                foreach( var nupkg in nugetPackages.Where( p => !p.FullPath.EndsWith( ".symbols.nupkg" ) ) )
                {
                    Cake.Information( $"Pushing '{nupkg}' to '{pushUrl}'." );
                    Cake.NuGetPush( nupkg, settings );
                }
            }
        }
    }
}
