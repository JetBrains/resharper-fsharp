package com.jetbrains.rider.plugins.fsharp.test.cases.debugger

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.DebuggerTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test

@Test
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

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testAsync() {
        testDebugProgram({
            toggleBreakpoint(project, "Async.fs", 22)
            toggleBreakpoint(project, "Async.fs", 28)
            toggleBreakpoint(project, "Async.fs", 39)
        }, {
            waitForPause()
            dumpExecutionPoint(message = "Stopped inside a1")
            initSmartStepInto(1, 0, 4, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into f1")
            resumeSession()

            waitForPause()
            dumpExecutionPoint(message = "Stopped inside a2")
            initSmartStepInto(0, 0, 4, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into T.Prop")
            resumeSession()

            waitForPause()
            dumpExecutionPoint(message = "Stopped inside a4")
            initSmartStepInto(1, 0, 5, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into incrementAsync")
            resumeSession()
        })
    }

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testTask() {
        testDebugProgram({
            toggleBreakpoint(project, "Task.fs", 22)
            toggleBreakpoint(project, "Task.fs", 28)
            toggleBreakpoint(project, "Task.fs", 39)
        }, {
            waitForPause()
            dumpExecutionPoint(message = "Stopped inside a1")
            initSmartStepInto(1, 0, 2, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into f1")
            resumeSession()

            waitForPause()
            dumpExecutionPoint(message = "Stopped inside a2")
            initSmartStepInto(0, 0, 2, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into T.Prop")
            resumeSession()

            waitForPause()
            dumpExecutionPoint(message = "Stopped inside a4")
            initSmartStepInto(1, 0, 2, session)
            waitForPause()
            dumpFullCurrentData(message = "Stepped into incrementAsync")
            resumeSession()
        })
    }

}

