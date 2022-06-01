package fantomas

import com.intellij.util.application
import com.intellij.util.io.createFile
import com.intellij.util.io.delete
import com.intellij.util.io.write
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.fsharp.test.*
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.protocol.protocolManager
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBe
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.base.PrepareTestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeTest
import org.testng.annotations.Test
import java.io.PrintStream
import java.nio.file.Paths
import java.time.Duration
import kotlin.io.path.Path
import kotlin.io.path.absolutePathString

@Test
@TestEnvironment(coreVersion = CoreVersion.DOT_NET_6)
class FantomasRunOptionsTest : EditorTestBase() {
    override fun getSolutionDirectoryName() = "FormatCodeApp"

    private val fantomasNotifications = ArrayList<String>()
    private val bundledVersion = "4.7.8.0"
    private val globalVersion = "4.7.2.0"
    private var dotnetToolsInvalidated = false

    private fun dumpRunOptions() = project.fcsHost.dumpFantomasRunOptions.sync(Unit)
    private fun checkFantomasVersion(version: String) = project.fcsHost.fantomasVersion.sync(Unit).shouldBe(version)
    private fun dumpNotifications(stream: PrintStream, expectedCount: Int) {
        stream.println("\n\nNotifications:")
        waitAndPump(project.lifetime,
            { fantomasNotifications.size == expectedCount },
            Duration.ofSeconds(30),
            { "Didn't wait for notifications. Expected $expectedCount, but was ${fantomasNotifications.size}" })

        fantomasNotifications.forEach { stream.println(it) }
    }

    private fun withFantomasSetting(value: String, function: () -> Unit) =
        withSetting(project, "FSharp/FSharpFantomasOptions/Version/@EntryValue", value, "AutoDetected") {
            function()
        }

    private fun withDotnetToolsUpdate(function: () -> Unit) {
        dotnetToolsInvalidated = false
        function()
        waitAndPump(
            project.lifetime,
            { dotnetToolsInvalidated },
            Duration.ofSeconds(15),
            { "Dotnet tools wasn't changed." })
    }

    private fun withFantomasLocalTool(version: String, function: () -> Unit) {
        val fileName = Paths.get(project.solutionDirectory.absolutePath, ".config", "dotnet-tools.json")
        val toolResolverCache =
            Paths.get(project.solutionDirectory.absolutePath, ".dotnet", "toolResolverCache", "1", "fantomas-tool")
        frameworkLogger.info("Create '$fileName'")
        val file = fileName.createFile()
        val toolsJson = """"fantomas-tool": { "version": "$version", "commands": [ "fantomas" ] }"""
        try {
            dotnetToolsInvalidated = false
            file.write("""{ "version": 1, "isRoot": true, "tools": { $toolsJson } }""")
            flushFileChanges(project, fileName.absolutePathString())
            waitAndPump(project.lifetime,
                { dotnetToolsInvalidated },
                Duration.ofSeconds(15),
                { "Dotnet tools wasn't changed." })
            dotnetToolsInvalidated = false
            // Trigger dotnet tools restore
            restoreNuGet(project)
            flushFileChanges(project, toolResolverCache.absolutePathString())
            waitAndPump(project.lifetime,
                { dotnetToolsInvalidated },
                Duration.ofSeconds(15),
                { "Dotnet tools wasn't changed." })
            function()
        } finally {
            file.delete()
            flushFileChanges(project, fileName.absolutePathString())
        }
    }

    private fun withFantomasGlobalTool(function: () -> Unit) {
        val env = mapOf("DOTNET_CLI_HOME" to tempTestDirectory.absolutePath)
        val fantomasExePath =
            Path(tempTestDirectory.absolutePath, ".dotnet", "tools", "fantomas.exe").absolutePathString()
        dotnetToolsInvalidated = false
        runProcessWaitForExit(
            Path(PrepareTestEnvironment.dotnetCoreCliPath),
            listOf("tool", "install", "fantomas-tool", "-g", "--version", globalVersion),
            env
        )
        flushFileChanges(project, fantomasExePath)
        waitAndPump(project.lifetime,
            { dotnetToolsInvalidated },
            Duration.ofSeconds(15),
            { "Dotnet tools wasn't changed." })
        function()
    }

    @BeforeTest(alwaysRun = true)
    fun prepareDotnetCliHome() {
        application.protocolManager.protocolHosts.forEach {
            setReSharperEnvVar("DOTNET_CLI_HOME", tempTestDirectory.absolutePath, it)
        }
    }

    override fun beforeDoTestWithDocuments() {
        project.fcsHost.fantomasNotificationFired.advise(project.lifetime) {
            fantomasNotifications.add(it)
        }
        project.fcsHost.dotnetToolInvalidated.advise(project.lifetime) {
            dotnetToolsInvalidated = true
        }
        super.beforeDoTestWithDocuments()
    }

    @Test
    fun default() {
        executeWithGold(testGoldFile) {
            withOpenedEditor("Program.fs") {
                waitForDaemon()
                it.print(dumpRunOptions())

                reformatCode()
                checkFantomasVersion(bundledVersion)
                dumpNotifications(it, 0)
            }
        }
    }

    @Test
    fun `local tool`() {
        executeWithGold(testGoldFile) {
            withOpenedEditor("Program.fs") {
                withFantomasLocalTool("4.7.6") {
                    it.println("--With local dotnet tool--")
                    it.print(dumpRunOptions())

                    reformatCode()
                    checkFantomasVersion("4.7.6.0")
                    dumpNotifications(it, 0)
                    dotnetToolsInvalidated = false
                }

                it.println("\n--Without local dotnet tool--")
                it.print(dumpRunOptions())

                reformatCode()
                checkFantomasVersion(bundledVersion)
                dumpNotifications(it, 0)
            }
        }
    }

    @Test
    fun `local tool 3_3`() = doLocalToolTest("3.3.0", "3.3.0.0")

    @Test
    fun `local tool 4_5`() = doLocalToolTest("4.5.0", "4.5.0.0")

    @Test
    fun `local tool 4_6`() = doLocalToolTest("4.6.0", "4.6.0.0")

    @Test
    fun `local tool 5_0_0-alpha`() = doLocalToolTest("5.0.0-alpha-001", "5.0.0.0")

    @Test
    fun `global tool`() {
        executeWithGold(testGoldFile) {
            withOpenedEditor("Program.fs") {
                withFantomasGlobalTool {
                    it.print(dumpRunOptions())

                    reformatCode()
                    checkFantomasVersion(globalVersion)
                    dumpNotifications(it, 0)
                }
            }
        }
    }

    @Test
    fun `local tool selected but not found`() = doSelectedButNotFoundTest("LocalDotnetTool")

    @Test
    fun `global tool selected but not found`() = doSelectedButNotFoundTest("GlobalDotnetTool")

    @Test
    fun `local tool has unsupported version`() {
        executeWithGold(testGoldFile) {
            withFantomasLocalTool("wrong_version") {
                withOpenedEditor("Program.fs") {
                    it.print(dumpRunOptions())

                    reformatCode()
                    checkFantomasVersion(bundledVersion)

                    dumpNotifications(it, 1)
                }
            }
        }
    }

    @Test
    fun `run global tool if local tool failed to run`() {
        executeWithGold(testGoldFile) {
            withOpenedEditor("Program.fs") {
                withFantomasLocalTool("wrong_version") {
                    withFantomasGlobalTool {
                        it.print(dumpRunOptions())

                        reformatCode()
                        checkFantomasVersion(globalVersion)

                        dumpNotifications(it, 1)
                    }
                }
            }
        }
    }

    private fun doLocalToolTest(version: String, expectedVersion: String) {
        withOpenedEditor("Program.fs") {
            withFantomasLocalTool(version) {
                reformatCode()
                checkFantomasVersion(expectedVersion)
            }
        }
    }

    private fun doSelectedButNotFoundTest(version: String) {
        executeWithGold(testGoldFile) {
            withFantomasSetting(version) {
                withOpenedEditor("Program.fs") {
                    waitForDaemon()
                    it.print(dumpRunOptions())

                    reformatCode()
                    checkFantomasVersion(bundledVersion)

                    it.println("\n")
                    dumpNotifications(it, 1)
                }
            }
        }
    }
}
