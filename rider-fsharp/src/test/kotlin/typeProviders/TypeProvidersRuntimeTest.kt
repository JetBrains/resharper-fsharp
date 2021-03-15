package typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import withTypeProviders

@Test
class TypeProvidersRuntimeTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "CoreTypeProviderLibrary"
    override val restoreNuGetPackages = true

    private val rdFcsHost get() = project.solution.rdFSharpModel.fsharpTestHost

    @Test
    @TestEnvironment(
        toolset = ToolsetVersion.TOOLSET_16,
        coreVersion = CoreVersion.DOT_NET_CORE_3_1,
        solution = "TypeProviderLibrary")
    fun framework461() = doTest(".NET Framework 4.8")

    @Test(enabled = false)
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_CORE_2_1)
    fun core21() = doTest(".NET Core 3.1")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
    fun core31() = doTest(".NET Core 3.1")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_5)
    fun net5() = doTest(".NET 5")

    @Test(enabled = false)
    @TestEnvironment(
        toolset = ToolsetVersion.TOOLSET_16_CORE,
        coreVersion = CoreVersion.DOT_NET_CORE_3_1,
        solution = "FscTypeProviderLibrary"
    )
    fun fsc() = doTest(".NET Framework 4.8")

    private fun doTest(expectedRuntime: String) {
        withTypeProviders {
            withOpenedEditor(project, "TypeProviderLibrary/Library.fs") {
                waitForDaemon()
                rdFcsHost.typeProvidersRuntimeVersion.sync(Unit)
                    .shouldNotBeNull()
                    .startsWith(expectedRuntime)
                    .shouldBeTrue()
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }
    }
}
