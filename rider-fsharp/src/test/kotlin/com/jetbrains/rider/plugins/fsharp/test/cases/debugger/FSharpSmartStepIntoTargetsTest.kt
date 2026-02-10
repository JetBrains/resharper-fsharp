package com.jetbrains.rider.plugins.fsharp.test.cases.debugger

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.DebuggerTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.collectSmartStepIntoTargets
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import org.testng.annotations.Test

@Test
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SmartStepIntoTest")
class FSharpSmartStepIntoTargetsTest : DebuggerTestBase() {
    override val projectName = "SmartStepIntoTest"

    private fun testBreakpoints(fileName: String, lineNumbers: List<Int>, withOffsets: Boolean = true) {
        testDebugProgram({
            lineNumbers.forEach {
                toggleBreakpoint(fileName, it)
            }
        }, {
            repeat(lineNumbers.size) {
                waitForPause()
                dumpExecutionPoint(withOffsets = withOffsets)
                collectSmartStepIntoTargets(this)
                resumeSession()
            }
        })
    }

    @Test
    fun testNestedCalls() {
        testBreakpoints("NestedCalls.fs", listOf(21, 22, 23, 24, 25, 26, 27, 29, 37, 38))
    }

    @Test
    fun testChainedCalls() {
        testBreakpoints("ChainedCalls.fs", listOf(22, 23, 25, 26, 27))
    }

    @Test
    fun testInline() {
        testBreakpoints("Inline.fs", listOf(11))
    }

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testAsync() {
        testBreakpoints("Async.fs", listOf(22, 28, 34, 39, 41, 51, 61, 66, 72), false)
    }

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testTask() {
        testBreakpoints("Task.fs", listOf(22, 28, 34, 39, 41, 51, 61, 66, 72), false)
    }

}
