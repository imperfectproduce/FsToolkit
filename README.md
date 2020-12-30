# FsToolkit
FSharp Toolkit - helpful packages for everyday F# programming

All packages target `netstandard2.0`. There is one `.nuspec` file per project in the solution (each project is a package).

Packages are automatically published to AppVeyor project feeds with version pattern _&lt;major>.&lt;minor>.&lt;ci build number>_ via a CI post-build script like

    nuget pack "FsToolkit.Json\FsToolkit.Json.nuspec" -properties build_number=$Env:APPVEYOR_BUILD_NUMBER
    Get-ChildItem .\*.nupkg | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }

Major and Minor numbers should be manually incremented in `.nuspec` files by developers as appropriate.

Packages are all published to Imperfect's private account feed https://ci.appveyor.com/nuget/relayfoods

## Project-specific Readme's

- [FsToolkit.Json](https://github.com/relayfoods/FsToolkit/tree/master/FsToolkit.Json)

