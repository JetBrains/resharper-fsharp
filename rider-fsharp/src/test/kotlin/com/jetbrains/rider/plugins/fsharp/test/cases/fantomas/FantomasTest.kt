package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.jetbrains.rider.plugins.fsharp.test.framework.withEditorConfig
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.junit5.base.EditorTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.DOT_NET_6, buildTool = BuildTool.SDK)
class FantomasTest : EditorTestBase() {
  override val testSolution: String = "FormatCodeApp"

  @Test
  fun withEditorConfig() = doTest("EditorConfig.fs")

  @Test
  fun simpleFormatting() = doTest("Simple.fs")

  @Mute("RIDER-139900")
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
