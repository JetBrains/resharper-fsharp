package markup

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.plugins.fsharp.test.withCultureInfo
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldContains
import com.jetbrains.rider.test.base.HoverDocTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@TestEnvironment(solution = "CoreConsoleApp", toolset = ToolsetVersion.TOOLSET_16_CORE)
class FSharpHoverDocTest : HoverDocTestBase() {
    @Test
    fun `test hover docs for EntryPoint`() = doTest("Program.fs", "Program.fs")

    @Test
    fun `test hover docs for a function definition`() = doTest("Program.fs", "Program.fs")

    @Test
    fun `test hover docs for a parameter`() = doTest("Program.fs", "Program.fs")

    @Test
    fun `test hover docs for a submodule`() = doTest("Program.fs", "Program.fs")

    @Test
    fun `test xml doc with symbol reference`() = doTest("Program.fs", "Program.fs")

    @Test
    fun `test empty xml doc`() = doTest("Program.fs", "Program.fs")

    @Test
    @TestEnvironment(
        solution = "SwaggerProviderCSharp",
        toolset = ToolsetVersion.TOOLSET_17_CORE,
        coreVersion = CoreVersion.DOT_NET_6
    )
    fun `provided method in csharp`() = doTestWithTypeProviders("get all courses")

    @Test
    @TestEnvironment(
        solution = "SwaggerProviderCSharp",
        toolset = ToolsetVersion.TOOLSET_17_CORE,
        coreVersion = CoreVersion.DOT_NET_6
    )
    fun `provided abbreviation in csharp`() = doTestWithTypeProviders("OpenAPI Provider for")

    @Test
    fun `test xml doc parsing error`() {
        withCultureInfo(project, "en-US") {
            doTest("Program.fs", "Program.fs")
        }
    }

    private fun doTestWithTypeProviders(summary: String){
        doTestWithMarkupModel("CSharpLibrary.cs", "CSharpLibrary.cs") {
            waitForDaemon()
            generateBackendHoverDoc().shouldContains(summary)
        }
    }
}
