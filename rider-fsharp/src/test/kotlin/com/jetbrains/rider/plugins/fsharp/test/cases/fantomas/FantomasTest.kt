package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.plugins.fsharp.logs.FSharpLogTraceScenarios
import com.jetbrains.rider.plugins.fsharp.test.withEditorConfig
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class FantomasTest : EditorTestBase() {
  override fun getSolutionDirectoryName() = "FormatCodeApp"
  override val traceScenarios: Set<LogTraceScenario>
    get() = super.traceScenarios + FSharpLogTraceScenarios.Fantomas

  @Test
  fun withEditorConfig() = doTest("EditorConfig.fs")

  @Test
  fun simpleFormatting() = doTest("Simple.fs")

  @Test
  fun formatLastFile() = doTest("Program.fs")

  private fun doTest(fileName: String) {
    withEditorConfig(project) {
      withOpenedEditor(fileName) {
        waitForDaemon()
        reformatCode()
        executeWithGold(testGoldFile) {
          dumpOpenedDocument(it, project!!, false)
        }
      }
    }
  }
}
