import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.CompletionTestBase
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.callBasicCompletion
import com.jetbrains.rider.test.scriptingApi.completeWithTab
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.waitForCompletion
import com.jetbrains.rider.util.idea.waitAndPump
import com.jetbrains.rider.util.lifetime.Lifetime
import org.testng.annotations.Test

@Test
class FSharpCompletionTest : CompletionTestBase() {
    override fun getSolutionDirectoryName() = "CoreConsoleApp"
    override val restoreNuGetPackages = true

    private val rdFcsHost get() = project.solution.fsharpCompilerServiceHost
    private var isFcsReady = false

    @Test(enabled = false)
    fun namespaceKeyword() = doTest("na")

    @Test(enabled = false) // todo: remove static items in FCS basic completion
    fun listModule() = doTest("Lis")

    @Test
    fun listModuleValue() = doTest("filt")

    private fun waitForFcs() {
        waitAndPump(Lifetime.Eternal, { isFcsReady }, 60000)
    }

    private fun doTest(typed: String) {
        rdFcsHost.projectChecked.advise(Lifetime.Eternal, { project ->
            isFcsReady = true
            frameworkLogger.info("FCS: $project checked")
        })

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
