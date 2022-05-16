package typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rdclient.testFramework.waitForNextDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.time.Duration

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class GenerativeTypeProvidersTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"

    @Test
    fun `generative type providers cross-project analysis`() {
        val generativeProviderProjectPath = "${project.solutionDirectoryPath}/GenerativeTypeProvider/GenerativeTypeProvider.fsproj"

        withOpenedEditor(project, "GenerativeTypeLibrary/Library.fs") {
            waitForDaemon()
            markupAdapter.hasErrors.shouldBeTrue()

            buildSelectedProjectsWithConsoleBuild(listOf(generativeProviderProjectPath))

            waitForNextDaemon(Duration.ofSeconds(5))
            markupAdapter.hasErrors.shouldBeFalse()
        }

        unloadProject(arrayOf("TypeProviderLibrary", "GenerativeTypeProvider"))
        reloadProject(arrayOf("TypeProviderLibrary", "GenerativeTypeProvider"))

        withOpenedEditor(project, "GenerativeTypeLibrary/Library.fs") {
            waitForDaemon()
            markupAdapter.hasErrors.shouldBeFalse()
        }
    }
}
