package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.testFramework.ProjectViewTestUtil
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.editors.getProjectModelId
import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.withNonFSharpProjectReferences
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.assertAllProjectsWereLoaded
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.syncFromBackend
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import kotlin.test.assertEquals

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FcsProjectProviderTest : BaseTestWithSolution() {
  override val traceCategories
    get() = super.traceCategories.plus("JetBrains.ReSharper.Plugins.FSharp.Checker.FcsProjectProvider")

  @BeforeMethod(alwaysRun = true)
  fun setUpTestCaseProjectModel() {
    ProjectViewTestUtil.setupImpl(project, true)
  }

  private fun EditorImpl.assertFcsStampAndReferencedProjectNames(
    editorImpl: EditorImpl,
    expectedReferencedProjects: List<String>
  ) {
    val project = project!!
    val workspaceModel = WorkspaceModel.getInstance(project)
    val fcsHost = project.fcsHost

    val fileProjectModelEntity = workspaceModel.getProjectModelEntity(editorImpl.getProjectModelId())
    val projectProjectModelId = fileProjectModelEntity?.containingProjectEntity()?.getId(project)!!

    val referencedProjects = fcsHost.dumpFcsReferencedProjects.syncFromBackend(projectProjectModelId, project)!!
    assert(expectedReferencedProjects == referencedProjects)
  }

  private fun assertHasErrorsAndProjectStampAndReferences(
    fileName: String,
    hasErrors: Boolean,
    expectedReferencedProjects: List<String>
  ) {
    withOpenedEditor(project, fileName) {
      waitForDaemon()
      project!!.fcsHost.dumpFcsModuleReader.sync(Unit)
      assertEquals(hasErrors, markupAdapter.hasErrors)
      assertFcsStampAndReferencedProjectNames(this, expectedReferencedProjects)
    }
  }

  @Solution("ProjectReferencesFSharp")
  fun projectReferencesFSharp() {
    assertAllProjectsWereLoaded(project)
    assertHasErrorsAndProjectStampAndReferences("ReferenceFrom/Library.fs", true, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesFSharp", "ReferenceFrom"), "<ReferenceTo>")
    assertHasErrorsAndProjectStampAndReferences("ReferenceFrom/Library.fs", false, listOf("ReferenceTo"))

    waitForDaemonCloseAllOpenEditors(project)
    deleteElement(
      project,
      arrayOf(
        "ProjectReferencesFSharp",
        "ReferenceFrom",
        "Dependencies",
        ".NETStandard 2.0",
        "Projects",
        "ReferenceTo"
      )
    )
    assertHasErrorsAndProjectStampAndReferences("ReferenceFrom/Library.fs", true, emptyList())
  }

  @Mute("Broken after ProjectModelMonitor refactoring")
  @Solution("ProjectReferencesCSharp")
  fun projectReferencesCSharp() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded(project)
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

      waitForDaemonCloseAllOpenEditors(project)
      addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))

      buildSolutionWithReSharperBuild()
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))

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
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

      waitForDaemonCloseAllOpenEditors(project)
      addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))
    }
  }

  @Solution("ProjectReferencesCSharp")
  @Mute("RIDER-100270 Need to somehow set setting before solution load")
  fun projectReferencesCSharpNoModuleReader() {
    assertAllProjectsWereLoaded(project)
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    buildSolutionWithReSharperBuild()
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, emptyList())

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

    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, emptyList())
  }
}
