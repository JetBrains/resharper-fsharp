package com.jetbrains.rider.plugins.fsharp.test.cases

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.PatchEngineCompletionTestBase
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpOpenedEditorFacade
import org.testng.annotations.Test

abstract class FSharpCompletionTestBase(mode: PatchEngineEditorTestMode) : PatchEngineCompletionTestBase(mode) {
  override val testSolution: String = "CoreConsoleApp"
  override val restoreNuGetPackages = true

  @Test
  fun namespaceKeyword() = doTestTyping("names")

  @Test
  fun listModule() = doTestChooseItem("List")

  @Test
  fun listModuleValue() = doTestTyping("filt")

  @Mute("RIDER-103671")
  @Test
  fun localVal01() = doTestChooseItem("x")

  @Test
  fun localVal02() = doTestTyping("x")

  @Test
  fun qualified01() = doTestChooseItem("a")

  @Test
  fun qualified02() = doTestChooseItem("a")

  private fun doTestTyping(typed: String, fileName: String = "Program.fs") {
    dumpOpenedEditorFacade(fileName, fileName) {
      waitForDaemon()
      type(typed)
      callBasicCompletion()
      waitForCompletion()
      completeWithTab()
    }
  }

  private fun doTestChooseItem(item: String, fileName: String = "Program.fs") {
    dumpOpenedEditorFacade(fileName, fileName) {
      waitForDaemon()
      callBasicCompletion()
      waitForCompletion()
      completeWithTab(item)
    }
  }

  @Test
  fun `nuget reference - simple`() = doTestTyping("nu", "Script.fsx")

  @Test
  fun `nuget reference - reference`() = doTestTyping("nu", "Script.fsx")

  @Test
  fun `nuget reference - triple quoted string`() = doTestTyping("nu", "Script.fsx")

  @Test
  fun `nuget reference - verbatim string`() = doTestTyping("nu", "Script.fsx")

  @Test
  fun `nuget reference - package name`() = doTestTyping("JetBrains.Annotatio", "Script.fsx")

  @Test
  fun `nuget reference - version`() = doTestTyping("-", "Script.fsx")

  @Test
  fun `nuget reference - replace whole package`() = doTestTyping("FSharp.", "Script.fsx")

  @Test
  fun `nuget reference - replace path 01`() = doTestChooseItem("nuget:", "Script.fsx")

  @Test
  fun `nuget reference - replace path 02`() = doTestChooseItem("nuget:", "Script.fsx")

  @Mute("RIDER-103666")
  @Test
  fun `nuget reference - replace path part`() = doTestChooseItem("Folder3/", "Script.fsx")

  @Mute("RIDER-104549")
  fun `comments - language injections`() = doTestChooseItem("f#")
  fun `doc comments - not available`() {
    dumpOpenedEditorFacade("Program.fs", "Program.fs") {
      waitForDaemon()
      callBasicCompletion()
      ensureThereIsNoLookup()
    }
  }
}

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpCompletionTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.LegacyAsyncTyping)

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpCompletionSequentialTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.Sequential)

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpCompletionSpeculativeTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.Speculative)

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@Mute("RIDER-116517")
class FSharpCompletionSpeculativeAndForceRebaseTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.SpeculativeAndForceRebase)
