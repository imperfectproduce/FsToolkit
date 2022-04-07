# FsToolkit
Helpful packages for everyday F# programming

All packages target `netstandard2.0`. There is one `.nuspec` file per non-test project in the solution (each non-test project is a package).

Packages are automatically published to nuget.org via circleci build workflow. There are some differences to how we publish packages to nuget.org with circleci compared to how we published packages to our private AppVeyor:
* we no longer auto-increment the package version patch number in our coninuous integration builds. Package version numbers must be manually set in `.nuspec` files.
* all branches are published, allowing publish of alpha-versioned packages from feature branches if desired. But the `--skip-duplicate` flag is used in the `dotnet nuget push` command so packages only published when package version numbers are updated in the `.nuspec` file.
* all packages use `Imperfect.` id-suffix like `Imperfect.FsToolkit.Json` instead of `FsToolkit.Json`. This is to differentiate our packages from existing `FsToolkit`-suffixed packages in nuget.org 
* since we are publishing to the central nuget.org repository, we no longer need a custom source in `NuGet.Config` files for these packages.

Recommended workflow for publishing new package versions:
* create a PR and once approved merge to master
* make a direct commit to master to increment package version number(s) in appropriate `.nuspec` file(s)
* the circleci job will automatically publish the new package version(s)

## Project-specific Readme's

- [FsToolkit](https://github.com/relayfoods/FsToolkit/tree/master/FsToolkit)
- [FsToolkit.Json](https://github.com/relayfoods/FsToolkit/tree/master/FsToolkit.Json)

