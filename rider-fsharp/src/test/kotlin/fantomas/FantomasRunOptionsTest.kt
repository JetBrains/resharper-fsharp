package fantomas

import com.intellij.util.application
import com.intellij.util.io.createFile
import com.intellij.util.io.delete
import com.intellij.util.io.write
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.flushFileChanges
import com.jetbrains.rider.plugins.fsharp.test.runProcessWaitForExit
import com.jetbrains.rider.plugins.fsharp.test.withSetting
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.protocol.protocolManager
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBe
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.base.PrepareTestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeTest
import org.testng.annotations.Test
import java.io.PrintStream
import java.nio.file.Paths
import java.time.Duration
import kotlin.io.path.Path
import kotlin.io.path.absolutePathString
import kotlin.io.path.createDirectory

@Test
@TestEnvironment(coreVersion = CoreVersion.DOT_NET_6, reuseSolution = false)
class FantomasRunOptionsTest : EditorTestBase() {
    override fun getSolutionDirectoryName() = "FormatCodeApp"
    override val restoreNuGetPackages = false

    private fun getDotnetCliHome() = Path(tempTestDirectory.parent, "dotnetHomeCli")
    private val fantomasNotifications = ArrayList<String>()
    private val bundledVersion = "4.7.8.0"
    private val globalVersion = "4.7.2.0"
    private var dotnetToolsInvalidated = false

    private fun dumpRunOptions() = project.fcsHost.dumpFantomasRunOptions.sync(Unit)
    private fun checkFantomasVersion(version: String) = project.fcsHost.fantomasVersion.sync(Unit).shouldBe(version)
    private fun dumpNotifications(stream: PrintStream, expectedCount: Int) {
        stream.println("\n\nNotifications:")
        waitAndPump(project.lifetime,
            { fantomasNotifications.size >= expectedCount },
            Duration.ofSeconds(30),
            { "Didn't wait for notifications. Expected $expectedCount, but was ${fantomasNotifications.size}" })

        fantomasNotifications.forEach { stream.println(it) }
    }

    private fun withFantomasSetting(value: String, function: () -> Unit) {
        withSetting(project, "FSharp/FSharpFantomasOptions/Location/@EntryValue", value, "AutoDetected") {
            function()
        }
    }

    private fun withDotnetToolsUpdate(function: () -> Unit) {
        dotnetToolsInvalidated = false
        function()
        flushFileChanges(project)
        waitAndPump(Duration.ofSeconds(15), { dotnetToolsInvalidated == true }, { "Dotnet tools wasn't changed." })
    }

    private fun withFantomasLocalTool(version: String, restore: Boolean = true, function: () -> Unit) {
        val manifestFile = Paths.get(project.solutionDirectory.absolutePath, ".config", "dotnet-tools.json")
        frameworkLogger.info("Create '$manifestFile'")
        val file = manifestFile.createFile()

        try {
            withDotnetToolsUpdate {
                val toolsJson = """"fantomas-tool": { "version": "$version", "commands": [ "fantomas" ] }"""
                file.write("""{ "version": 1, "isRoot": true, "tools": { $toolsJson } }""")
            }
            if (restore) {
                withDotnetToolsUpdate {
                    // Trigger dotnet tools restore
                    // TODO: use separate dotnet tools restore API
                    restoreNuGet(project)
                }
            }
            function()
        } finally {
            withDotnetToolsUpdate {
                file.delete()
                project.fcsHost.terminateFantomasHost.sync(Unit)
            }
        }
    }

    private fun withFantomasGlobalTool(function: () -> Unit) {
        try {
            val env = mapOf("DOTNET_CLI_HOME" to getDotnetCliHome().absolutePathString())

            withDotnetToolsUpdate {
                runProcessWaitForExit(
                    Path(PrepareTestEnvironment.dotnetCoreCliPath),
                    listOf("tool", "install", "fantomas-tool", "-g", "--version", globalVersion),
                    env
                )
            }
            function()
        } finally {
            project.fcsHost.terminateFantomasHost.sync(Unit)
        }
    }

    @BeforeTest(alwaysRun = true)
    fun prepareDotnetCliHome() {
        application.protocolManager.protocolHosts.forEach {
            setReSharperEnvVar("DOTNET_CLI_HOME", getDotnetCliHome().absolutePathString(), it)
        }
    }

    @BeforeMethod(alwaysRun = true)
    fun clearDotnetCliHomeFolder() {
        fantomasNotifications.clear()
        getDotnetCliHome().delete(true)
        getDotnetCliHome().createDirectory()
    }

    override fun beforeDoTestWithDocuments() {
        project.fcsHost.fantomasNotificationFired.advise(testLifetimeDef.lifetime) {
            fantomasNotifications.add(it)
        }
        project.fcsHost.dotnetToolInvalidated.advise(testLifetimeDef.lifetime) {
            dotnetToolsInvalidated = true
        }
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
            withFantomasLocalTool("wrong_version", false) {
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
                withFantomasLocalTool("wrong_version", false) {
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
        withOpenedEditor("Simple.fs") {
            withFantomasLocalTool(version) {
                executeWithGold(testGoldFile) {
                    reformatCode()
                    checkFantomasVersion(expectedVersion)
                    dumpOpenedDocument(it, project!!, false)
                }
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

                    dumpNotifications(it, 1)
                }
            }
        }
    }
}
