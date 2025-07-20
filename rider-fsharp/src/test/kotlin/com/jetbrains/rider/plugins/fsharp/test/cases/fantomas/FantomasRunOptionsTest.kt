package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7)
class FantomasRunOptionsTest : FantomasDotnetToolTestBase() {
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
  fun `local tool 5_0`() = doLocalToolTest("fantomas", "5.2.1", "5.2.1.0")

  @Test
  fun `local tool 6_0 with cursor`() {
    withFantomasLocalTool("fantomas", "6.0.0") {
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
