package typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.plugins.fsharp.test.withOutOfProcessTypeProviders
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeNull
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersSettingTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"
    override val restoreNuGetPackages = true
    private val rdFcsHost get() = project.solution.rdFSharpModel.fsharpTestHost

    @Test
    fun enableTypeProvidersSetting() {
        val sourceFile = "TypeProviderLibrary2/Library.fs"

        withOutOfProcessTypeProviders {
            withOpenedEditor(project, sourceFile) {
                waitForDaemon()
                rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldNotBeNull()
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }

        unloadAllProjects()
        reloadAllProjects(project)

        withOpenedEditor(project, sourceFile) {
            waitForDaemon()
            rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldBeNull()
            markupAdapter.hasErrors.shouldBeFalse()
        }

        unloadAllProjects()
        reloadAllProjects(project)

        withOutOfProcessTypeProviders {
            withOpenedEditor(project, sourceFile) {
                waitForDaemon()
                rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldNotBeNull()
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }
    }
}
