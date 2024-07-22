package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.fileEditor.ex.FileEditorManagerEx
import com.intellij.openapi.project.Project
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForNextDaemon
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.editors.getProjectModelId
import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.withNonFSharpProjectReferences
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.assertAllProjectsWereLoaded
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.waitForDaemon
import com.jetbrains.rider.util.idea.syncFromBackend
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.PrintStream
import java.time.Duration

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FcsModuleReaderTest : ProjectModelBaseTest() {
  companion object {
    private var launchCounter = 0
  }

  override val testSolution: String = "EmptySolution"
  override val restoreNuGetPackages = true

  @AfterMethod(alwaysRun = true)
  fun tearDownTestCase() {
    launchCounter = 0
  }

  @BeforeMethod
  fun beforeTestCase() {
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

  private fun openFsFileDumpModuleReader(
    printStream: PrintStream,
    caption: String,
    hasErrors: Boolean,
    expectedReferencedProjects: List<String>
  ) {
    val project = project
    withOpenedEditor(project, "FSharpProject/Library.fs") {
      waitForNextDaemon()
      assert(markupAdapter.hasErrors == hasErrors)
      assertFcsStampAndReferencedProjectNames(this, expectedReferencedProjects)
      dumpModuleReader(printStream, caption, project)
    }
    waitForDaemonCloseAllOpenEditors(project)
  }

  private fun dumpModuleReader(
    printStream: PrintStream,
    caption: String,
    project: Project
  ) {
    printStream.appendLine("===================")
    printStream.println(caption)
    printStream.println()
    printStream.println(project.fcsHost.dumpFcsModuleReader.syncFromBackend(Unit, project))
  }

  // Copied from EditorTestBase
  private fun waitForEditorSwitch(targetFileName: String, hostTimeout: Duration = Duration.ofSeconds(20)) {
    frameworkLogger.info("Waiting for editor switch")
    val instanceEx = FileEditorManagerEx.getInstanceEx(project)

    pumpMessages(hostTimeout) {
      val name = instanceEx.selectedEditor?.file?.name
      name == targetFileName
    }

    val name = instanceEx.selectedEditor?.file?.name

    assert(name == targetFileName) { "Editor should be switched. Current editor: $name" }
    frameworkLogger.info("Editor switched to $targetFileName")
  }


  @Mute("RIDER-102738")
  @TestEnvironment(solution = "ProjectReferencesCSharp")
  fun testUnloadReloadCSharp() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded(project)
        dumpModuleReader(it, "Init", project)

        openFsFileDumpModuleReader(it, "1. Open F# file", true, emptyList())

        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        dumpModuleReader(it, "2. Add reference", project)

        openFsFileDumpModuleReader(it, "3. Open F# file", false, listOf("CSharpProject"))

        unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "4. Unload C# project", project)

        openFsFileDumpModuleReader(it, "5. Open F# file", true, emptyList())

        reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "6. Reload C# project", project)

        openFsFileDumpModuleReader(it, "7. Open F# file", false, listOf("CSharpProject"))
      }
    }
  }

  @Mute("Temporary because of RIDER-20984")
  @TestEnvironment(solution = "ProjectReferencesCSharp")
  fun testTypeInsideClassUnloadReload() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded(project)
        openFsFileDumpModuleReader(it, "Init", true, emptyList())

        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        dumpModuleReader(it, "1. Add reference", project)

        openFsFileDumpModuleReader(it, "2. Open F# file", false, listOf("CSharpProject"))

        withOpenedEditor(project, "CSharpProject/Class1.cs") {
          typeFromOffset(" ", 75)
          waitForNextDaemon()
        }

        waitForDaemonCloseAllOpenEditors(project)
        dumpModuleReader(it, "3. Type inside C# file", project)

        openFsFileDumpModuleReader(it, "4. Open F# file", false, listOf("CSharpProject"))

        unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "5. Unload C# project", project)

        openFsFileDumpModuleReader(it, "6. Open F# file", true, emptyList())

        reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "7. Reload C# project", project)

        openFsFileDumpModuleReader(it, "8. Open F# file", false, listOf("CSharpProject"))

        withOpenedEditor(project, "CSharpProject/Class1.cs") {
          typeFromOffset(" ", 75)
        }

        waitForDaemonCloseAllOpenEditors(project)
        dumpModuleReader(it, "9. Type inside C# file", project)

        unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "10. Unload C# project", project)

        openFsFileDumpModuleReader(it, "11. Open F# file", true, emptyList())

        reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "12. Reload C# project", project)

        openFsFileDumpModuleReader(it, "13. Open F# file", false, listOf("CSharpProject"))
      }
    }
  }

  @Mute
  @TestEnvironment(solution = "ProjectReferencesCSharp")
  fun testTypeOutsideClassUnloadReload() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded(project)
        openFsFileDumpModuleReader(it, "Init", true, emptyList())

        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        dumpModuleReader(it, "1. Add reference", project)

        openFsFileDumpModuleReader(it, "2. Open F# file", false, listOf("CSharpProject"))

        withOpenedEditor(project, "CSharpProject/Class1.cs") {
          typeFromOffset(" ", 129)
        }

        waitForDaemonCloseAllOpenEditors(project)
        dumpModuleReader(it, "3. Type inside C# file", project)

        openFsFileDumpModuleReader(it, "4. Open F# file", false, listOf("CSharpProject"))
      }
    }
  }


  @Mute("RIDER-102738")
  @TestEnvironment(solution = "ProjectReferencesCSharp2")
  fun testLoadReferenced() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded(project)
        openFsFileDumpModuleReader(it, "Init", false, listOf("CSharpProject"))

        waitForDaemonCloseAllOpenEditors(project)
        unloadProject(arrayOf("ProjectReferencesCSharp2", "CSharpProject"))
        dumpModuleReader(it, "2. Unload C# project", project)

        waitForDaemonCloseAllOpenEditors(project)
        openFsFileDumpModuleReader(it, "3. Open F#", true, emptyList())
      }
    }
  }

  override val backendLoadedTimeout: Duration
    get() = Duration.ofMinutes(20)

  @TestEnvironment(solution = "ProjectReferencesCSharp2")
  fun testGotoUsagesFromCSharp() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded(project)
      withOpenedEditor(project, "CSharpProject/Class1.cs", "Class1.cs") {
        waitForNextDaemon()
        callAction(IdeActions.ACTION_GOTO_DECLARATION)
        waitForEditorSwitch("Library.fs")
      }
    }
  }

  @TestEnvironment(solution = "ProjectReferencesCSharp3")
  fun testGotoUsagesFromCSharpChangeCSharp() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded(project)
      withOpenedEditor(project, "CSharpProject/Class1.cs", "Class1.cs") {
        typeWithLatency("1")
        waitForNextDaemon()
        callAction(IdeActions.ACTION_GOTO_DECLARATION)
        waitForEditorSwitch("Library.fs")
      }
    }
  }

  @TestEnvironment(solution = "ProjectReferencesCSharp3")
  fun testGotoUsagesFromCSharpChangeCSharp2() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded(project)

      withOpenedEditor(project, "FSharpProject/Library.fs") {
        waitForNextDaemon()
      }

      waitForDaemonCloseAllOpenEditors(project)

      withOpenedEditor(project, "CSharpProject/Class1.cs", "Class1.cs") {
        typeWithLatency("1")
        waitForNextDaemon()
        callAction(IdeActions.ACTION_GOTO_DECLARATION)
        waitForEditorSwitch("Library.fs")
      }
    }
  }
}
