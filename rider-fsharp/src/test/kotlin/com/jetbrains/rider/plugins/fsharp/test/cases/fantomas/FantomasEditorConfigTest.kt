package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.jetbrains.rider.plugins.fsharp.test.framework.flushFileChanges
import com.jetbrains.rider.plugins.fsharp.test.framework.withEditorConfig
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import org.testng.annotations.Test
import java.io.File

@Test
@TestSettings(sdkVersion = SdkVersion.DOT_NET_7)
class FantomasEditorConfigTest : FantomasDotnetToolTestBase() {
  override val testSolution = "FormatCodeApp"
  override fun beforeDoTestWithDocuments() {
    super.beforeDoTestWithDocuments()

    val sourceEditorConfigFile = File(testCaseSourceDirectory, ".editorconfig")
    val slnEditorConfigFile = File(testWorkDirectory, ".editorconfig")
    sourceEditorConfigFile.copyTo(slnEditorConfigFile, true)
    flushFileChanges(project)
  }

  private fun doEditorConfigTestWithVersion(fantomasVersion: String) {
    withEditorConfig(project) {
      withFantomasLocalTool("fantomas", fantomasVersion) {
        withOpenedEditor("Program.fs", "Brackets.fs") {
          waitForDaemon()
          reformatCode()
          executeWithGold(testGoldFile) {
            dumpOpenedDocument(it, project!!, false)
          }
        }
      }
    }
  }

  private fun doEditorConfigTest() {
    withEditorConfig(project) {
      withOpenedEditor("Program.fs", "Newline.fs") {
        waitForDaemon()
        reformatCode()
        executeWithGold(testGoldFile) {
          dumpOpenedDocument(it, project!!, false)
        }
      }
    }
  }

  @Test(description = "Doesn't support experimental_stroustrup, 'cramped' should be used instead")
  fun `editorconfig enum values 01`() = doEditorConfigTestWithVersion("6.0.1")

  @Test(description = "Supports stroustrup")
  fun `editorconfig enum values 02`() = doEditorConfigTestWithVersion("6.0.1")

  @Test(description = "RIDER-111743")
  fun `editorconfig overrides 01 - final newline`() = doEditorConfigTest()

  @Test(description = "RIDER-111743")
  fun `editorconfig overrides 02 - final newline`() = doEditorConfigTest()
}
