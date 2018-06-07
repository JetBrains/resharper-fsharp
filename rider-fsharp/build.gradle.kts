import org.apache.tools.ant.taskdefs.condition.Os
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


val baseVersion = "2018.2"

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
        version = "$baseVersion-SNAPSHOT"
    }
    intellijRepo = "https://www.jetbrains.com/intellij-repository"

    downloadSources = false
    updateSinceUntilBuild = false

    // Workaround for https://youtrack.jetbrains.com/issue/IDEA-179607
    setPlugins("rider-plugins-appender")
}

val repoRoot = projectDir.parentFile!!
val resharperPluginPath = "$repoRoot/ReSharper.FSharp"
val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"

val libFiles = listOf(
        "FSharp.Common/bin/$buildConfiguration/net461/FSharp.Core.dll",
        "FSharp.Common/bin/$buildConfiguration/net461/FSharp.Compiler.Service.dll", // todo: add pdb after next repack
        "FSharp.Psi.Features/bin/$buildConfiguration/net461/Fantomas.dll")

val pluginFiles = listOf(
        "FSharp.ProjectModelBase/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase",
        "FSharp.Common/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.Common",
        "FSharp.Psi/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.Psi",
        "FSharp.Psi.Features/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.Psi.Features",
        "Daemon.FSharp/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs",
        "Services.FSharp/bin/$buildConfiguration/net461/JetBrains.ReSharper.Plugins.FSharp.Services.Cs")

tasks {
    withType<PrepareSandboxTask> {
        var files = libFiles + pluginFiles.map { "$it.dll" } + pluginFiles.map { "$it.pdb" }
        files = files.map { "$resharperPluginPath/src/$it" }

        if (name == IntelliJPlugin.PREPARE_TESTING_SANDBOX_TASK_NAME) {
            val testHostPath = "$resharperPluginPath/test/src/FSharp.Tests.Host/bin/$buildConfiguration/net461"
            val testHostName = "$testHostPath/JetBrains.ReSharper.Plugins.FSharp.Tests.Host"
            files += listOf("$testHostName.dll", "$testHostName.pdb")
        }

        files.forEach {
            from(it, { into("${intellij.pluginName}/dotnet") })
        }

        into("${intellij.pluginName}/projectTemplates") {
            from("projectTemplates")
        }

        doLast {
            files.forEach {
                val file = file(it)
                if (!file.exists()) throw RuntimeException("File $file does not exist")
                logger.warn("$name: ${file.name} -> $destinationDir/${intellij.pluginName}/dotnet")
            }
        }
    }

    withType<KotlinCompile> {
        kotlinOptions.jvmTarget = "1.8"
        dependsOn("generateModel")
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

    "prepare" {
        group = "intellij"
        dependsOn("generateModel")
        doLast {
            val sdkPath = intellij.ideaDependency.classes
            println("SDK path: $sdkPath")
            file("$repoRoot/NuGet.Config").writeText("""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="resharper-sdk" value="${sdkPath.relativeTo(repoRoot)}/lib/ReSharperHostSdk" />
  </packageSources>
</configuration>
""")

            exec {
                executable = "dotnet"
                args = listOf("restore", "$resharperPluginPath/ReSharper.FSharp.sln")
            }
        }
    }

    "generateModel" {
        group = "intellij"
        doLast {
            val sdkPath = intellij.ideaDependency.classes
            val rdLibDirectory = File(sdkPath, "lib/rd").canonicalFile
            ext.properties["rdLibDirectory"] = rdLibDirectory
            val rdgenJar = File(rdLibDirectory.absolutePath, "rd-gen.jar")

            assert(rdLibDirectory.isDirectory)
            assert(rdgenJar.isFile)

            // If we specify these as outputs of the task, gradle will create them for us, but we then
            // need to also specify inputs. RdGen does up to date checks for us, but we could skip that
            // altogether if we hard code them in the task. Downside is that we'd need to keep the list
            // of inputs/outputs up to date manually, instead of leaving it to rdgen
            mkdir("build/rdgen")

            val modelDir = File(repoRoot, "rider-fsharp/protocol/src/kotlin/model")
            val packageName = "model"

            val cpSeparator = if (Os.isFamily(Os.FAMILY_WINDOWS)) ";" else ":"
            val compilerClasspath = "$rdLibDirectory/rd-gen.jar$cpSeparator$rdLibDirectory/rider-model.jar"
            val rdGenClasspath = files(compilerClasspath)

            // Protocol between backend and frontend
            // Direction isn't important for this model, so we don't specify it in the model. Which means
            // we have to specify output directories here
            // Inputs: rider/protocol/src/main/kotlin/model/rider/RdUnityModel.kt
            // Outputs: resharper/src/resharper-unity/Rider/RdUnityProtocol/RdUnityModel.Generated.cs
            //          rider/src/main/kotlin/com/jetbrains/rider/protocol/RdUnityProtocol/RdUnityModel.Generated.kt
            val csOutput = File(repoRoot, "Resharper.FSharp/src/FSharp.ProjectModelBase/src/Protocol")
            val ktOutput = File(repoRoot, "rider-fsharp/src/main/java/com/jetbrains/rider/plugins/fsharp/protocol")
            javaexec {
                main = "com.jetbrains.rider.generator.nova.MainKt"
                classpath = rdGenClasspath
                systemProperty("model.out.src.cs.dir", "$csOutput")
                systemProperty("model.out.src.kt.dir", "$ktOutput")
                errorOutput = System.out // kotlin throws warnings in stderr which causes appveyor build to fail
                args = listOf(
                        "--verbose",
                        "--hash-folder=build/rdgen",
                        "--compiler-classpath=$compilerClasspath",
                        "--source=$modelDir",
                        "--packages=$packageName"
                )
            }
        }
    }
}

tasks {
    task<Wrapper>("wrapper") {
        gradleVersion = "4.7"
        distributionType = Wrapper.DistributionType.ALL
        distributionUrl = "https://cache-redirector.shared.aws.intellij.net/services.gradle.org/distributions/gradle-$gradleVersion-all.zip"
    }
}
