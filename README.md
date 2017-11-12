# F# language support in JetBrains Rider
[![JetBrains incubator project](http://jb.gg/badges/official.svg)](https://confluence.jetbrains.com/display/ALL/JetBrains+on+GitHub)

Rider is built using IntelliJ platform and ReSharper.Host (a ReSharper variant modified to work as a backend for IntelliJ). The two parts communicate using RdProtocol with APIs available on both .NET and JVM sides.

F# support is implemented as two plugins: a plugin for ReSharper.Host and a plugin for IntelliJ targeting Rider.

The backend part plugin adds F# support to ReSharper and is implemented in ReSharper.FSharp solution.
The frontend part defines a new IntelliJ language which support should be delegated to the backend for most features. This part also adds F# Interactive support.
Some additional plugin overview is covered in this [paper](http://se.math.spbu.ru/SE/diploma/2017/pi/Auduchinok.pdf).


## Build

Requirements:

* .NET Framework 4.5.1 SDK (Windows only for now, please see details below)
* JDK 1.8

The backend Rider.SDK references assemblies that are part of .NET Framework and aren't shipped with Mono (e.g. PresentationFramework). Build on Mono is possible using .NET Framework facades obtained as a NuGet package and is going to be added later (take a look at [dotnet/sdk#335](https://github.com/dotnet/sdk/issues/335#issuecomment-330772137)).

The plugin uses [gradle-intellij-plugin](https://github.com/JetBrains/gradle-intellij-plugin) — a Gradle plugin that downloads needed IntelliJ-based SDK, packs the plugin and executes it in the desired IDE or its test shell environment.

To build the plugin, the backend part should be built first using Debug configuration in ReSharper.FSharp solution. The output assemblies are later copied to the frontend plugin directories by Gradle.

To build the frontend part either IntelliJ IDEA or gradlew may be used.
For the first option open `rider-fsharp` project. Gradle will download Rider SDK and set all dependencies. To launch Rider with the plugin installed execute `runIde` Gradle task. To build the plugin for installing into another Rider instance use `buildPlugin` task.


## Notes on development

Debugging the backeng plugin is possible by attaching to ReSharper.Host process. To debug the frontend part, start `runIde` task in Debug mode.

For backend-frontend communication RdProtocol should be used. Currently it's not possible to extend it from a plugin and should be done in Rider code and some extensions are defined there. For details please look at [RIDER-4217](https://youtrack.jetbrains.com/issue/RIDER-4217).

Gradle downloads a newer frontend SDK when it is available and keeps it in its caches. However, using Gradle cache directory leads to exceeding allowed path length for .NET assembly loader making some ReSharper.Host assemblies fail to load. When IntelliJ SDK is set to Rider the following workaround is used: the SDK is copied to the project `build` directory so assemblies can be loaded (assuming the path length is shorter than in the Gradle cache). This workaround, however, prevents Rider SDK from being updated automatically. For details and a workaround please take a look at [gradle-intellij-plugin#234](https://github.com/JetBrains/gradle-intellij-plugin/issues/234).
