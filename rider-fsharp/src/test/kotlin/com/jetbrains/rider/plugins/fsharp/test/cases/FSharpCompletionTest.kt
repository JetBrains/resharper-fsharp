package com.jetbrains.rider.plugins.fsharp.test.cases

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CompletionTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.callBasicCompletion
import com.jetbrains.rider.test.scriptingApi.completeWithTab
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.waitForCompletion
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpCompletionTest : CompletionTestBase() {
  override fun getSolutionDirectoryName() = "CoreConsoleApp"
  override val restoreNuGetPackages = true

  @Test
  fun namespaceKeyword() = doTestTyping("names")

  @Test
  fun listModule() = doTestChooseItem("List")

  @Test
  fun listModuleValue() = doTestTyping("filt")

  @Test(enabled = false)
  fun localVal01() = doTestChooseItem("x")

  @Test
  fun localVal02() = doTestTyping("x")

  @Test
  fun qualified01() = doTestChooseItem("a")

  @Test
  fun qualified02() = doTestChooseItem("a")

  private fun doTestTyping(typed: String, fileName: String = "Program.fs") {
    dumpOpenedEditor(fileName, fileName) {
      waitForDaemon()
      typeWithLatency(typed)
      callBasicCompletion()
      waitForCompletion()
      completeWithTab()
    }
  }

  private fun doTestChooseItem(item: String, fileName: String = "Program.fs") {
    dumpOpenedEditor(fileName, fileName) {
      waitForDaemon()
      callBasicCompletion()
      waitForCompletion()
      completeWithTab(item)
    }
  }

  @Test
  fun `nuget reference - simple`() = doTestTyping("nu", "Script.fsx")
  fun `nuget reference - reference`() = doTestTyping("nu", "Script.fsx")
  fun `nuget reference - triple quoted string`() = doTestTyping("nu", "Script.fsx")
  fun `nuget reference - verbatim string`() = doTestTyping("nu", "Script.fsx")
  fun `nuget reference - package name`() = doTestTyping("JetBrains.Annotatio", "Script.fsx")
  fun `nuget reference - version`() = doTestTyping("-", "Script.fsx")
  fun `nuget reference - replace whole package`() = doTestTyping("FSharp.", "Script.fsx")
  fun `nuget reference - replace path 01`() = doTestChooseItem("nuget:", "Script.fsx")
  fun `nuget reference - replace path 02`() = doTestChooseItem("nuget:", "Script.fsx")
  fun `nuget reference - replace path part`() = doTestChooseItem("Folder3/", "Script.fsx")
}
