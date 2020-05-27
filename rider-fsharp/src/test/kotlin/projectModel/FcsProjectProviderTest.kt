package projectModel

import com.intellij.testFramework.ProjectViewTestUtil
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.assertAllProjectsWereLoaded
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.time.Duration

@Test
@TestEnvironment(coreVersion = CoreVersion.DEFAULT)
class FcsProjectProviderTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = throw Exception("Solutions are set in tests below")

    @BeforeMethod(alwaysRun = true)
    fun setUpTestCaseProjectModel() {
        ProjectViewTestUtil.setupImpl(project, true)
    }

    @Test
    @TestEnvironment(solution = "ProjectReferencesFSharp")
    fun projectReferencesFSharp() {
        assertAllProjectsWereLoaded(project)
        withOpenedEditor(project, "ReferenceFrom/Library.fs") {
            waitForDaemon()
            assert(markupAdapter.hasErrors)
        }

        addReference2(project, arrayOf("ProjectReferencesFSharp", "ReferenceFrom"), "<ReferenceTo>")
        withOpenedEditor(project, "ReferenceFrom/Library.fs") {
            waitForDaemon()
            assert(!markupAdapter.hasErrors)
        }

        deleteElement2(project, arrayOf("ProjectReferencesFSharp", "ReferenceFrom", "Dependencies", ".NETStandard 2.0", "Projects", "ReferenceTo/1.0.0"))
        withOpenedEditor(project, "ReferenceFrom/Library.fs") {
            waitForDaemon()
            assert(markupAdapter.hasErrors)
        }
    }

    @Test
    @TestEnvironment(solution = "ProjectReferencesCSharp")
    fun projectReferencesCSharp() {
        val fcsHost = project.solution.rdFSharpModel.fcsHost

        assertAllProjectsWereLoaded(project)
        withOpenedEditor(project, "FSharpProject/Library.fs") {
            waitForDaemon()
            assert(markupAdapter.hasErrors)
        }

        addReference2(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        withOpenedEditor(project, "FSharpProject/Library.fs") {
            waitForDaemon()
            assert(markupAdapter.hasErrors)
        }

        buildSolutionWithReSharperBuild()
        withOpenedEditor(project, "FSharpProject/Library.fs") {
            waitForDaemon()
            wait(Duration.ofSeconds(15))
            assert(!markupAdapter.hasErrors)
        }

        deleteElement2(project, arrayOf("ProjectReferencesFSharp", "FSharpProject", "Dependencies", ".NETStandard 2.0", "Projects", "CSharpProject/1.0.0"))
        withOpenedEditor(project, "FSharpProject/Library.fs") {
            waitForDaemon()
            assert(markupAdapter.hasErrors)
        }

        addReference2(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        withOpenedEditor(project, "FSharpProject/Library.fs") {
            waitForDaemon()
            assert(!markupAdapter.hasErrors)
        }
    }
}
