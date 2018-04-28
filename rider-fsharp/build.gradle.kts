import org.gradle.internal.jvm.Jvm
import org.jetbrains.intellij.tasks.PublishTask
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.intellij.IntelliJPlugin
import org.jetbrains.intellij.tasks.PrepareSandboxTask

plugins {
    id("org.jetbrains.kotlin.jvm") version "1.2.41"
    id("org.jetbrains.intellij") version "0.3.1"
}

repositories {
    mavenCentral()
}

java {
    sourceCompatibility = JavaVersion.VERSION_1_8
    targetCompatibility = JavaVersion.VERSION_1_8
}

intellij {
    type = "RD"

    // Download a version of Rider to compile and run with. Either set `version` to
    // 'LATEST-TRUNK-SNAPSHOT' or 'LATEST-EAP-SNAPSHOT' or a known version.
    // This will download from www.jetbrains.com/intellij-repository/snapshots or
    // www.jetbrains.com/intellij-repository/releases, respectively.
    // Note that there's no guarantee that these are kept up to date
    // version = 'LATEST-TRUNK-SNAPSHOT'
    // If the build isn't available in intellij-repository, use an installed version via `localPath`
    // localPath = '/Users/matt/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/171.4089.265/Rider EAP.app/Contents'
    // localPath = "C:\\Users\\Ivan.Shakhov\\AppData\\Local\\JetBrains\\Toolbox\\apps\\Rider\\ch-0\\171.4456.459"
    // localPath = "C:\\Users\\ivan.pashchenko\\AppData\\Local\\JetBrains\\Toolbox\\apps\\Rider\\ch-0\\dev"
    // localPath 'build/riderRD-173-SNAPSHOT'

    val dir = file("build/rider")
    if (dir.exists()) {
        logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
        localPath = dir.absolutePath
    } else {
        logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
        version = "LATEST-TRUNK-SNAPSHOT" // Trunk until we have release
    }
    intellijRepo = "https://www.jetbrains.com/intellij-repository"

    downloadSources = false
    updateSinceUntilBuild = false

    // Workaround for https://youtrack.jetbrains.com/issue/IDEA-179607
    setPlugins("rider-plugins-appender")
}

val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"

val backendDir = "../ReSharper.FSharp"
val libDllFiles = listOf(
        "FSharp.Common/bin/$buildConfiguration/net451/FSharp.Core.dll",
        "FSharp.Common/bin/$buildConfiguration/net451/FSharp.Compiler.Service.dll", // todo: add pdb after next repack
        "FSharp.Psi.Features/bin/$buildConfiguration/net451/FantomasLib.dll")

val pluginDllFiles = listOf(
        "FSharp.ProjectModelBase/bin/$buildConfiguration/net451/JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase",
        "FSharp.Common/bin/$buildConfiguration/net451/JetBrains.ReSharper.Plugins.FSharp.Common",
        "FSharp.Psi/bin/$buildConfiguration/net451/JetBrains.ReSharper.Plugins.FSharp.Psi",
        "FSharp.Psi.Features/bin/$buildConfiguration/net451/JetBrains.ReSharper.Plugins.FSharp.Psi.Features",
        "Daemon.FSharp/bin/$buildConfiguration/net451/JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs",
        "Services.FSharp/bin/$buildConfiguration/net451/JetBrains.ReSharper.Plugins.FSharp.Services.Cs")

tasks {
    withType<PrepareSandboxTask> {
        var files = libDllFiles + pluginDllFiles.map { "$it.dll" } + pluginDllFiles.map { "$it.pdb" }

        if (name == IntelliJPlugin.PREPARE_TESTING_SANDBOX_TASK_NAME) {
            val hostDllName = "JetBrains.ReSharper.Plugins.FSharp.Tests.Host"
            val hostDllPath = "$backendDir/test/src/FSharp.Tests.Host/bin/$buildConfiguration/net451/$hostDllName"
            files += listOf("$hostDllPath.dll", "$hostDllPath.pdb")
        }

        files.forEach {
            val file = file("$backendDir/src/$it")
            if (!file.exists()) throw RuntimeException("File $file does not exist")

            logger.warn("$name: $file -> $destinationDir/${intellij.pluginName}/dotnet")
            from(file, { into("${intellij.pluginName}/dotnet") })
        }
        into("${intellij.pluginName}/projectTemplates") {
            from("projectTemplates")
        }
    }

    withType<Test> {
        useTestNG()
        testLogging {
            showStandardStreams = true
            exceptionFormat = TestExceptionFormat.FULL
        }
        val rerunSuccessfulTests = false
        outputs.upToDateWhen { !rerunSuccessfulTests }

    }
}
