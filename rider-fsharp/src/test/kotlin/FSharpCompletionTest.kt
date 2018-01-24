import com.intellij.openapi.editor.impl.EditorImpl
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

    @Test
    fun basicCompletion() = doTest("Program.fs", "filt")

    @Test
    fun namespaceKeyword() = doTest("Empty.fs", "na")


    private fun waitForFcs() {
        waitAndPump(Lifetime.Eternal, { isFcsReady }, 60000)
    }

    private fun doTest(fileName: String, typed: String) {
        rdFcsHost.projectChecked.advise(Lifetime.Eternal, { project ->
            isFcsReady = true
            frameworkLogger.info("FCS: $project checked")
        })

        isFcsReady = false
        doTestWithDocuments {
            withCaret(fileName, fileName) {
                isFcsReady = false
                typeWithLatency(typed)
                waitForFcs()
                callBasicCompletion()
                waitForCompletion()
                completeWithTab()
            }
        }
    }
}
