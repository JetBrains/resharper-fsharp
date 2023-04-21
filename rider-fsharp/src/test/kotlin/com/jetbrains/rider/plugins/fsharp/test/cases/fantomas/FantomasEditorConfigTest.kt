package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.jetbrains.rider.plugins.fsharp.test.flushFileChanges
import com.jetbrains.rider.plugins.fsharp.test.withEditorConfig
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.Test
import java.io.File

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7, reuseSolution = false)
class FantomasEditorConfigTest : FantomasDotnetToolTestBase() {
  override fun getSolutionDirectoryName() = "FormatCodeApp"
  override fun beforeDoTestWithDocuments() {
    super.beforeDoTestWithDocuments()

    val sourceEditorConfigFile = File(testCaseSourceDirectory, ".editorconfig")
    val slnEditorConfigFile = File(tempTestDirectory, ".editorconfig")
    sourceEditorConfigFile.copyTo(slnEditorConfigFile, true)
    flushFileChanges(project)
  }

  private fun doEditorConfigEnumTest(fantomasVersion: String) {
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

  @Test(description = "Doesn't support experimental_stroustrup, 'cramped' should be used instead")
  fun `editorconfig enum values 01`() = doEditorConfigEnumTest("6.0.1")

  @Test(description = "Supports stroustrup")
  fun `editorconfig enum values 02`() = doEditorConfigEnumTest("6.0.1")
}
