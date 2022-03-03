package com.jetbrains.rider.plugins.fsharp.test.cases.templates.core

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(coreVersion = CoreVersion.DOT_NET_CORE_3_1, toolset = ToolsetVersion.TOOLSET_16_CORE)
class FSharpTemplatesTestCore3 : FSharpTemplatesTestCore() {
    fun classLibCoreTemplate() = classLibCoreTemplate(
        CoreTemplateTestArgs(expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0)
    )

    fun classLibNetCoreAppTemplate() = classLibNetCoreAppTemplate(
        CoreTemplateTestArgs(expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0,
            targetFramework = "netcoreapp3.1")
    )

    fun consoleAppCoreTemplate() = consoleAppCoreTemplate(
        CoreTemplateTestArgs(expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0,
            expectedOutput = "Hello World from F#!", breakpointFile = "Program.fs", breakpointLine = 7)
    )

    fun xUnitCoreTemplate() = xUnitCoreTemplate(
        CoreTemplateTestArgs(
            expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0,
            breakpointFile = "Tests.fs", breakpointLine = 8
        )
    )
}
