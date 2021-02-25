package markup

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.HoverDocTestBase
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
}