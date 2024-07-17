import com.jetbrains.plugin.structure.base.utils.isFile
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.tasks.PrepareSandboxTask
import org.jetbrains.intellij.platform.gradle.tasks.RunIdeTask
import org.jetbrains.kotlin.daemon.common.toHexString
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import kotlin.io.path.absolutePathString

plugins {
  // Version is configured in gradle.properties
  id("me.filippov.gradle.jvm.wrapper")
  id("org.jetbrains.grammarkit")
  id("org.jetbrains.intellij.platform")
  kotlin("jvm")
}

apply {
  plugin("kotlin")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
  maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
  maven("https://cache-redirector.jetbrains.com/maven-central")
  intellijPlatform {
    defaultRepositories()
    jetbrainsRuntime()
  }
}

val riderBaseVersion: String by project
val buildCounter = ext.properties["build.number"] ?: "9999"
version = "$riderBaseVersion.$buildCounter"

dependencies {
  testImplementation("junit:junit:4.13.2")
  testRuntimeOnly("org.junit.vintage:junit-vintage-engine:5.9.2")
  testRuntimeOnly("org.junit.platform:junit-platform-launcher:1.9.2")
  intellijPlatform {
    val dir = file("build/rider")
    if (dir.exists()) {
      logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
      local(dir)
    } else {
      logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
      rider("$riderBaseVersion-SNAPSHOT")
    }
    jetbrainsRuntime()
    bundledPlugin("JavaScript")
    bundledPlugin("com.intellij.css")
    bundledPlugin("com.intellij.database")
    bundledPlugin("com.intellij.ml.llm")
    bundledPlugin("org.intellij.intelliLang")
    bundledPlugin("org.jetbrains.plugins.textmate")
    bundledPlugin("rider.intellij.plugin.appender")
    bundledLibrary("lib/testFramework.jar")
    instrumentationTools()
  }
}

val isMonorepo = rootProject.projectDir != projectDir
val repoRoot: File = projectDir.parentFile
val resharperPluginPath = repoRoot.resolve("ReSharper.FSharp")

val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"
val primaryTargetFramework = "net472"
val outputRelativePath = "bin/$buildConfiguration/$primaryTargetFramework"
val ktOutputRelativePath = "src/main/java/com/jetbrains/rider/plugins/fsharp/protocol"

if (!isMonorepo) {
  sourceSets.getByName("main") {
    java {
      srcDir(repoRoot.resolve("rider-fsharp/src/generated/java"))
    }
    kotlin {
      srcDir(repoRoot.resolve("rider-fsharp/src/generated/kotlin"))
    }
  }
}

val libFiles = listOf(
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.dll",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.xml",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Compiler.Service.dll", // todo: add pdb after next repack
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.DependencyManager.Nuget.dll",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Compiler.Interactive.Settings.dll"
)

val pluginFiles = listOf(
  "FSharp/FSharp.ProjectModelBase/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase",
  "FSharp/FSharp.Common/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Common",
  "FSharp/FSharp.Psi/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi",
  "FSharp/FSharp.Psi.Services/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Services",
  "FSharp/FSharp.Psi.Daemon/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon",
  "FSharp/FSharp.Psi.Intentions/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions",
  "FSharp/FSharp.Psi.Features/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Features",
  "FSharp/FSharp.Fantomas.Protocol/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol",
  "FSharp/FSharp.TypeProviders.Protocol/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol"
)

val typeProvidersFiles = listOf(
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.dll",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.xml",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.pdb",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe.config",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.dll",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.pdb",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.win.runtimeconfig.json",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.unix.runtimeconfig.json"
)

val fantomasHostFiles = listOf(
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.dll",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.pdb",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.deps.json",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Core.dll",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.FCS.dll",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Host.win.runtimeconfig.json",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Host.unix.runtimeconfig.json"
)

val externalAnnotationsDirectory = "$resharperPluginPath/src/FSharp/annotations"

val nugetConfigPath = File(repoRoot, "NuGet.Config")
val dotNetSdkPathPropsPath = File("build", "DotNetSdkPath.generated.props")

val riderFSharpTargetsGroup = "rider-fsharp"

fun File.writeTextIfChanged(content: String) {
  val bytes = content.toByteArray()

  if (!exists() || readBytes().toHexString() != bytes.toHexString()) {
    println("Writing $path")
    writeBytes(bytes)
  }
}

val riderModel: Configuration by configurations.creating {
  isCanBeConsumed = true
  isCanBeResolved = false
}

val platformLibConfiguration: Configuration by configurations.creating {
  isCanBeConsumed = true
  isCanBeResolved = false
}

val platformLibFile = project.layout.buildDirectory.file("platform.lib.txt")
val resolvePlatformLibPath = tasks.create("resolvePlatformLibPath") {
  dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
  outputs.file(platformLibFile)
  doLast {
    platformLibFile.get().asFile.writeTextIfChanged(intellijPlatform.platformPath.resolve("lib").absolutePathString())
  }
}

artifacts {
  add(riderModel.name, provider {
    val sdkRoot = intellijPlatform.platformPath
    sdkRoot.resolve("lib/rd/rider-model.jar").also {
      check(it.isFile) {
        "rider-model.jar is not found at $riderModel"
      }
    }
  }) {
    builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
  }

  add(platformLibConfiguration.name, provider { resolvePlatformLibPath.outputs.files.singleFile }) {
    builtBy(resolvePlatformLibPath)
  }
}

tasks {
  val generateDisabledPluginsTxt by registering {
    val out = layout.buildDirectory.file("disabled_plugins.txt")
    outputs.file(out)
    doLast {
      file(out).writeText(
        """
          com.intellij.ml.llm
          com.intellij.swagger
        """.trimIndent()
      )
    }
  }

  withType<PrepareSandboxTask> {
    dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    var files = libFiles + pluginFiles.map { "$it.dll" } + pluginFiles.map { "$it.pdb" }
    files = files.map { "$resharperPluginPath/src/$it" }
    val fantomasHostFiles = fantomasHostFiles.map { "$resharperPluginPath/src/$it" }
    val typeProvidersFiles = typeProvidersFiles.map { "$resharperPluginPath/src/$it" }

    if (name == Constants.Tasks.PREPARE_TEST_SANDBOX) {
      val testHostPath = "$resharperPluginPath/src/FSharp/FSharp.Tests.Host/$outputRelativePath"
      val testHostName = "$testHostPath/JetBrains.ReSharper.Plugins.FSharp.Tests.Host"
      files = files + listOf("$testHostName.dll", "$testHostName.pdb")

      dependsOn(generateDisabledPluginsTxt)
      from(generateDisabledPluginsTxt.get().outputs.files.singleFile) {
        into("../config-test")
      }
    }

    fun moveToPlugin(files: List<String>, destinationFolder: String) {
      files.forEach {
        from(it) { into("${intellijPlatform.projectName.get()}/$destinationFolder") }
      }
    }

    moveToPlugin(files, "dotnet")
    moveToPlugin(fantomasHostFiles, "fantomas")
    moveToPlugin(typeProvidersFiles, "typeProviders")
    moveToPlugin(listOf("projectTemplates"), "projectTemplates")
    moveToPlugin(listOf(externalAnnotationsDirectory), "dotnet/Extensions/com.jetbrains.rider.fsharp/annotations")

    doLast {
      fun validateFiles(files: List<String>, destinationFolder: String) {
        files.forEach {
          val file = file(it)
          if (!file.exists()) throw RuntimeException("File $file does not exist")
          logger.warn("$name: ${file.name} -> $destinationDir/${intellijPlatform.projectName.get()}/$destinationFolder")
        }
      }
      validateFiles(files, "dotnet")
      validateFiles(fantomasHostFiles, "fantomas")
      validateFiles(typeProvidersFiles, "typeProviders")
      validateFiles(listOf(externalAnnotationsDirectory), "dotnet/Extensions/com.jetbrains.rider.fsharp/annotations")
    }
  }

  // Initially introduced in:
  // https://github.com/JetBrains/ForTea/blob/master/Frontend/build.gradle.kts
  withType<RunIdeTask> {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"
  }

  withType<KotlinCompile> {
    kotlinOptions.jvmTarget = "17"
    dependsOn(":protocol:rdgen", ":lexer:generateLexer")
  }

  val parserTest by register<Test>("parserTest") {
    useJUnitPlatform()
  }

  named<Test>("test") {
    dependsOn(parserTest)
    useTestNG {
      groupByInstances = true
    }
  }

  withType<Test> {
    testLogging {
      showStandardStreams = true
      exceptionFormat = TestExceptionFormat.FULL
    }
    val rerunSuccessfulTests = false
    outputs.upToDateWhen { !rerunSuccessfulTests }
    ignoreFailures = true
  }

  val writeDotNetSdkPathProps = create("writeDotNetSdkPathProps") {
    dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    group = riderFSharpTargetsGroup
    inputs.property("platformPath") { intellijPlatform.platformPath.toString() }
    outputs.file(dotNetSdkPathPropsPath)
    doLast {
      dotNetSdkPathPropsPath.writeTextIfChanged(
        """<Project>
  <PropertyGroup>
    <DotNetSdkPath>${intellijPlatform.platformPath.resolve("lib").resolve("DotNetSdkForRdPlugins").absolutePathString()}</DotNetSdkPath>
  </PropertyGroup>
</Project>
"""
      )
    }

    getByName("buildSearchableOptions") {
      enabled = buildConfiguration == "Release"
    }
  }

  val writeNuGetConfig = create("writeNuGetConfig") {
    dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    group = riderFSharpTargetsGroup
    inputs.property("platformPath") { intellijPlatform.platformPath.toString() }
    outputs.file(nugetConfigPath)
    doLast {
      nugetConfigPath.writeTextIfChanged(
        """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="resharper-sdk" value="${intellijPlatform.platformPath.resolve("lib").resolve("DotNetSdkForRdPlugins").absolutePathString()}" />
  </packageSources>
</configuration>
"""
      )
    }
  }

  named("assemble") {
    doLast {
      logger.lifecycle("Plugin version: $version")
      logger.lifecycle("##teamcity[buildNumber '$version']")
    }
  }

  val prepare = create("prepare") {
    group = riderFSharpTargetsGroup
    dependsOn(":protocol:rdgen", writeNuGetConfig, writeDotNetSdkPathProps, ":lexer:generateLexer")
  }

  val buildReSharperPlugin by registering(Exec::class) {
    group = riderFSharpTargetsGroup
    dependsOn(prepare)

    executable = "dotnet"
    args("build", "$resharperPluginPath/ReSharper.FSharp.sln")
  }

  wrapper {
    gradleVersion = "8.7"
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-bin.zip"
  }

  defaultTasks(prepare)
}
