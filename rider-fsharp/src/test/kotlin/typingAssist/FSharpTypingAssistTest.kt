package typingAssist

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.TypingAssistTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.scriptingApi.typeOrCallAction
import org.testng.annotations.DataProvider
import org.testng.annotations.Test

@TestEnvironment(coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class FSharpTypingAssistTest: TypingAssistTestBase() {

    override fun getSolutionDirectoryName(): String = "CoreConsoleApp"

    @DataProvider(name = "testTypingAssists")
    fun testTypingAssists() = arrayOf(
            arrayOf("closingBrace1", "{"),
            arrayOf("parentheses1", "("),
            arrayOf("quotes1", "\""),
            arrayOf("singleQuote1", "'"),
            arrayOf("closingAngleBracket1", "<"),
            arrayOf("indentAfterEnter", "EditorEnter")
    )

    @Test(dataProvider = "testTypingAssists")
    fun testTyping(caseName: String, input: String) {
        dumpOpenedEditor("Program.fs", "Program.fs") {
            typeOrCallAction(input)
        }
    }

}
