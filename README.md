# F# language support in JetBrains Rider
[![JetBrains incubator project](http://jb.gg/badges/official.svg)](https://confluence.jetbrains.com/display/ALL/JetBrains+on+GitHub)

F# support plugin is implemented as two parts: a plugin for ReSharper.Host and a plugin for IntelliJ targeting Rider. ReSharper.Host is a ReSharper version modified to work as a backend for IntelliJ.

The backend part plugin adds F# support to ReSharper and is implemented in ReSharper.FSharp solution.
The frontend part defines a new IntelliJ language and is used to delegate the most of the work to the backend. This part also adds F# Interactive support.

The plugin uses [FSharp.Compiler.Service](https://github.com/Microsoft/visualfsharp) and [Fantomas](https://github.com/dungpa/fantomas) projects made available by contributors.

## Build

### Requirements

* .NET Framework 4.5.1 SDK (Windows only for now, please check the details below)
* .NET Core SDK 2.0+ (MSBuild 15 and F# build targets are needed)
* JDK 1.8

The backend Rider.SDK references assemblies that are part of .NET Framework and aren't shipped with Mono (e.g. PresentationFramework). Building on Mono however is possible using .NET Framework facades obtained as a NuGet package (take a look at [dotnet/sdk#335](https://github.com/dotnet/sdk/issues/335#issuecomment-330772137)) and is going to be added later.

The plugin uses [gradle-intellij-plugin](https://github.com/JetBrains/gradle-intellij-plugin) Gradle plugin that downloads needed IntelliJ-based SDK, packs the plugin and installs it to a sandboxed IDE or its test shell allowing testing the plugin in a separate environment.

### Building the plugin and launching sandboxed Rider 

*  build `Debug` configuration in ReSharper.FSharp solution. The output assemblies are later copied to the frontend plugin directories by Gradle.

* open `rider-fsharp` project in IntelliJ IDEA. Gradle will download Rider SDK and set up all dependencies. To launch Rider with the plugin installed execute `runIde` Gradle task. This task will also build the frontend plugin part and install the plugin to the sandbox before launch.

Alternatively Gradle command line may be used for frontend part building and launching Rider. Use `$ rider-fsharp/gradlew runIde` command.

### Installing to existing Rider

* build `Debug` configuration in ReSharper.FSharp solution
* execute `buildPlugin` Gradle task
* install to Rider using `Install plugin from disk` action.

## Contributing

There're plenty of open issues on Rider [issue tracker](https://youtrack.jetbrains.com/issues?q=in:%20rider%20%23Unresolved%20Technology:%20FSharp). Although there're no Up for Grabs tags yet, contributions are welcome. Some issues are marked as [third-party problem](https://youtrack.jetbrains.com/issues/RIDER?q=Technology:%20FSharp%20%20state:%20%7BThird%20party%20problem%7D) and a fix is needed in FCS or other tools we depend on. If you're willing to work on an issue please leave a note on the issue page so the team wouldn't work on it at the same time and could assist you if needed. To propose a change please fork this repo and open a Pull Request. Public build on CI server is going to be available shortly.

The new code is usually being written in F# (except for `FSharp.Psi` project) and initial implementations in `Daemon.FSharp` and `Services.FSharp` projects are moved to `FSharp.Psi.Servises` during refactorings and fixes.

The project lacks a good suite of tests and only few are implemented right now. There's currently no requirement on adding new ones yet, but as soon as more things are covered adding new tests for changed code should become necessary for a Pull Request to be accepted.

Documentation for SDK used is available here:

* [ReSharper SDK](https://www.jetbrains.com/help/resharper/sdk/README.html)
* [IntelliJ IDEA SDK](https://www.jetbrains.org/intellij/sdk/docs/welcome.html)


## Development notes

The main development is currently being done in `master` branch and builds from this branch are bundled with Rider nightly updates available in [Toolbox App](https://www.jetbrains.com/toolbox/app/). When preparing a release or EAP build, changes are cherry-picked to the corresponding release branch like `wave10-rider-release`.

The project depends on nightly SDK builds and if some particular or previous build is needed it may be changed in [rider-fsharp/build.gradle](rider-fsharp/build.gradle) and [ReSharper.FSharp/Directory.Build.props](ReSharper.FSharp/Directory.Build.props).

Debugging the backeng plugin is possible by attaching to ReSharper.Host process launched in `runIde` Gradle task. To debug the frontend plugin part, start `runIde` task in Debug mode.

JVM and .NET Rider parts communicate using RdProtocol with APIs available on both sides. For plugin backend-frontend communication the RdProtocol should be used as well. However, it's not currently possible to extend the protocol from a plugin and should be done in Rider code. Some extensions needed for the plugin are already defined there. Please check [RIDER-4217](https://youtrack.jetbrains.com/issue/RIDER-4217) for updates. Meanwhile if a protocol extension is needed please raise an issue.

Gradle downloads a newer frontend SDK when it is available and keeps it in its caches. However, using Gradle cache directory leads to exceeding allowed path length for .NET assembly loader making some ReSharper.Host assemblies fail to load. When IntelliJ SDK is set to Rider the following workaround is used: the SDK is copied to the project `build` directory so assemblies can be loaded (assuming the path length is shorter than in the Gradle cache). This workaround, however, prevents Rider SDK from being updated automatically by Gradle. For details and a workaround please take a look at [gradle-intellij-plugin#234](https://github.com/JetBrains/gradle-intellij-plugin/issues/234).

## Some nearest goals

* Provide PSI modules for scripts (i.e. a ReSharper project context with referenced assemblies and a target framework) to enable ReSharper navigation and other features in such files.
* Cover more things with tests, e.g. add tests for mapping FCS representations to R# for declared elements and types.