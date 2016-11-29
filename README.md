[FsToolkit.Json](https://github.com/relayfoods/FsToolkit/tree/master/FsToolkit.Json)

---

# FsToolkit
FSharp Toolkit - helpful packages for everyday F# programming

Requires VS 2015 with Paket (for package restore on build) and NUnit 3 Test Adapter (for running tests) extensions installed.

All packages target `net452`. There is one `.nuspec` file per project in the solution (each project is a package).

Packages are automatically published to AppVeyor project feeds with version pattern _&lt;major>.&lt;minor>.&lt;ci build number>_ via a CI post-build script like

    nuget pack "FsToolkit.Json\FsToolkit.Json.nuspec" -properties build_number=$Env:APPVEYOR_BUILD_NUMBER
    Get-ChildItem .\*.nupkg | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }

Major and Minor numbers should be manually incremented in `.nuspec` files by developers as appropriate.

The following are the package feeds:
  - https://ci.appveyor.com/nuget/fstoolkit-json
