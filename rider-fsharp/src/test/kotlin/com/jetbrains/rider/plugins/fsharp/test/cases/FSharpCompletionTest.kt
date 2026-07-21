package com.jetbrains.rider.plugins.fsharp.test.cases

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.facades.editor.PatchEngineEditorTestMode
import com.jetbrains.rider.test.junit5.base.PatchEngineCompletionTestBase
import com.jetbrains.rider.test.scriptingApi.dumpOpenedEditorFacade
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.MethodOrderer
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.TestMethodOrder

// Order by name due to tests dependency on their order. TODO: Investigate the reason of dependency and fix it
@TestMethodOrder(MethodOrderer.MethodName::class)
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
  fun `nuget reference - package name`() = doTestTyping("o", "Script.fsx")

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
  @Test
  fun `comments - language injections`() = doTestChooseItem("f#")
  @Test
  fun `doc comments - not available`() {
    dumpOpenedEditorFacade("Program.fs", "Program.fs") {
      waitForDaemon()
      callBasicCompletion()
      ensureThereIsNoLookup()
    }
  }
}

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
class FSharpCompletionSequentialTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.Sequential)

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
class FSharpCompletionSpeculativeTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.Speculative)

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Mute("RIDER-116517")
class FSharpCompletionSpeculativeAndForceRebaseTest : FSharpCompletionTestBase(PatchEngineEditorTestMode.SpeculativeAndForceRebase)
