package com.jetbrains.rider.plugins.fsharp.test.cases.debugger

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.DebuggerTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpFullCurrentData
import com.jetbrains.rider.test.scriptingApi.initSmartStepInto
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import org.testng.annotations.Test

@Test
@Mute("TeamCity config needs merging first")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SmartStepIntoTest")
class FSharpSmartStepIntoTest : DebuggerTestBase() {
    override val projectName = "SmartStepIntoTest"

    @Test
    fun testNestedCalls() {
        testDebugProgram({
            toggleBreakpoint(project, "NestedCalls.fs", 37)
            toggleBreakpoint(project, "NestedCalls.fs", 38)
        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped on g1 call")
            initSmartStepInto(1, 0, 3, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into Prop2")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped on g2 call")
            initSmartStepInto(3, 0, 4, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into g2")
            resumeSession()
        })
    }

    @Test
    fun testInline() {
        testDebugProgram({
            toggleBreakpoint(project, "Inline.fs", 11)
        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped on not call")
            initSmartStepInto(2, 0, 3, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into eq")
            resumeSession()
        })
    }
}

