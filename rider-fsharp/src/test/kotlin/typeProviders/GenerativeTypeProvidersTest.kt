package typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.buildSelectedProjectsWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import withOutOfProcessTypeProviders

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class GenerativeTypeProvidersTest: BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"

    @Test
    fun `generative type providers cross-project analysis`() {
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, "GenerativeTypeLibrary/Library.fs") {
                waitForDaemon()
                markupAdapter.hasErrors.shouldBeTrue()

                buildSelectedProjectsWithReSharperBuild(listOf("${project!!.solutionDirectoryPath}/GenerativeTypeProvider/GenerativeTypeProvider.fsproj"))

                waitForDaemon()
                typeWithLatency("//")
                waitForDaemon()
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }
    }
}
