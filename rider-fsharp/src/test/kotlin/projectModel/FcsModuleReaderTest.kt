package projectModel

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.fileEditor.ex.FileEditorManagerEx
import com.intellij.openapi.project.Project
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rdclient.testFramework.waitForNextDaemon
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.editors.getProjectModelId
import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.withNonFSharpProjectReferences
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.assertAllProjectsWereLoaded
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.syncFromBackend
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import java.io.PrintStream
import java.time.Duration

@Suppress("UnstableApiUsage")
@Test
@TestEnvironment(coreVersion = CoreVersion.DEFAULT)
class FcsModuleReaderTest : ProjectModelBaseTest() {
    companion object {
        private var launchCounter = 0
    }

    override fun getSolutionDirectoryName() = "EmptySolution"
    override val restoreNuGetPackages = true

    @AfterMethod(alwaysRun = true)
    fun tearDownTestCase() {
        launchCounter = 0
    }

    private fun EditorImpl.assertReferencedFcsProjectNames(
        editorImpl: EditorImpl,
        expectedReferencedProjects: List<String>
    ) {
        val project = project!!
        val workspaceModel = WorkspaceModel.getInstance(project)
        val fcsHost = project.fcsHost

        val fileProjectModelEntity = workspaceModel.getProjectModelEntity(editorImpl.getProjectModelId())
        val projectProjectModelId = fileProjectModelEntity?.containingProjectEntity()?.getId(project)!!
        val referencedProjects = fcsHost.dumpFcsRefrencedProjects.syncFromBackend(projectProjectModelId, project)!!
        assert(expectedReferencedProjects == referencedProjects)
    }

    private fun assertHasErrorsAndProjectReferences(
        printStream: PrintStream,
        caption: String,
        hasErrors: Boolean,
        expectedReferencedProjects: List<String>
    ) {
        val project = project
        withOpenedEditor(project, "FSharpProject/Library.fs") {
            waitForDaemon()
            assert(markupAdapter.hasErrors == hasErrors)
            assertReferencedFcsProjectNames(this, expectedReferencedProjects)
            dumpModuleReader(printStream, caption, project)
        }
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


    @TestEnvironment(solution = "ProjectReferencesCSharp")
    fun testUnloadReloadCSharp() {
        executeWithGold(testGoldFile) {
            withNonFSharpProjectReferences {
                assertAllProjectsWereLoaded(project)
                assertHasErrorsAndProjectReferences(it, "Init", true, emptyList())

                waitForDaemonCloseAllOpenEditors(project)
                addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
                assertHasErrorsAndProjectReferences(it, "Add reference", false, listOf("CSharpProject"))

                unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "Unload C# project", true, emptyList())

                reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "Reload C# project", false, listOf("CSharpProject"))
            }
        }
    }

    @TestEnvironment(solution = "ProjectReferencesCSharp")
    fun testTypeInsideClassUnloadReload() {
        executeWithGold(testGoldFile) {
            withNonFSharpProjectReferences {
                assertAllProjectsWereLoaded(project)
                assertHasErrorsAndProjectReferences(it, "Init", true, emptyList())

                waitForDaemonCloseAllOpenEditors(project)
                addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
                assertHasErrorsAndProjectReferences(it, "1. Add reference", false, listOf("CSharpProject"))

                waitForDaemonCloseAllOpenEditors(project)
                withOpenedEditor(project, "CSharpProject/Class1.cs") {
                    typeFromOffset(" ", 75)
                    waitForDaemon()
                }

                waitForDaemonCloseAllOpenEditors(project)
                dumpModuleReader(it, "2. Type", project)

                waitForDaemonCloseAllOpenEditors(project)
                assertHasErrorsAndProjectReferences(it, "3. Open F#", false, listOf("CSharpProject"))
                waitForDaemonCloseAllOpenEditors(project)

                unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "4. Unload C# projject", true, emptyList())

                reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "5. Reload C# project", false, listOf("CSharpProject"))

                withOpenedEditor(project, "CSharpProject/Class1.cs") {
                    typeFromOffset(" ", 75)
                }

                waitForDaemonCloseAllOpenEditors(project)
                dumpModuleReader(it, "6. Type", project)

                unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "7. Unload C# projject", true, emptyList())

                reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "8. Reload C# project", false, listOf("CSharpProject"))
            }
        }
    }

    @TestEnvironment(solution = "ProjectReferencesCSharp")
    fun testTypeOutsideClassUnloadReload() {
        executeWithGold(testGoldFile) {
            withNonFSharpProjectReferences {
                assertAllProjectsWereLoaded(project)
                assertHasErrorsAndProjectReferences(it, "Init", true, emptyList())

                waitForDaemonCloseAllOpenEditors(project)
                addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
                assertHasErrorsAndProjectReferences(it, "1. Add reference", false, listOf("CSharpProject"))

                waitForDaemonCloseAllOpenEditors(project)
                withOpenedEditor(project, "CSharpProject/Class1.cs") {
                    typeFromOffset(" ", 129)
                }

                waitForDaemonCloseAllOpenEditors(project)
                dumpModuleReader(it, "2. Type", project)

                waitForDaemonCloseAllOpenEditors(project)
                assertHasErrorsAndProjectReferences(it, "3. Open F#", false, listOf("CSharpProject"))
            }
        }
    }


    @TestEnvironment(solution = "ProjectReferencesCSharp2")
    fun testLoadReferenced() {
        executeWithGold(testGoldFile) {
            withNonFSharpProjectReferences {
                assertAllProjectsWereLoaded(project)
                assertHasErrorsAndProjectReferences(it, "Init", false, listOf("CSharpProject"))
            }
        }
    }

    @TestEnvironment(solution = "ProjectReferencesCSharp2")
    fun testGotoUsagesFromCSharp() {
        withNonFSharpProjectReferences {
            assertAllProjectsWereLoaded(project)
            withOpenedEditor(project, "CSharpProject/Class1.cs", "Class1.cs") {
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
                waitForDaemon()
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
