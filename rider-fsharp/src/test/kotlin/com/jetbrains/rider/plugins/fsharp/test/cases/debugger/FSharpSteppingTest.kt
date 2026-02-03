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

    @Test
    fun testStepIntoFunction() {
        testDebugProgram({
            toggleBreakpoint("FunctionsModule.fs", 16)
            toggleBreakpoint("FunctionsModule.fs", 17)
            toggleBreakpoint("FunctionsModule.fs", 18)
            toggleBreakpoint("FunctionsModule.fs", 19)
            toggleBreakpoint("FunctionsLocal.fs", 16)
            toggleBreakpoint("FunctionsLocal.fs", 17)
            toggleBreakpoint("FunctionsLocal.fs", 18)
            toggleBreakpoint("FunctionsLocal.fs", 19)

        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped at module f1 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into module f1")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at module f2 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into module f2")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at module f3 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into module f3")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at module f4 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into module f4")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at local f1 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into local f1")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at local f2 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into local f2")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at local f3 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into local f3")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at local f4 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into local f4")

            resumeSession()
        }, true)
    }

    @Test
    fun testTuples() {
        testDebugProgram({
            toggleBreakpoint("Tuples.fs", 13)
            toggleBreakpoint("Tuples.fs", 14)
            toggleBreakpoint("Tuples.fs", 15)
            toggleBreakpoint("Tuples.fs", 16)
        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped at f1 with existing tuple")

            stepInto()
            dumpFullCurrentData(message = "Stepped into f1")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at f1 with new tuple")

            stepInto()
            dumpFullCurrentData(message = "Stepped into f1")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at f2 with existing tuple")

            stepInto()
            dumpFullCurrentData(message = "Stepped into f2")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at f2 with new tuple")

            stepInto()
            dumpFullCurrentData(message = "Stepped into f2")
            resumeSession()
        }, true)
    }

    @Test
    fun testStepOutInitClass() {
        testDebugProgram({
            toggleBreakpoint("StepOutInitClass.fs", 12)
        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped at Prop1 call")

            stepInto()
            dumpFullCurrentData(message = "Stepped into Prop1")
            resumeSession()
        }, true)
    }

    @Test
    fun testPipes() {
        testDebugProgram({
            toggleBreakpoint("Pipes.fs", 4)
            toggleBreakpoint("Pipes.fs", 5)
            toggleBreakpoint("Pipes.fs", 7)
        }, {
            waitForPause()
            dumpFullCurrentData(message = "Stopped at the first empty list")
            stepOver()
            dumpFullCurrentData(message = "Stepped to ignore")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at the second empty list")
            stepOver()
            dumpFullCurrentData(message = "Stepped to List.map")
            stepOver()
            dumpFullCurrentData(message = "Stepped to ignore")
            resumeSession()

            waitForPause()
            dumpFullCurrentData(message = "Stopped at the second empty list")
            stepOver()
            dumpFullCurrentData(message = "Stepped to List.map")
            stepOver()
            dumpFullCurrentData(message = "Stepped to ignore")
            resumeSession()
        }, true)
    }

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testAsync() {
        testDebugProgram({
            toggleBreakpoint("Async.fs", 27)
            toggleBreakpoint("Async.fs", 33)
            toggleBreakpoint("Async.fs", 39)
            toggleBreakpoint("Async.fs", 46)
            toggleBreakpoint("Async.fs", 56)
        }, {
            fun dumpState(message: String? = null) {
                dumpExecutionPoint(message = message)
                stream.println()
            }

            fun repeatStepOver(count: Int) {
                repeat(count, {
                    stepOver()
                    dumpState(message = "Stepped over")
                })
            }

            waitForPause()
            dumpState(message = "Stopped inside a2")
            stepOver()
            dumpState(message = "Stepped over")
            stepInto()
            dumpState(message = "Stepped into Prop")
            resumeSession()

            waitForPause()
            dumpState(message = "Stopped inside a3")
            stepOver()
            dumpState(message = "Stepped over")
            stepInto()
            dumpState(message = "Stepped into Prop")
            resumeSession()

            waitForPause()
            dumpState(message = "Stopped inside a4")
            repeatStepOver(2)
            resumeSession()

            waitForPause()
            dumpState(message = "Stopped inside a5")
            repeatStepOver(3)
            resumeSession()

            waitForPause()
            dumpState(message = "Stopped inside a6")
            repeatStepOver(4)
            resumeSession()
        }, true)
    }
}
