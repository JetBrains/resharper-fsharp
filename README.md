# F# language support in JetBrains Rider
[![JetBrains incubator project](http://jb.gg/badges/official.svg)](https://confluence.jetbrains.com/display/ALL/JetBrains+on+GitHub)

F# support plugin is implemented in two parts: a plugin for ReSharper.Host and a plugin for IntelliJ targeting Rider. ReSharper.Host is a ReSharper version modified to work as a backend for IntelliJ.

The backend part plugin adds F# support to ReSharper and is implemented in ReSharper.FSharp solution.
The frontend part defines a new IntelliJ language and is used to delegate the work to the backend. This part also adds F# Interactive support.

## Build

Requirements:

* .NET Framework 4.5.1 SDK (Windows only for now, please check details below)
* JDK 1.8

The backend Rider.SDK references assemblies that are part of .NET Framework and aren't shipped with Mono (e.g. PresentationFramework). Build on Mono is possible using .NET Framework facades obtained as a NuGet package (take a look at [dotnet/sdk#335](https://github.com/dotnet/sdk/issues/335#issuecomment-330772137)) and is going to be added later.

The plugin uses [gradle-intellij-plugin](https://github.com/JetBrains/gradle-intellij-plugin) Gradle plugin that downloads needed IntelliJ-based SDK, packs the plugin and executes it in the desired IDE or its test shell environment.

### Using Gradle and sandboxed Rider 

*  build the backend part by building `Debug` configuration in ReSharper.FSharp solution. The output assemblies are later copied to the frontend plugin directories by Gradle.

* open `rider-fsharp` project in IntelliJ IDEA. Gradle will download Rider SDK and set up all dependencies. To launch Rider with the plugin installed execute `runIde` Gradle task. This task will also build the frontend plugin and install it to Rider before launch.

Alternatively Gradle command line may be used for frontend part building and launching Rider. Use `$ rider-fsharp/gradlew runIde` command.

### Installing to existing Rider

To install the plugin to existing Rider instance, build backend plugin part and execute `buildPlugin` Gradle task. To install use `Install plugin from disk` action in Rider.


## Development notes


Debugging the backeng plugin is possible by attaching to ReSharper.Host process. To debug the frontend part, start `runIde` task in Debug mode.

JVM and .NET Rider parts communicate using RdProtocol with APIs available on both sides. For plugin backend-frontend communication the RdProtocol should be used as well. However, it's not currently possible to extend the protocol from a plugin and should be done in Rider code. Some extensions needed for the plugin are already defined there. Please check [RIDER-4217](https://youtrack.jetbrains.com/issue/RIDER-4217) for updates. Meanwhile if a protocol extension is needed please raise an issue.

Gradle downloads a newer frontend SDK when it is available and keeps it in its caches. However, using Gradle cache directory leads to exceeding allowed path length for .NET assembly loader making some ReSharper.Host assemblies fail to load. When IntelliJ SDK is set to Rider the following workaround is used: the SDK is copied to the project `build` directory so assemblies can be loaded (assuming the path length is shorter than in the Gradle cache). This workaround, however, prevents Rider SDK from being updated automatically. For details and a workaround please take a look at [gradle-intellij-plugin#234](https://github.com/JetBrains/gradle-intellij-plugin/issues/234).