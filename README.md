# F# language support in JetBrains Rider

[![JetBrains official project](http://jb.gg/badges/official.svg)](https://confluence.jetbrains.com/display/ALL/JetBrains+on+GitHub)
[![Join the chat at https://gitter.im/JetBrains/fsharp-support](https://badges.gitter.im/JetBrains/fsharp-support.svg)](https://gitter.im/JetBrains/fsharp-support?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

F# support in Rider is implemented as a plugin made of two major components: 
* ReSharper.Host plugin (referred to as the *backend*) that adds F# support to ReSharper and is implemented in ReSharper.FSharp solution. ReSharper.Host is a modification of ReSharper used as a language service that the IntelliJ Platform interacts with. The backend is written in C# and F#.
* IntelliJ Platform plugin for Rider (referred to as the *frontend*) that defines F# as a new IntelliJ Platform language but delegates most of the work to the backend. This part also adds F# Interactive support. The frontend is written in Kotlin.

F# support in Rider makes use of open source software, most notably [FSharp.Compiler.Service](https://github.com/Microsoft/visualfsharp) and [Fantomas](https://github.com/dungpa/fantomas).

## Building the plugin

### Requirements

* [.NET Framework 4.6.1 Developer Pack](https://www.microsoft.com/en-us/download/details.aspx?id=49978)
* [.NET Core SDK 2.0+](https://www.microsoft.com/net/download/windows) for MSBuild 15 and F# build targets
* [JDK 1.8](http://www.oracle.com/technetwork/java/javase/downloads/jdk8-downloads-2133151.html)

### Optional

* [IntelliJ IDEA](https://www.jetbrains.com/idea/)
* [JetBrains Rider](https://www.jetbrains.com/rider/) or a different .NET IDE

### Building the plugin and launching Rider in a sandbox 

1. Install SDK and prepare backend plugin build using Gradle
    * if using IntelliJ IDEA:

	     Open the `rider-fsharp` project in IntelliJ IDEA. When suggested to import Gradle projects, accept the suggestion: Gradle will download Rider SDK and set up all necessary dependencies. `rider-fsharp` uses the [gradle-intellij-plugin](https://github.com/JetBrains/gradle-intellij-plugin) Gradle plugin that downloads the IntelliJ Platform SDK, packs the F# plugin and installs it into a sandboxed IDE or its test shell, which allows testing the plugin in a separate environment.

	     Open the *Gradle* tool window in IntelliJ IDEA (*View | Tool Windows | Gradle*), and execute the `rider-fsharp/prepare` task.

    * if using Gradle command line:

        ```
        $ ./rider-fsharp/gradlew prepare
        ```

2. Open `ReSharper.FSharp.sln` solution and build using the `Debug` configuration. The output assemblies are later copied to the frontend plugin directories by Gradle. (If you're seeing build errors in Rider, choose *File | Settings | Build, Execution, Deployment | Toolset and Build*, and in the *Use MSBuild version* drop-down, make sure that Rider uses MSBuild shipped with .NET Core SDK.)

3. Launch Rider with the plugin installed

    * if using IntelliJ IDEA:

        Open the *Gradle* tool window in IntelliJ IDEA (*View | Tool Windows | Gradle*), and execute the `intellij/runIde` task. This will build the frontend, install the plugin to a sandbox, and launch Rider with the plugin.

    * if using Gradle command line:

        ```
        $ rider-fsharp/gradlew runIde
        ```

### Installing to an existing Rider instance

1. Build the `Debug` configuration in `ReSharper.FSharp.sln`.
2. Execute the `buildPlugin` Gradle task.
3. Install the plugin (`rider-fsharp/build/distributions/*.zip`) to your Rider installation [from disk](https://www.jetbrains.com/help/idea/installing-a-plugin-from-the-disk.html).

## Contributing

We welcome contributions that address any F# plugin issues that are open in Rider's [issue tracker](https://youtrack.jetbrains.com/issues?q=in:%20rider%20%23Unresolved%20Technology:%20FSharp). Some of these issues are marked as [Up for grabs](https://youtrack.jetbrains.com/issues/RIDER?q=Technology:%20FSharp%20%23Unresolved%20tag:%20%7BUp%20For%20Grabs%7D): we expect issues tagged this way to be easier addressed by external contributors as they are unlikely to require any changes outside the F# plugin. Note that some issues are marked as [third-party problems](https://youtrack.jetbrains.com/issues/RIDER?q=Technology:%20FSharp%20%20state:%20%7BThird%20party%20problem%7D), and addressing them requires fixes from FCS or other projects that this plugin depends on.

If you are willing to work on an issue, please *leave a comment* under the issue. Doing this will make sure that the team doesn't start working on the same issue, and help you get any necessary assistance.

New code is usually written in F#, except for the `FSharp.Psi` project that is written in C#.

As soon as you are done with changes in your fork, please open a pull request for review.

Note that the public CI server is not set up at this point but it's going to be available shortly.

The project currently lacks a solid test suite. There's currently no requirement to add new tests, but as soon as more functionality is covered, adding new tests along with code changes will become necessary for a PR to be accepted.

We suggest that you read docs on the two SDKs that this plugin uses:

* [ReSharper SDK](https://www.jetbrains.com/help/resharper/sdk/README.html)
* [IntelliJ IDEA SDK](https://www.jetbrains.org/intellij/sdk/docs/welcome.html)


## Development notes

`master` is currently the main development branch, and builds from this branch are bundled with nightly Rider 2018.2 builds available via [JetBrains Toolbox App](https://www.jetbrains.com/toolbox/app/) when 2018.2 nightlies become public. 2018.1.x updates are built using `wave12` branch. When preparing a release or EAP build, changes are cherry-picked to the corresponding release branch like `wave10-rider-release`.

By default, the project depends on nightly SDK builds, but a specific SDK version can be referenced in [rider-fsharp/build.gradle](rider-fsharp/build.gradle.kts) if necessary.

To update to the latest frontend SDK, run the `intellij/prepare` Gradle task in IntelliJ IDEA which will download both Rider SDK and ReSharper SDK. To restore ReSharper SDK in projects run *Tools | NuGet | NuGet Force Restore* in Rider. The force restore is currently needed for floating package versions like `2017.3-SNAPSHOT*` ([RIDER-11395](https://youtrack.jetbrains.com/issue/RIDER-11395)).

To debug the backend, attach to the ReSharper.Host process launched via the `runIde` Gradle task. To debug the frontend, start the `runIde` task in Debug mode.

Rider's JVM-based frontend and .NET-based backend communicate using RdProtocol with APIs available on both sides. For backend-frontend communication in plugins, RdProtocol should be used as well. Protocol model is defined in [this file](https://github.com/JetBrains/fsharp-support/blob/master/rider-fsharp/protocol/src/kotlin/model/RdFSharpModel.kt).

## Known issues

As soon as you build the backend for the first time, Rider will show false red code warnings across the backend's F# projects. This is due to a bug in Rider waiting to be fixed ([RIDER-11392](https://youtrack.jetbrains.com/issue/RIDER-11392)). As a workaround, you can unload all projects in `ReSharper.FSharp.sln`, and then reload them.

## Roadmap

* Cover more functionality with tests, e.g. add tests for mapping FCS representations to ReSharper for declared elements and types.
* Set up a public Continuous Integration server for test runs.
* Enable development on macOS and Linux.
