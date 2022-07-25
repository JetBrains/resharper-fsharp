package projectModel

import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
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
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.syncFromBackend
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import java.io.PrintStream

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

            printStream.appendLine("===================")
            printStream.println(caption)
            printStream.println()
            printStream.println(project.fcsHost.dumpFcsModuleReader.syncFromBackend(Unit, project))
        }
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
                assertHasErrorsAndProjectReferences(it, "Unload project", true, emptyList())

                reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
                assertHasErrorsAndProjectReferences(it, "Reload project", false, listOf("CSharpProject"))
            }
        }
    }
}