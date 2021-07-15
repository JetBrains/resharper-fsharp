package typeProviders

import com.intellij.execution.configurations.GeneralCommandLine
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.NetCoreRuntime
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.*
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import withTypeProviders
import java.time.Duration

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersActionsTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"
    override val restoreNuGetPackages = true
    private val sourceFile = "TypeProviderLibrary/Caches.fs"
    private val rdFcsHost get() = project.solution.rdFSharpModel.fsharpTestHost
    private val restartTypeProvidersAction = "Rider.Plugins.FSharp.RestartTypeProviders"

    private fun waitForTypeProviders() {
        waitAndPump(project.lifetime, { rdFcsHost.typeProvidersRuntimeVersion.sync(Unit) != null }, Duration.ofSeconds(60000))
    }

    @Test
    fun restartTypeProviders() {
        withTypeProviders {
            withOpenedEditor(project, sourceFile) {
                waitForDaemon()
                rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldNotBeNull()
                markupAdapter.hasErrors.shouldBeFalse()

                rdFcsHost.killTypeProvidersProcess.sync(Unit)
                rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldBeNull()

                callAction(restartTypeProvidersAction)
                waitForTypeProviders()
                waitForDaemon()
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }
    }

    @Test
    @TestEnvironment(solution = "LemonadeProvider")
    fun rebuildTypeProvider() {
        val lemonadeProviderProject = "${project.solutionDirectoryPath}/LemonadeProvider.DesignTime/LemonadeProvider.DesignTime.fsproj"
        val sourceFileToCheck = "LemonadeProviderConsumer/Library.fs"

        withTypeProviders(true) {

            GeneralCommandLine()
                .withWorkDirectory(project.solutionDirectoryPath.toString())
                .withExePath(NetCoreRuntime.cliPath.value)
                .withParameters("tool", "restore")
                .createProcess()
                .waitFor()
                .shouldBe(0)

            buildSelectedProjectsWithReSharperBuild(listOf(lemonadeProviderProject), ignoreReferencesResolve = true)

            withOpenedEditor(project, sourceFileToCheck) {
                waitForDaemon()
                rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldNotBeNull()
                markupAdapter.hasErrors.shouldBeFalse()
            }

            withOpenedEditor(project, "LemonadeProvider.DesignTime/LemonadeProvider.DesignTime.fs") {
                //change "Drink" -> "Drink1"
                typeFromOffset("1", 496)
            }

            buildSelectedProjectsWithReSharperBuild(project, listOf(lemonadeProviderProject))

            withOpenedEditor(project, sourceFileToCheck) {
                callAction(restartTypeProvidersAction)
                waitForTypeProviders()
                waitForDaemon()
                markupAdapter.hasErrors.shouldBeTrue()

                //change "Drink" -> "Drink1"
                typeFromOffset("1", 86)
                waitForDaemon()
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }
    }
}
