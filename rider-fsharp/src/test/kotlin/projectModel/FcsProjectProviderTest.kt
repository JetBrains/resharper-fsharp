package projectModel

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
@TestEnvironment(coreVersion = CoreVersion.DEFAULT)
class FcsProjectProviderTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = throw Exception("Solutions are set in tests below")

    @BeforeMethod(alwaysRun = true)
    fun setUpTestCaseProjectModel() {
        ProjectViewTestUtil.setupImpl(project, true)
    }

    private fun EditorImpl.assertReferencedFcsProjectNames(editorImpl: EditorImpl, expectedReferencedProjects: List<String>) {
        val project = project!!
        val workspaceModel = WorkspaceModel.getInstance(project)
        val fcsHost = project.fcsHost

        val fileProjectModelEntity = workspaceModel.getProjectModelEntity(editorImpl.getProjectModelId())
        val projectProjectModelId = fileProjectModelEntity?.containingProjectEntity()?.getId(project)!!
        val referencedProjects = fcsHost.dumpFcsRefrencedProjects.syncFromBackend(projectProjectModelId, project)!!
        assert(expectedReferencedProjects == referencedProjects)
    }

    private fun assertHasErrorsAndProjectReferences(fileName: String, hasErrors: Boolean, expectedReferencedProjects: List<String>) {
        withOpenedEditor(project, fileName) {
            waitForDaemon()
            assert(markupAdapter.hasErrors == hasErrors)
            assertReferencedFcsProjectNames(this, expectedReferencedProjects)
        }
    }

    @TestEnvironment(solution = "ProjectReferencesFSharp")
    fun projectReferencesFSharp() {
        assertAllProjectsWereLoaded(project)
        assertHasErrorsAndProjectReferences("ReferenceFrom/Library.fs", true, emptyList())

        waitForDaemonCloseAllOpenEditors(project)
        addReference(project, arrayOf("ProjectReferencesFSharp", "ReferenceFrom"), "<ReferenceTo>")
        assertHasErrorsAndProjectReferences("ReferenceFrom/Library.fs", false, listOf("ReferenceTo"))

        waitForDaemonCloseAllOpenEditors(project)
        deleteElement(project, arrayOf("ProjectReferencesFSharp", "ReferenceFrom", "Dependencies", ".NETStandard 2.0", "Projects", "ReferenceTo/1.0.0"))
        assertHasErrorsAndProjectReferences("ReferenceFrom/Library.fs", true, emptyList())
    }

    @TestEnvironment(solution = "ProjectReferencesCSharp")
    fun projectReferencesCSharp() {
        withNonFSharpProjectReferences {
            assertAllProjectsWereLoaded(project)
            assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", true, emptyList())

            waitForDaemonCloseAllOpenEditors(project)
            addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
            assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))

            buildSolutionWithReSharperBuild()
            assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))

            waitForDaemonCloseAllOpenEditors(project)
            deleteElement(project, arrayOf("ProjectReferencesFSharp", "FSharpProject", "Dependencies", ".NETStandard 2.0", "Projects", "CSharpProject/1.0.0"))
            assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", true, emptyList())

            waitForDaemonCloseAllOpenEditors(project)
            addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
            assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", false, listOf("CSharpProject"))
        }
    }

    @TestEnvironment(solution = "ProjectReferencesCSharp")
    fun projectReferencesCSharpNoModuleReader() {
        assertAllProjectsWereLoaded(project)
        assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", true, emptyList())

        waitForDaemonCloseAllOpenEditors(project)
        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", true, emptyList())

        waitForDaemonCloseAllOpenEditors(project)
        buildSolutionWithReSharperBuild()
        assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", false, emptyList())

        waitForDaemonCloseAllOpenEditors(project)
        deleteElement(project, arrayOf("ProjectReferencesFSharp", "FSharpProject", "Dependencies", ".NETStandard 2.0", "Projects", "CSharpProject/1.0.0"))
        assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", true, emptyList())

        waitForDaemonCloseAllOpenEditors(project)
        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        assertHasErrorsAndProjectReferences("FSharpProject/Library.fs", false, emptyList())
    }
}
