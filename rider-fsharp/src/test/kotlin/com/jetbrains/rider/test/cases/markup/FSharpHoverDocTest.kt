package com.jetbrains.rider.test.cases.markup

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.HoverDocTestBase
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test
import com.jetbrains.rider.test.framework.withCultureInfo

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
    fun `test xml doc parsing error`() {
        withCultureInfo(project, "en-US") {
            doTest("Program.fs", "Program.fs")
        }
    }
}
