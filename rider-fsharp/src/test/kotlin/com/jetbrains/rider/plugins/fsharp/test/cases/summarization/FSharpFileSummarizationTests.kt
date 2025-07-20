package com.jetbrains.rider.plugins.fsharp.test.cases.summarization

import com.intellij.openapi.components.serviceAsync
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rd.ide.model.FileSummaryRequest
import com.jetbrains.rd.ide.model.junieModel
import com.jetbrains.rdclient.util.idea.toIOFile
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntities
import com.jetbrains.rider.protocol.protocol
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.changeFileSystem2
import com.jetbrains.rider.test.scriptingApi.getVirtualFileFromPath
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.test.scriptingApi.runBlockingWithProtocolPumping
import org.testng.annotations.Test
import java.nio.file.Path
import kotlin.io.path.Path
import kotlin.io.path.div
import kotlin.io.path.name

class FSharpFileSummarizationTests : PerTestSolutionTestBase() {

  @TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
  @Solution("CoreConsoleApp")
  @Test
  fun testFSharpFileSummarization() = doTest(Path("Program.fs"))

  private fun doTest(solutionRelativeFilePath: Path) {
    changeFileSystem2(project) {
      replaceFileContent(project, solutionRelativeFilePath.name)
      arrayOf(getVirtualFileFromPath(solutionRelativeFilePath.name, project.solutionDirectory).toIOFile())
    }

    executeWithGold(testGoldFile) { gold ->
      val summary = runBlockingWithProtocolPumping(project.protocol, testMethod.name) {
        val projectModelEntityId = project.serviceAsync<WorkspaceModel>()
          .getProjectModelEntities(project.solutionDirectory.toPath() / solutionRelativeFilePath, project)
          .single()
          .getId(project).shouldNotBeNull("Project model id should not be null")

        val model = project.solution.junieModel
        model.getFileSummary.startSuspending(FileSummaryRequest(projectModelEntityId)).summary
      }

      gold.print(summary)
    }
  }
}
