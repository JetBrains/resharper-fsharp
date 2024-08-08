package com.jetbrains.rider.plugins.fsharp.test.cases.typingAssist

import com.intellij.openapi.actionSystem.IdeActions
import com.jetbrains.rdclient.patches.PATCH_ENGINE_REGISTRY_SETTING
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.TypingAssistTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.typeOrCallAction
import com.jetbrains.rider.test.scriptingApi.undo
import org.testng.annotations.DataProvider
import org.testng.annotations.Test

@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpPatchEngineTypingAssists : TypingAssistTestBase() {
  override val registrySetting: Map<String, String>
    get() = mapOf(PATCH_ENGINE_REGISTRY_SETTING to "true")

  override val checkTextControls = false

  override fun getSolutionDirectoryName(): String = "CoreConsoleApp"

  @DataProvider(name = "simpleCases")
  fun simpleCases() = arrayOf(
    arrayOf("emptyFile"),
    arrayOf("docComment"),
    arrayOf("indent1"),
    arrayOf("indent2"),
    arrayOf("removeSelection1"),
    arrayOf("removeSelection2")
  )

  @Test(dataProvider = "simpleCases")
  fun testStartNewLine(caseName: String) {
    dumpOpenedEditor("Program.fs", "Program.fs") {
      typeOrCallAction(IdeActions.ACTION_EDITOR_START_NEW_LINE)
    }
  }

  @Test
  fun testStartNewLineUndo() {
    dumpOpenedEditor("Program.fs", "Program.fs") {
      typeOrCallAction(IdeActions.ACTION_EDITOR_START_NEW_LINE)
      undo()
    }
  }
}
