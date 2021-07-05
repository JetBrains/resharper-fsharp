
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CompletionTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.callBasicCompletion
import com.jetbrains.rider.test.scriptingApi.completeWithTab
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.waitForCompletion
import org.testng.annotations.Test

@Test
@TestEnvironment(coreVersion = CoreVersion.DEFAULT)
class FSharpCompletionTest : CompletionTestBase() {
    override fun getSolutionDirectoryName() = "CoreConsoleApp"
    override val restoreNuGetPackages = true

    @Test(enabled = false)
    fun namespaceKeyword() = doTest("na")

    @Test(enabled = false) // todo: remove static items in FCS basic completion
    fun listModule() = doTest("Lis")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE)
    fun listModuleValue() = doTest("filt")

    private fun doTest(typed: String) {
        dumpOpenedEditor("Program.fs", "Program.fs") {
            waitForDaemon()
            typeWithLatency(typed)
            callBasicCompletion()
            waitForCompletion()
            completeWithTab()
        }
    }
}
