package fantomas

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.plugins.fsharp.test.withEditorConfig
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
@TestEnvironment(coreVersion = CoreVersion.DOT_NET_6)
class FantomasTest : EditorTestBase() {
    override fun getSolutionDirectoryName() = "FormatCodeApp"

    @Test
    fun withEditorConfig() = doTest("EditorConfig.fs")

    @Test
    fun simpleFormatting() = doTest("Simple.fs")

    @Test
    fun formatLastFile() = doTest("Program.fs")

    private fun doTest(fileName: String) {
        withEditorConfig(project) {
            withOpenedEditor(fileName) {
                waitForDaemon()
                reformatCode()
                executeWithGold(testGoldFile) {
                    dumpOpenedDocument(it, project!!, false)
                }
            }
        }
    }
}
