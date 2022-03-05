package editor

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CodeFoldingTestBase
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@TestEnvironment(solution = "CoreConsoleApp", toolset = ToolsetVersion.TOOLSET_16_CORE)
class CodeFoldingTest : CodeFoldingTestBase() {

    @Test
    fun codeFolding() {
        doTestWithMarkupModel("CodeFolding.fs", "CodeFolding.fs") {
            waitForDaemon()
            dumpFoldingHighlightersWithText()
        }
    }
}
