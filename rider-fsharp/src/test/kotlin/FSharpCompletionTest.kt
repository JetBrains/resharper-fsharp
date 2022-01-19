import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CompletionTestBase
import com.jetbrains.rider.test.enums.CoreVersion
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

    @Test
    fun namespaceKeyword() = doTestTyping("names")

    @Test
    fun listModule() = doTestChooseItem("List")

    @Test
    fun listModuleValue() = doTestTyping("filt")

    @Test(enabled = false)
    fun localVal01() = doTestChooseItem("x")

    @Test
    fun localVal02() = doTestTyping("x")

    @Test
    fun qualified01() = doTestChooseItem("a")

    @Test
    fun qualified02() = doTestChooseItem("a")

    private fun doTestTyping(typed: String) {
        dumpOpenedEditor("Program.fs", "Program.fs") {
            waitForDaemon()
            typeWithLatency(typed)
            callBasicCompletion()
            waitForCompletion()
            completeWithTab()
        }
    }

    private fun doTestChooseItem(item: String) {
        dumpOpenedEditor("Program.fs", "Program.fs") {
            waitForDaemon()
            callBasicCompletion()
            waitForCompletion()
            completeWithTab(item)
        }
    }
}
