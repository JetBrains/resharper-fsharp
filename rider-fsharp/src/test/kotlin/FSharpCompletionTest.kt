import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CompletionTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DEFAULT)
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

    @Test
    @TestEnvironment(solution = "YamlProviderCSharp", toolset = ToolsetVersion.TOOLSET_16)
    fun `provided type abbreviation`() = doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

    @Test
    @TestEnvironment(solution = "YamlProviderCSharp", toolset = ToolsetVersion.TOOLSET_16)
    fun `provided nested type`() = doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

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

    private fun doTestDumpLookupItems(relativePath: String, sourceFileName: String) {
        withOpenedEditor(relativePath, sourceFileName) {
            waitForDaemon()
            callBasicCompletion()
            waitForCompletion()
            executeWithGold(testGoldFile) {
                dumpActiveLookupItemsPresentations(it)
            }
        }
    }
}
