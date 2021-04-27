import org.jetbrains.intellij.tasks.BuildSearchableOptionsTask
import org.jetbrains.intellij.tasks.RunIdeTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask

plugins {
    id("org.jetbrains.intellij")
}

var baseVersion = ext.properties["build.baseVersion"]

intellij {
    type = "RD"
    version = "$baseVersion-SNAPSHOT"
    instrumentCode = false
    downloadSources = false
}

java {
    sourceCompatibility = JavaVersion.VERSION_1_8
    targetCompatibility = JavaVersion.VERSION_1_8
}

val buildCounter = ext.properties["build.number"] ?: "9999"
version = "$baseVersion.$buildCounter"

val repoRoot = projectDir.parentFile.parentFile!!
val resharperPluginPath = File(repoRoot, "ReSharper.FSharp")
val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"

val pluginFiles = listOf(
        "FSharp.ExternalFormatter.Protocol/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.ExternalFormatter.Protocol.dll",
        "FSharp.ExternalFormatter.Protocol/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.ExternalFormatter.Protocol.pdb",
        "FSharp.ExternalFormatter.Host/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.ExternalFormatter.Host.exe",
        "FSharp.ExternalFormatter.Host/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.ExternalFormatter.Host.runtimeconfig.json",
        "FSharp.ExternalFormatter.Host/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.ExternalFormatter.Host.pdb",
        "FSharp.ExternalFormatter.Host/bin/$buildConfiguration/net461/Fantomas.dll")

tasks {
    withType<BuildSearchableOptionsTask> {
        enabled = false
    }

    withType<RunIdeTask> {
        enabled = false
    }

    withType<PrepareSandboxTask> {
        val files = pluginFiles.map { "$resharperPluginPath/src/$it" }

        files.forEach {
            from(it) { into("${intellij.pluginName}/dotnet") }
        }

        doLast {
            files.forEach {
                val file = file(it)
                if (!file.exists()) throw RuntimeException("File $file does not exist")
                logger.warn("$name: ${file.name} -> $destinationDir/${intellij.pluginName}/dotnet")
            }
        }
    }
}
