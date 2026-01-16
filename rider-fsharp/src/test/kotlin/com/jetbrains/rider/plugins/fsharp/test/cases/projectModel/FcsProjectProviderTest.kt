package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.testFramework.ProjectViewTestUtil
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.editors.getProjectModelId
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.framework.withoutNonFSharpProjectReferences
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.syncFromBackend
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import kotlin.test.assertEquals

@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
class FcsProjectProviderTest : PerTestSolutionTestBase() {
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
    withOpenedEditor(fileName) {
      waitForDaemon()
      project!!.fcsHost.dumpFcsModuleReader.sync(Unit)
      assertEquals(hasErrors, markupAdapter.hasErrors)
      assertFcsStampAndReferencedProjectNames(this, expectedReferencedProjects)
    }
  }

  @Solution("ProjectReferencesFSharp")
  @Test
  fun projectReferencesFSharp() {
    assertAllProjectsWereLoaded()
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

  @Test
  @Solution("ProjectReferencesCSharp")
  fun projectReferencesCSharp() {
    assertAllProjectsWereLoaded()
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
        "CSharpProject"
      )
    )
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

    waitForDaemonCloseAllOpenEditors(project)
    addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
    assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))
  }

  @Test
  @Solution("ProjectReferencesCSharp")
  fun projectReferencesCSharpNoModuleReader() {
    withoutNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()
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
          "CSharpProject"
        )
      )

      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", true, emptyList())

      waitForDaemonCloseAllOpenEditors(project)
      addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
      assertHasErrorsAndProjectStampAndReferences("FSharpProject/Library.fs", false, emptyList())
    }
  }
}
