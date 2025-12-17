package com.jetbrains.rider.plugins.fsharp.test.cases.debugger

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.DebuggerTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpFullCurrentData
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.stepInto
import com.jetbrains.rider.test.scriptingApi.stepOver
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import org.testng.annotations.Test

@Test
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SteppingTests")
class FSharpSteppingTest : DebuggerTestBase() {
    override val projectName = "SteppingTests"

    @Test
    fun testHiddenSequencePointInConstructor() {
        testDebugProgram({
            toggleBreakpoint("Program.fs", 5)
        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped at 'let")

            stepInto()
            dumpFullCurrentData(message = "Stepped into T constructor")

            stepOver()
            dumpFullCurrentData(message = "Stepped over ()")

            stepOver()
            dumpFullCurrentData(message = "Stepped out")

            resumeSession()
        }, true)
    }
}
