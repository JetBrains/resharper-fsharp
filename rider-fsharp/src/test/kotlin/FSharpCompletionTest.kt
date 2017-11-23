import com.intellij.openapi.editor.impl.EditorImpl
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.CompletionTestBase
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.waitAndPump
import com.jetbrains.rider.util.lifetime.Lifetime
import org.testng.annotations.Test

@Test
class FSharpCompletionTest : CompletionTestBase() {
    override fun getSolutionDirectoryName() = "CoreConsoleApp"
    override val restoreNuGetPackages = true

    private val rdFcsHost get() = project.solution.fsharpCompilerServiceHost

    @Test
    fun basicCompletion() {
        doTest {
            typeWithLatency("filt")
            waitForFcs()
            callBasicCompletion()
            waitForCompletion()
            completeWithTab()
        }
    }

    private fun waitForFcs() {
        val lifetimeDefinition = Lifetime.create(Lifetime.Eternal)

        var isFcsReady = false
        rdFcsHost.projectChecked.advise(lifetimeDefinition.lifetime, { project ->
            isFcsReady = true
            frameworkLogger.info("FCS: $project checked")
        })

        waitAndPump(Lifetime.Eternal, { isFcsReady }, 60000)
        lifetimeDefinition.terminate()
    }

    private fun doTest(test: EditorImpl.() -> Unit) {
        val fileName = "Program.fs"

        doTestWithDocuments {
            withCaret(fileName, fileName) {
                test()
            }
        }
    }
}
