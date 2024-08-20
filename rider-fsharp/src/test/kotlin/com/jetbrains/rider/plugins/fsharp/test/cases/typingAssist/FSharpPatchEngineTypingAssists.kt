package com.jetbrains.rider.plugins.fsharp.test.cases.typingAssist

import com.intellij.openapi.actionSystem.IdeActions
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.PatchEngineEditorTestBase
import com.jetbrains.rider.test.base.PatchEngineEditorTestMode
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import org.testng.annotations.DataProvider
import org.testng.annotations.Test

abstract class FSharpTypingAssistPatchEngineTest(mode: PatchEngineEditorTestMode) : PatchEngineEditorTestBase(mode) {
  override val checkTextControls = false
  override val testSolution = "CoreConsoleApp"

  @DataProvider(name = "simpleCases")
  fun simpleCases() = arrayOf(
    arrayOf("emptyFile"),
    arrayOf("docComment"),
    arrayOf("indent1"),
    arrayOf("indent2"),
    arrayOf("removeSelection1"),
    arrayOf("removeSelection2"),
    arrayOf("trailingSpace")
  )

  @Test(dataProvider = "simpleCases")
  fun testStartNewLine(caseName: String) {
    dumpOpenedEditorFacade("Program.fs", "Program.fs") {
      typeOrCallAction(IdeActions.ACTION_EDITOR_START_NEW_LINE)
    }
  }

  @Test
  fun testStartNewLineUndo() {
    dumpOpenedEditorFacade("Program.fs", "Program.fs") {
      typeOrCallAction(IdeActions.ACTION_EDITOR_START_NEW_LINE)
      undo()
    }
  }
}

@Test
@Subsystem(SubsystemConstants.TYPING_ASSIST)
@Feature("Typing Assist")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpTypingAssistPatchEngineSpeculativeRebaseProhibitedTest :
  FSharpTypingAssistPatchEngineTest(PatchEngineEditorTestMode.SpeculativeRebaseProhibited)

@Test
@Subsystem(SubsystemConstants.TYPING_ASSIST)
@Feature("Typing Assist")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpTypingAssistPatchEngineSpeculativeAndForceRebaseTest :
  FSharpTypingAssistPatchEngineTest(PatchEngineEditorTestMode.SpeculativeAndForceRebase)
