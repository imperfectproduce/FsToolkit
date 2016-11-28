# FsToolkit
FSharp Toolkit - helpful packages for everyday F# programming

Requires VS 2015 with Paket extension installed (for package restore on build).

All packages target `net452`. There is one `.nuspec` file per project in the solution (each project is a package).

Packages are automatically published to AppVeyor project feeds with version pattern _&lt;major>&lt;minor>.&lt;ci build number>_ (via Relay Foods CI builds). Major and Minor numbers should be manually incremented in `.nuspec` files by developers as appropriate.

The following are the package feeds:
  - https://ci.appveyor.com/nuget/fstoolkit-json
