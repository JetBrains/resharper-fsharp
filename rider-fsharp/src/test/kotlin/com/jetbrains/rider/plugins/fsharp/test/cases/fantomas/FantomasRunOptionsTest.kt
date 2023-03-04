package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.intellij.util.application
import com.intellij.util.io.createFile
import com.intellij.util.io.delete
import com.intellij.util.io.write
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.fsharp.test.*
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.protocol.protocolManager
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBe
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.env.Environment
import com.jetbrains.rider.test.env.dotNetSdk
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.BeforeTest
import org.testng.annotations.Test
import java.io.PrintStream
import java.nio.file.Paths
import java.time.Duration
import kotlin.io.path.*

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class FantomasRunOptionsTest : EditorTestBase() {
  override fun getSolutionDirectoryName() = "FormatCodeApp"
  override val restoreNuGetPackages = false

  private fun getDotnetCliHome() = Path(tempTestDirectory.parent, "dotnetHomeCli")
  private val fantomasNotifications = ArrayList<String>()
  private val bundledVersion = "5.2.1.0"
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

  private fun withFantomasLocalTool(name: String, version: String, restore: Boolean = true, function: () -> Unit) {
    val manifestFile = Paths.get(project.solutionDirectory.absolutePath, ".config", "dotnet-tools.json")
    frameworkLogger.info("Create '$manifestFile'")
    val file = manifestFile.createFile()

    try {
      withDotnetToolsUpdate {
        val toolsJson = """"$name": { "version": "$version", "commands": [ "fantomas" ] }"""
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
          Environment.dotNetSdk(testMethod.environment.sdkVersion).root.toPath(),
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
      editFSharpBackendSettings(it) {
        dotnetCliHomeEnvVar = getDotnetCliHome().absolutePathString()
      }
    }
  }

  override fun beforeDoTestWithDocuments() {
    super.beforeDoTestWithDocuments()

    fantomasNotifications.clear()

    project.fcsHost.fantomasNotificationFired.advise(testLifetimeDef.lifetime) {
      fantomasNotifications.add(it)
    }
    project.fcsHost.dotnetToolInvalidated.advise(testLifetimeDef.lifetime) {
      dotnetToolsInvalidated = true
    }

    val dotnetCliHome = getDotnetCliHome()
    if (dotnetCliHome.listDirectoryEntries().any { it.name != ".nuget" }) {
      withDotnetToolsUpdate {
        dotnetCliHome.delete(true)
        dotnetCliHome.createDirectory()
        flushFileChanges(project)
      }
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
      withOpenedEditor("Types.fs") {
        withFantomasLocalTool("fantomas-tool", "4.7.6") {
          it.println("--With local dotnet tool--")
          it.print(dumpRunOptions())

          reformatCode()
          checkFantomasVersion("4.7.6.0")

          it.println("\nFormatted:")
          dumpOpenedDocument(it, project!!)
          dumpNotifications(it, 0)
        }

        it.println("\n--Without local dotnet tool--")
        it.print(dumpRunOptions())

        reformatCode()
        checkFantomasVersion(bundledVersion)

        it.println("\nFormatted:")
        dumpOpenedDocument(it, project!!)
        dumpNotifications(it, 0)
      }
    }
  }

  @Test
  fun `local tool 3_3`() = doLocalToolTest("fantomas-tool", "3.3.0", "3.3.0.0")

  @Test
  fun `local tool 4_5`() = doLocalToolTest("fantomas-tool", "4.5.0", "4.5.0.0")

  @Test
  fun `local tool 4_6`() = doLocalToolTest("fantomas-tool", "4.6.0", "4.6.0.0")

  @Test
  fun `local tool 5_0`() = doLocalToolTest(
    "com/jetbrains/rider/plugins/fsharp/test/cases/fantomasrains/rider/plugins/fsharp/test/cases/fantomas",
    "5.0.0",
    "5.2.1.0"
  )

  @Test
  fun `local tool 6_0 with cursor`() {
    withFantomasLocalTool("fantomas", "6.0.0-alpha-004") {
      withOpenedEditor("Simple.fs", "LargeFile.fs") {
        executeWithGold(testGoldFile) {
          reformatCode()
          checkFantomasVersion("6.0.0.0")
          dumpOpenedDocument(it, project!!, true)
        }
      }
    }
  }

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
      withFantomasLocalTool("fantomas-tool", "wrong_version", false) {
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
        withFantomasLocalTool("fantomas-tool", "wrong_version", false) {
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

  private fun doLocalToolTest(name: String, version: String, expectedVersion: String) {
    withOpenedEditor("Simple.fs") {
      withFantomasLocalTool(name, version) {
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
