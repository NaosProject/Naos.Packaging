<a target="_blank" href="https://ci.appveyor.com/project/Naos-Project/naos-packaging">
![Build status](https://ci.appveyor.com/api/projects/status/github/NaosProject/Naos.Packaging?branch=master&svg=true)
</a>
<br/> 
<a target="_blank" href="http://nugetstatus.com/packages/Naos.Packaging">
![NuGet Status](http://nugetstatus.com/Naos.Packaging.png)
</a>

Naos.Packaging
================
A wrapper around downloading NuGet packages from public and private repositories.  Using latest version so it supports V3 depedencies as well as V2.

Use - Referencing in your code
-----------
It's best to reference the NuGet package: <a target="_blank" href="http://www.nuget.org/packages/Naos.Packaging.NuGet">http://www.nuget.org/packages/Naos.Packaging.NuGet</a> which will provide a more feature rich interaction with packages...
The specific NuGet operations using the prototcol are included in a single file which can be copied into your project without a Naos depedency in your project (will still REQUIRE package NuGet.PackageManagement): <a target="_blank" href="https://raw.githubusercontent.com/NaosProject/Naos.Packaging/master/Naos.Packaging.NuGet/NuGetPackageManager.cs">https://raw.githubusercontent.com/NaosProject/Naos.Packaging/master/Naos.Packaging.NuGet/NuGetPackageManager.cs</a>.

```C#
// this is how you would use the full retriever
[Fact(Skip = "Meant for local debugging and to show usage.")]
public void DownloadPrivate()
{
		var repoConfig = new PackageRepositoryConfiguration
							{
								Source = "https://ci.appveyor.com/nuget/XXX",
								ClearTextPassword = "ThisIsPassword",
								Username = "ThisIsUser",
								SourceName = "ThisIsGalleryName",
								ProtocolVersion = 2,
							};

	var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
	var pm = new PackageRetriever(repoConfig, defaultWorkingDirectory);
	var bundleAllDependencies = false;
	var package = pm.GetPackage(new PackageDescription { Id = "ThisIsPackage" }, bundleAllDependencies);
	Assert.NotNull(package.PackageFileBytes);
}

[Fact(Skip = "Meant for local debugging and to show usage.")]
public void DownloadPublic()
{
	var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
	var pm = new PackageRetriever(defaultWorkingDirectory);
	var bundleAllDependencies = false;
	var package = pm.GetPackage(new PackageDescription { Id = "Newtonsoft.Json" }, bundleAllDependencies);
	Assert.NotNull(package.PackageFileBytes);
}

// this is how you would use the NuGet only file
[Fact(Skip = "Meant for local debugging and to show usage.")]
public void DownloadPrivate()
{
	var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
	var downloadDirectory = Path.Combine(defaultWorkingDirectory, Guid.NewGuid() + ".tmp");

	var pm = new NuGetPackageManager(2, "ThisIsGalleryName", "https://ci.appveyor.com/nuget/XXX", "ThisIsUser", "ThisIsPassword");

	var includeUnlisted = true;
	var includePreRelease = true;
	var latestVersionTask = pm.GetLatestVersionAsync("ThisIsPackageId", includeUnlisted, includePreRelease);
	latestVersionTask.Wait();
	var version = latestVersionTask.Result;

	var includeDependencies = true;

	pm.DownloadPackageToPathAsync(
		"ThisIsPackageid",
		version,
		downloadDirectory,
		includeDependencies,
		includeUnlisted,
		includePreRelease).Wait();
}

[Fact]
public void DownloadPublic()
{
	var defaultWorkingDirectory = @"D:\Temp\NewNuGet";
	var downloadDirectory = Path.Combine(defaultWorkingDirectory, Guid.NewGuid() + ".tmp");

	var pm = new NuGetPackageManager();

	var includeUnlisted = true;
	var includePreRelease = true;
	var latestVersionTask = pm.GetLatestVersionAsync("Newtonsoft.Json", includeUnlisted, includePreRelease);
	latestVersionTask.Wait();
	var version = latestVersionTask.Result;

	var includeDependencies = true;

	pm.DownloadPackageToPathAsync(
		"Newtonsoft.Json",
		version,
		downloadDirectory,
		includeDependencies,
		includeUnlisted,
		includePreRelease).Wait();
}
```