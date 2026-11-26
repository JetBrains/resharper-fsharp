package com.jetbrains.rider.plugins.fsharp.test.cases.unitTesting

import com.intellij.openapi.editor.impl.EditorImpl
import com.jetbrains.rdclient.daemon.util.annotateDocumentWithHighlighterTags
import com.jetbrains.rdclient.daemon.util.isBackendGutterMark
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.PerTestSettingsTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.markupContributor
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.withBuildApiFacade
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.withSolution
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.io.PrintStream
import java.time.Duration

@Tag(TeamCityTags.Plugins.FSharp.General)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
class ExpectoGutterMarksTest : PerTestSettingsTestBase() {
  private val openSolutionParams: OpenSolutionParams
    get() = OpenSolutionParams().apply {
      restoreNuGetPackages = true
      waitForCaches = true
      waitForSolutionBuilder = true
    }

  private fun EditorImpl.dumpGutterMarks(stream: PrintStream, title: String) {
    stream.println(title)
    stream.println(annotateDocumentWithHighlighterTags(markupContributor.markupAdapter, { it.isBackendGutterMark }))
  }

  @Test
  fun marksAfterBuild() {
    withSolution("ExpectoTests", openSolutionParams) {
      withOpenedEditor("Tests.fs") {
        executeWithGold(testGoldFile) { stream ->
          waitForDaemon()
          dumpGutterMarks(stream, "Before the build:")

          withBuildApiFacade { buildSolutionWithReSharperBuild() }
          withUtFacade(project!!) { it.waitForDiscovering() }
          pumpMessages(Duration.ofSeconds(5))
          waitForDaemon()
          dumpGutterMarks(stream, "After the build, no edit:")

          typeWithLatency(" ")
          waitForDaemon()
          dumpGutterMarks(stream, "After an edit:")
        }
      }
    }
  }
}
