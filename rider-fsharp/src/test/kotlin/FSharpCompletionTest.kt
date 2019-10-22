
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CompletionTestBase
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.callBasicCompletion
import com.jetbrains.rider.test.scriptingApi.completeWithTab
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.waitForCompletion
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rd.util.lifetime.Lifetime
import org.testng.annotations.Test

@Test
class FSharpCompletionTest : CompletionTestBase() {
    override fun getSolutionDirectoryName() = "CoreConsoleApp"
    override val restoreNuGetPackages = true

    private val rdFcsHost get() = project.solution.rdFSharpModel.fSharpCompilerServiceHost
    private var isFcsReady = false

    @Test(enabled = false)
    fun namespaceKeyword() = doTest("na")

    @Test(enabled = false) // todo: remove static items in FCS basic completion
    fun listModule() = doTest("Lis")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE)
    fun listModuleValue() = doTest("filt")

    private fun waitForFcs() {
        waitAndPump(Lifetime.Eternal, { isFcsReady }, 60000)
    }

    private fun doTest(typed: String) {
        rdFcsHost.projectChecked.advise(Lifetime.Eternal) { project ->
            isFcsReady = true
            frameworkLogger.info("FCS: $project checked")
        }

        isFcsReady = false
        dumpOpenedEditor("Program.fs", "Program.fs") {
            isFcsReady = false
            typeWithLatency(typed)
            waitForFcs()
            callBasicCompletion()
            waitForCompletion()
            completeWithTab()
        }
    }
}
