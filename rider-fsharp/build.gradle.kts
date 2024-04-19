import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.intellij.IntelliJPluginConstants
import org.jetbrains.intellij.tasks.InstrumentCodeTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.intellij.tasks.RunIdeTask
import org.jetbrains.kotlin.daemon.common.toHexString
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
  // Version is configured in gradle.properties
  id("me.filippov.gradle.jvm.wrapper")
  id("org.jetbrains.grammarkit")
  id("org.jetbrains.intellij")
  kotlin("jvm")
}

dependencies {
  testImplementation("junit:junit:4.13.2")
  testRuntimeOnly("org.junit.vintage:junit-vintage-engine:5.9.2")
  testRuntimeOnly("org.junit.platform:junit-platform-launcher:1.9.2")
}

apply {
  plugin("kotlin")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
  maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
  maven("https://cache-redirector.jetbrains.com/maven-central")
}

val baseVersion = "2024.2"
val buildCounter = ext.properties["build.number"] ?: "9999"
version = "$baseVersion.$buildCounter"

intellij {
  type.set("RD")

  // Download a version of Rider to compile and run with. Either set `version` to
  // 'LATEST-TRUNK-SNAPSHOT' or 'LATEST-EAP-SNAPSHOT' or a known version.
  // This will download from www.jetbrains.com/intellij-repository/snapshots or
  // www.jetbrains.com/intellij-repository/releases, respectively.
  // Note that there's no guarantee that these are kept up-to-date
  // version = 'LATEST-TRUNK-SNAPSHOT'
  // If the build isn't available in intellij-repository, use an installed version via `localPath`
  // localPath = '/Users/matt/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/171.4089.265/Rider EAP.app/Contents'
  // localPath = "C:\\Users\\Ivan.Shakhov\\AppData\\Local\\JetBrains\\Toolbox\\apps\\Rider\\ch-0\\171.4456.459"
  // localPath = "C:\\Users\\ivan.pashchenko\\AppData\\Local\\JetBrains\\Toolbox\\apps\\Rider\\ch-0\\dev"
  // localPath 'build/riderRD-173-SNAPSHOT'

  val dir = file("build/rider")
  if (dir.exists()) {
    logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
    localPath.set(dir.absolutePath)
  } else {
    logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
    version.set("$baseVersion-SNAPSHOT")
  }

  instrumentCode.set(false)
  downloadSources.set(false)

  // Uncomment when need to install plugin into a different IDE build.
  // updateSinceUntilBuild = false

  // rider-plugins-appender: workaround for https://youtrack.jetbrains.com/issue/IDEA-179607
  // org.intellij.intelliLang needed for tests with language injection marks
  plugins.set(
    listOf(
      "com.intellij.css",
      "com.intellij.database",
      "com.intellij.ml.llm",
      "JavaScript",
      "org.intellij.intelliLang",
      "rider-plugins-appender"
    )
  )
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

artifacts {
  add(riderModel.name, provider {
    val sdkRoot = tasks.setupDependencies.get().idea.get().classes
    sdkRoot.resolve("lib/rd/rider-model.jar").also {
      check(it.isFile) {
        "rider-model.jar is not found at $riderModel"
      }
    }
  }) {
    builtBy(tasks.setupDependencies)
  }
}

tasks {
  val dotNetSdkPath by lazy {
    val sdkPath = setupDependencies.get().idea.get().classes.resolve("lib").resolve("DotNetSdkForRdPlugins")
    if (sdkPath.isDirectory.not()) error("$sdkPath does not exist or not a directory")

    println("SDK path: $sdkPath")
    return@lazy sdkPath
  }

  withType<InstrumentCodeTask> {
    val bundledMavenArtifacts = file("build/maven-artifacts")
    if (bundledMavenArtifacts.exists()) {
      logger.lifecycle("Use ant compiler artifacts from local folder: $bundledMavenArtifacts")
      compilerClassPathFromMaven.set(
        bundledMavenArtifacts.walkTopDown()
          .filter { it.extension == "jar" && !it.name.endsWith("-sources.jar") }
          .toList() + File("${ideaDependency.get().classes}/lib/util.jar")
      )
    } else {
      logger.lifecycle("Use ant compiler artifacts from maven")
    }
  }

  withType<PrepareSandboxTask> {
    var files = libFiles + pluginFiles.map { "$it.dll" } + pluginFiles.map { "$it.pdb" }
    files = files.map { "$resharperPluginPath/src/$it" }
    val fantomasHostFiles = fantomasHostFiles.map { "$resharperPluginPath/src/$it" }
    val typeProvidersFiles = typeProvidersFiles.map { "$resharperPluginPath/src/$it" }

    if (name == IntelliJPluginConstants.PREPARE_TESTING_SANDBOX_TASK_NAME) {
      val testHostPath = "$resharperPluginPath/src/FSharp/FSharp.Tests.Host/$outputRelativePath"
      val testHostName = "$testHostPath/JetBrains.ReSharper.Plugins.FSharp.Tests.Host"
      files = files + listOf("$testHostName.dll", "$testHostName.pdb")
    }

    fun moveToPlugin(files: List<String>, destinationFolder: String) {
      files.forEach {
        from(it) { into("${intellij.pluginName.get()}/$destinationFolder") }
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
          logger.warn("$name: ${file.name} -> $destinationDir/${intellij.pluginName.get()}/$destinationFolder")
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
    group = riderFSharpTargetsGroup
    doLast {
      dotNetSdkPathPropsPath.writeTextIfChanged(
        """<Project>
  <PropertyGroup>
    <DotNetSdkPath>$dotNetSdkPath</DotNetSdkPath>
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
    group = riderFSharpTargetsGroup
    doLast {
      nugetConfigPath.writeTextIfChanged(
        """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="resharper-sdk" value="$dotNetSdkPath" />
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

  create("buildReSharperPlugin") {
    group = riderFSharpTargetsGroup
    dependsOn(prepare)
    doLast {
      exec {
        executable = "msbuild"
        args = listOf("$resharperPluginPath/ReSharper.FSharp.sln")
      }
    }
  }

  wrapper {
    gradleVersion = "8.7"
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-bin.zip"
  }

  defaultTasks(prepare)
}
