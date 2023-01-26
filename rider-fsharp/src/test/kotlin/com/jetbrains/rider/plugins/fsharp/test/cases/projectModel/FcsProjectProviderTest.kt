package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.testFramework.ProjectViewTestUtil
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.editors.getProjectModelId
import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.withNonFSharpProjectReferences
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.assertAllProjectsWereLoaded
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.syncFromBackend
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test

@Suppress("UnstableApiUsage")
@Test
@TestEnvironment(coreVersion = CoreVersion.LATEST_STABLE)
class FcsProjectProviderTest : BaseTestWithSolution() {
  override fun getSolutionDirectoryName() = throw Exception("Solutions are set in tests below")

  override val traceCategories
    get() = super.traceCategories.plus("JetBrains.ReSharper.Plugins.FSharp.Checker.FcsProjectProvider")

  @BeforeMethod(alwaysRun = true)
  fun setUpTestCaseProjectModel() {
    ProjectViewTestUtil.setupImpl(project, true)
  }

  private fun EditorImpl.assertFcsStampAndReferencedProjectNames(
    editorImpl: EditorImpl,
    expectedStamp: Long,
    expectedReferencedProjects: List<String>
  ) {
    val project = project!!
    val workspaceModel = WorkspaceModel.getInstance(project)
    val fcsHost = project.fcsHost

    val fileProjectModelEntity = workspaceModel.getProjectModelEntity(editorImpl.getProjectModelId())
    val projectProjectModelId = fileProjectModelEntity?.containingProjectEntity()?.getId(project)!!

    val stamp = fcsHost.dumpFcsProjectStamp.syncFromBackend(projectProjectModelId, project)!!
    assert(expectedStamp == stamp)

    val referencedProjects = fcsHost.dumpFcsReferencedProjects.syncFromBackend(projectProjectModelId, project)!!
    assert(expectedReferencedProjects == referencedProjects)
  }

  private fun assertHasErrorsAndProjectStampAndReferences(
    fileName: String,
    hasErrors: Boolean,
    expectedStamp: Long,
    expectedReferencedProjects: List<String>
  ) {
    withOpenedEditor(project, fileName) {
      waitForDaemon()
      project!!.fcsHost.dumpFcsModuleReader.sync(Unit)
      assert(markupAdapter.hasErrors == hasErrors)
      assertFcsStampAndReferencedProjectNames(this, expectedStamp, expectedReferencedProjects)
    }
  }

  @TestEnvironment(solution = "ProjectReferencesFSharp")
  fun projectReferencesFSharp() {
    assertAllProjectsWereLoaded(project)
    assertHasErrorsAndProjectStampAndReferences("ReferenceFrom/Library.fs", true, 0, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesFSharp", "ReferenceFrom"), "<ReferenceTo>")
    assertHasErrorsAndProjectStampAndReferences("ReferenceFrom/Library.fs", false, 2, listOf("ReferenceTo"))

    waitForDaemonCloseAllOpenEditors(project)
    deleteElement(
      project,
      arrayOf(
        "ProjectReferencesFSharp",
        "ReferenceFrom",
        "Dependencies",
        ".NETStandard 2.0",
        "Projects",
        "ReferenceTo/1.0.0"
      )
    )
    assertHasErrorsAndProjectStampAndReferences("ReferenceFrom/Library.fs", true, 3, emptyList())
  }

  @TestEnvironment(solution = "ProjectReferencesCSharp")
  fun projectReferencesCSharp() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded(project)
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, 0, emptyList())

      waitForDaemonCloseAllOpenEditors(project)
      addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, 1, listOf("CSharpProject"))

      buildSolutionWithReSharperBuild()
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, 1, listOf("CSharpProject"))

      waitForDaemonCloseAllOpenEditors(project)
      deleteElement(
        project,
        arrayOf(
          "ProjectReferencesCSharp",
          "FSharpProject",
          "Dependencies",
          ".NETStandard 2.0",
          "Projects",
          "CSharpProject/1.0.0"
        )
      )
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, 2, emptyList())

      waitForDaemonCloseAllOpenEditors(project)
      addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, 3, listOf("CSharpProject"))
    }
  }

  @TestEnvironment(solution = "ProjectReferencesCSharp")
  fun projectReferencesCSharpNoModuleReader() {
    assertAllProjectsWereLoaded(project)
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, 0, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, 1, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    buildSolutionWithReSharperBuild()
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, 1, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    deleteElement(
      project,
      arrayOf(
        "ProjectReferencesCSharp",
        "FSharpProject",
        "Dependencies",
        ".NETStandard 2.0",
        "Projects",
        "CSharpProject/1.0.0"
      )
    )

    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, 3, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, 4, emptyList())
  }
}
