import com.jetbrains.rd.generator.gradle.RdGenExtension
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.grammarkit.tasks.GenerateLexerTask
import org.jetbrains.intellij.IntelliJPluginConstants
import org.jetbrains.intellij.tasks.IntelliJInstrumentCodeTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.intellij.tasks.RunIdeTask
import org.jetbrains.kotlin.daemon.common.toHexString
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
    id ("com.jetbrains.rdgen") version "2022.3.1"
    id("org.jetbrains.intellij") version "1.9.0" // https://github.com/JetBrains/gradle-intellij-plugin/releases
    id("org.jetbrains.grammarkit") version "2021.2.2"
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
    kotlin("jvm") version "1.7.0"
}

apply {
    plugin("kotlin")
}

repositories {
    mavenCentral()
    maven("https://cache-redirector.jetbrains.com/repo1.maven.org/maven2")
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
}

val baseVersion = "2022.3"
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

    // Workaround for https://youtrack.jetbrains.com/issue/IDEA-179607
    plugins.set(listOf("rider-plugins-appender"))
}

val repoRoot = projectDir.parentFile!!
val resharperPluginPath = File(repoRoot, "ReSharper.FSharp")

val rdLibDirectory: () -> File = { file("${tasks.setupDependencies.get().idea.get().classes}/lib/rd") }
extra["rdLibDirectory"] = rdLibDirectory

val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"
val primaryTargetFramework = "net472"
val outputRelativePath = "bin/$buildConfiguration/$primaryTargetFramework"

val libFiles = listOf(
        "FSharp.Common/$outputRelativePath/FSharp.Core.dll",
        "FSharp.Common/$outputRelativePath/FSharp.Core.xml",
        "FSharp.Common/$outputRelativePath/FSharp.Compiler.Service.dll", // todo: add pdb after next repack
        "FSharp.Common/$outputRelativePath/FSharp.DependencyManager.Nuget.dll",
        "FSharp.Common/$outputRelativePath/FSharp.Compiler.Interactive.Settings.dll")

val pluginFiles = listOf(
        "FSharp.ProjectModelBase/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase",
        "FSharp.Common/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Common",
        "FSharp.Psi/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi",
        "FSharp.Psi.Services/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Services",
        "FSharp.Psi.Daemon/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon",
        "FSharp.Psi.Intentions/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions",
        "FSharp.Psi.Features/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Features",
        "FSharp.Fantomas.Protocol/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol",
        "FSharp.TypeProviders.Protocol/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol")

val typeProvidersFiles = listOf(
        "FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe",
        "FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.pdb",
        "FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe.config",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.dll",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.pdb",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.netcoreapp31.win.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.netcoreapp31.unix.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.net5.win.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.net5.unix.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.net6.win.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.net6.unix.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.net7.win.runtimeconfig.json",
        "FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.net7.unix.runtimeconfig.json")

val fantomasFiles = listOf(
        "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.FCS.dll",
        "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Core.dll",
        "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.dll",
        "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.pdb",
        "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Host.win.runtimeconfig.json",
        "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Host.unix.runtimeconfig.json")

val nugetConfigPath = File(repoRoot, "NuGet.Config")
val dotNetSdkPathPropsPath = File("build", "DotNetSdkPath.generated.props")
val backendLexerSources = "$repoRoot/rider-fsharp/build/backend-lexer-sources/"

val riderFSharpTargetsGroup = "rider-fsharp"

fun File.writeTextIfChanged(content: String) {
    val bytes = content.toByteArray()

    if (!exists() || readBytes().toHexString() != bytes.toHexString()) {
        println("Writing $path")
        writeBytes(bytes)
    }
}

tasks {
    val dotNetSdkPath by lazy {
        val sdkPath = setupDependencies.get().idea.get().classes.resolve("lib").resolve("DotNetSdkForRdPlugins")
        if (sdkPath.isDirectory.not()) error("$sdkPath does not exist or not a directory")

        println("SDK path: $sdkPath")
        return@lazy sdkPath
    }

    configure<RdGenExtension> {
        val csOutput = File(repoRoot, "ReSharper.FSharp/src/FSharp.ProjectModelBase/src/Protocol")
        val ktOutput = File(repoRoot, "rider-fsharp/src/main/java/com/jetbrains/rider/plugins/fsharp/protocol")

        val typeProviderClientOutput = File(repoRoot, "ReSharper.FSharp/src/FSharp.TypeProviders.Protocol/src/Client")
        val typeProviderServerOutput = File(repoRoot, "ReSharper.FSharp/src/FSharp.TypeProviders.Protocol/src/Server")

        val fantomasServerOutput = File(repoRoot, "ReSharper.FSharp/src/FSharp.Fantomas.Protocol/src/Server")
        val fantomasClientOutput = File(repoRoot, "ReSharper.FSharp/src/FSharp.Fantomas.Protocol/src/Client")

        verbose = true
        hashFolder = "build/rdgen"
        logger.info("Configuring rdgen params")
        classpath({
            logger.info("Calculating classpath for rdgen, intellij.ideaDependency is ${rdLibDirectory().canonicalPath}")
            rdLibDirectory().resolve("rider-model.jar").canonicalPath
        })
        sources(File(repoRoot, "rider-fsharp/protocol/src/kotlin/model"))
        packages = "model"

        generator {
            language = "kotlin"
            transform = "asis"
            root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
            namespace = "com.jetbrains.rider.model"
            directory = "$ktOutput"
        }

        generator {
            language = "csharp"
            transform = "reversed"
            root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
            namespace = "JetBrains.Rider.Model"
            directory = "$csOutput"
        }

        generator {
            language = "csharp"
            transform = "asis"
            root = "model.RdFSharpTypeProvidersModel"
            namespace = "JetBrains.Rider.FSharp.TypeProviders.Protocol.Client"
            directory = "$typeProviderClientOutput"
        }
        generator {
            language = "csharp"
            transform = "reversed"
            root = "model.RdFSharpTypeProvidersModel"
            namespace = "JetBrains.Rider.FSharp.TypeProviders.Protocol.Server"
            directory = "$typeProviderServerOutput"
        }

        generator {
            language = "csharp"
            transform = "asis"
            root = "model.RdFantomasModel"
            namespace = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client"
            directory = "$fantomasClientOutput"
        }
        generator {
            language = "csharp"
            transform = "reversed"
            root = "model.RdFantomasModel"
            namespace = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server"
            directory = "$fantomasServerOutput"
        }
    }

    withType<IntelliJInstrumentCodeTask> {
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
        val fantomasFiles = fantomasFiles.map { "$resharperPluginPath/src/$it" }
        val typeProvidersFiles = typeProvidersFiles.map { "$resharperPluginPath/src/$it" }

        if (name == IntelliJPluginConstants.PREPARE_TESTING_SANDBOX_TASK_NAME) {
            val testHostPath = "$resharperPluginPath/test/src/FSharp.Tests.Host/$outputRelativePath"
            val testHostName = "$testHostPath/JetBrains.ReSharper.Plugins.FSharp.Tests.Host"
            files = files + listOf("$testHostName.dll", "$testHostName.pdb")
        }

        fun moveToPlugin(files: List<String>, destinationFolder: String) {
            files.forEach {
                from(it) { into("${intellij.pluginName.get()}/$destinationFolder") }
            }
        }

        moveToPlugin(files, "dotnet")
        moveToPlugin(fantomasFiles, "fantomas")
        moveToPlugin(typeProvidersFiles, "typeProviders")
        moveToPlugin(listOf("projectTemplates"), "projectTemplates")

        doLast {
            fun validateFiles(files: List<String>, destinationFolder: String) {
                files.forEach {
                    val file = file(it)
                    if (!file.exists()) throw RuntimeException("File $file does not exist")
                    logger.warn("$name: ${file.name} -> $destinationDir/${intellij.pluginName.get()}/$destinationFolder")
                }
            }
            validateFiles(files, "dotnet")
            validateFiles(fantomasFiles, "fantomas")
            validateFiles(typeProvidersFiles, "typeProviders")
        }
    }

    // Initially introduced in:
    // https://github.com/JetBrains/ForTea/blob/master/Frontend/build.gradle.kts
    withType<RunIdeTask> {
        // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
        maxHeapSize = "1500m"
    }

    val resetLexerDirectory = create("resetLexerDirectory") {
        doFirst {
            File(backendLexerSources).deleteRecursively()
            File(backendLexerSources).mkdirs()
        }
    }

    // Cannot use ordinary copy here, because it requires eager evaluation of locations
    val copyUnicodeLex = create("copyUnicodeLex") {
        dependsOn(resetLexerDirectory)
        doFirst {
            val libPath = File("$dotNetSdkPath").parent
            File(libPath, "ReSharperHost/PsiTasks").listFiles { it -> it.extension == "lex" }!!.forEach {
                println(it)
                it.copyTo(File(backendLexerSources, it.name))
            }
        }
    }

    val copyBackendLexerSources = create<Copy>("copyBackendLexerSources") {
        dependsOn(resetLexerDirectory)
        from("$resharperPluginPath/src/FSharp.Psi/src/Parsing/Lexing") {
            include("*.lex")
        }
        into(backendLexerSources)
    }

    val generateFSharpLexer = task<GenerateLexerTask>("generateFSharpLexer") {
        dependsOn(copyBackendLexerSources, copyUnicodeLex)
        source.set("src/main/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer/_FSharpLexer.flex")
        targetDir.set("src/main/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer")
        targetClass.set("_FSharpLexer")
        purgeOldFiles.set(true)
    }

    withType<KotlinCompile> {
        kotlinOptions.jvmTarget = "17"
        dependsOn(generateFSharpLexer, rdgen)
    }

    withType<Test> {
        useTestNG()

        // Should be the same as community/plugins/devkit/devkit-core/src/run/OpenedPackages.txt
        jvmArgs("--add-opens=java.base/java.lang.reflect=ALL-UNNAMED",
            "--add-opens=java.base/java.net=ALL-UNNAMED",
            "--add-opens=java.base/java.nio=ALL-UNNAMED",
            "--add-opens=java.base/java.nio.charset=ALL-UNNAMED",
            "--add-opens=java.base/java.text=ALL-UNNAMED",
            "--add-opens=java.base/java.time=ALL-UNNAMED",
            "--add-opens=java.base/java.util=ALL-UNNAMED",
            "--add-opens=java.base/java.util.concurrent=ALL-UNNAMED",
            "--add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED",
            "--add-opens=java.base/jdk.internal.vm=ALL-UNNAMED",
            "--add-opens=java.base/sun.nio.ch=ALL-UNNAMED",
            "--add-opens=java.base/sun.nio.fs=ALL-UNNAMED",
            "--add-opens=java.base/sun.security.ssl=ALL-UNNAMED",
            "--add-opens=java.base/sun.security.util=ALL-UNNAMED",
            "--add-opens=java.desktop/com.apple.eawt=ALL-UNNAMED",
            "--add-opens=java.desktop/com.apple.eawt.event=ALL-UNNAMED",
            "--add-opens=java.desktop/com.apple.laf=ALL-UNNAMED",
            "--add-opens=java.desktop/com.sun.java.swing.plaf.gtk=ALL-UNNAMED",
            "--add-opens=java.desktop/java.awt=ALL-UNNAMED",
            "--add-opens=java.desktop/java.awt.dnd.peer=ALL-UNNAMED",
            "--add-opens=java.desktop/java.awt.event=ALL-UNNAMED",
            "--add-opens=java.desktop/java.awt.image=ALL-UNNAMED",
            "--add-opens=java.desktop/java.awt.peer=ALL-UNNAMED",
            "--add-opens=java.desktop/java.awt.font=ALL-UNNAMED",
            "--add-opens=java.desktop/javax.swing=ALL-UNNAMED",
            "--add-opens=java.desktop/javax.swing.plaf.basic=ALL-UNNAMED",
            "--add-opens=java.desktop/javax.swing.text.html=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.awt.X11=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.awt.datatransfer=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.awt.image=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.awt.windows=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.awt=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.font=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.java2d=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.lwawt=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.lwawt.macosx=ALL-UNNAMED",
            "--add-opens=java.desktop/sun.swing=ALL-UNNAMED",
            "--add-opens=jdk.attach/sun.tools.attach=ALL-UNNAMED",
            "--add-opens=jdk.compiler/com.sun.tools.javac.api=ALL-UNNAMED",
            "--add-opens=jdk.internal.jvmstat/sun.jvmstat.monitor=ALL-UNNAMED",
            "--add-opens=jdk.jdi/com.sun.tools.jdi=ALL-UNNAMED",
            "-Didea.jna.unpacked=true",
            "-Djna.nounpack=true",
            "-Djna.boot.library.path=${setupDependencies.orNull?.idea?.get()?.classes}/lib/jna/${System.getProperty("os.arch")}")

        testLogging {
            showStandardStreams = true
            exceptionFormat = TestExceptionFormat.FULL
        }
        val rerunSuccessfulTests = false
        outputs.upToDateWhen { !rerunSuccessfulTests }

        val runningOnGithubActions = when (ext.properties["running.on.github.actions"]) {
            "true" -> true
            else -> false
        }
        logger.lifecycle("Running on GitHub Actions: $runningOnGithubActions")
        ignoreFailures = !runningOnGithubActions
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
        dependsOn(rdgen, writeNuGetConfig, writeDotNetSdkPathProps)
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
    defaultTasks(prepare)
}
