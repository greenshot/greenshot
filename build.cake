#tool "xunit.runner.console"
#tool "OpenCover"
#tool "GitVersion.CommandLine"
#tool "docfx.console"
#tool "PdbGit"
// Needed for Cake.Compression, as described here: https://github.com/akordowski/Cake.Compression/issues/3
#addin "SharpZipLib"
#addin "Cake.FileHelpers"
#addin "Cake.DocFx"
#addin "Cake.Compression"

var target = Argument("target", "Build");
var configuration = Argument("configuration", "release");

// Used to publish NuGet packages
var nugetApiKey = Argument("nugetApiKey", EnvironmentVariable("NuGetApiKey"));

// Used to publish coverage report
var coverallsRepoToken = Argument("coverallsRepoToken", EnvironmentVariable("CoverallsRepoToken"));

// where is our solution located?
var solutionFilePath = GetFiles("src/*.sln").First();
var solutionName = solutionFilePath.GetDirectory().GetDirectoryName();

// Check if we are in a pull request, publishing of packages and coverage should be skipped
var isPullRequest = !string.IsNullOrEmpty(EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER"));

// Check if the commit is marked as release
var isRelease = Argument<bool>("isRelease", string.Compare("[release]", EnvironmentVariable("appveyor_repo_commit_message_extended"), true) == 0);

// Used to store the version, which is needed during the build and the packaging
var version = EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? "1.0.0";

Task("Default")
    .IsDependentOn("Publish");

// Publish the Artifact of the Package Task to the Nexus Pro
Task("Publish")
	.IsDependentOn("CreateAndUploadCoverageReport")
    .IsDependentOn("PublishPackages")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(nugetApiKey))
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRelease)
    .Does(()=>
{
});

Task("PublishPackages")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(nugetApiKey))
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRelease)
    .Does(()=>
{
    var settings = new NuGetPushSettings {
        Source = "https://www.nuget.org/api/v2/package",
        ApiKey = nugetApiKey
    };

    var packages = GetFiles("./artifacts/*.nupkg").Where(p => !p.FullPath.ToLower().Contains("symbols"));
    NuGetPush(packages, settings);
});

// Package the results of the build, if the tests worked, into a NuGet Package
Task("Package")
    .IsDependentOn("Coverage")
    .IsDependentOn("Documentation")
    .IsDependentOn("GitLink")
    .Does(()=>
{
	// N.A.
});

// Build the DocFX documentation site
Task("Documentation")
    .Does(() =>
{
    DocFxMetadata("./doc/docfx.json");
    DocFxBuild("./doc/docfx.json");

    CreateDirectory("artifacts");
    // Archive the generated site
    ZipCompress("./doc/_site", "./artifacts/site.zip");
});

Task("CreateAndUploadCoverageReport")
    .IsDependentOn("Coverage")
    .WithCriteria(() => !string.IsNullOrEmpty(coverallsRepoToken))
    .IsDependentOn("UploadCoverageReport")
    .Does(() =>
{
	// N.A.
});

Task("UploadCoverageReport")
    .WithCriteria(() => FileExists("./artifacts/coverage.xml"))
    .WithCriteria(() => !string.IsNullOrEmpty(coverallsRepoToken))
    .Does(() =>
{
	// N.A.
});

// Run the XUnit tests via OpenCover, so be get an coverage.xml report
Task("Coverage")
    .IsDependentOn("Build")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
	.WithCriteria(() => GetFiles("./**/*.csproj").Where(csprojFile => csprojFile.FullPath.Contains("Test")).Any())
    .Does(() =>
{
});

// This starts the actual MSBuild
Task("Build")
    .IsDependentOn("RestoreNuGetPackages")
    .IsDependentOn("Clean")
    .IsDependentOn("AssemblyVersion")
    .Does(() =>
{
    var settings = new MSBuildSettings {
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2017,
        Configuration = configuration,
        PlatformTarget = PlatformTarget.MSIL
    };

    MSBuild(solutionFilePath.FullPath, settings);
    
    // Make sure the .dlls in the obj path are not found elsewhere
    CleanDirectories("./**/obj");
});

// Generate Git links in the PDB files
Task("GitLink")
    .IsDependentOn("Build")
    .Does(() =>
{
	// N.A.
});

// Load the needed NuGet packages to make the build work
Task("RestoreNuGetPackages")
    .Does(() =>
{
    NuGetRestore(solutionFilePath.FullPath);
});

// Version is written to the AssemblyInfo files when !BuildSystem.IsLocalBuild
Task("AssemblyVersion")
    .Does(() =>
{
    foreach(var assemblyInfoFile in  GetFiles("./**/AssemblyInfo.cs").Where(p => p.FullPath.Contains(solutionName))) {
        var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile.FullPath);
        CreateAssemblyInfo(assemblyInfoFile.FullPath, new AssemblyInfoSettings {
            Version = version,
            InformationalVersion = version,
            FileVersion = version,

            CLSCompliant = assemblyInfo.ClsCompliant,
            Company = assemblyInfo.Company,
            ComVisible = assemblyInfo.ComVisible,
            Configuration = assemblyInfo.Configuration,
            Copyright = assemblyInfo.Copyright,
            //CustomAttributes = assemblyInfo.CustomAttributes,
            Description = assemblyInfo.Description,
            //Guid = assemblyInfo.Guid,
            InternalsVisibleTo = assemblyInfo.InternalsVisibleTo,
            Product = assemblyInfo.Product,
            Title = assemblyInfo.Title,
            Trademark = assemblyInfo.Trademark
        });
    }
});

Task("EnableDNC30")
    .Does(() =>
{
    ReplaceRegexInFiles("./**/*.csproj", "<TargetFrameworks>.*</TargetFrameworks><!-- net471;netcoreapp3.0 -->", "<TargetFrameworks>net471;netcoreapp3.0</TargetFrameworks>");
    ReplaceRegexInFiles("./**/*.csproj", "<Project Sdk=\"MSBuild.Sdk.Extras/1.6.65\"><!-- Microsoft.NET.Sdk.WindowsDesktop -->", "<Project Sdk=\"Microsoft.NET.Sdk.WindowsDesktop\">");
});

Task("DisableDNC30")
    .Does(() =>
{
    ReplaceRegexInFiles("./**/*.csproj", "<TargetFrameworks>net471;netcoreapp3.0</TargetFrameworks>", "<TargetFrameworks>net471</TargetFrameworks><!-- net471;netcoreapp3.0 -->");
    ReplaceRegexInFiles("./**/*.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.WindowsDesktop\">", "<Project Sdk=\"MSBuild.Sdk.Extras/1.6.65\"><!-- Microsoft.NET.Sdk.WindowsDesktop -->");
});

// Clean all unneeded files, so we build on a clean file system
Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/obj");
    CleanDirectories("./**/bin");
    CleanDirectories("./artifacts");	
});

RunTarget(target);