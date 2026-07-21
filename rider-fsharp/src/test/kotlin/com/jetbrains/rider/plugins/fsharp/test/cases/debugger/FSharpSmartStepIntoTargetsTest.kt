package com.jetbrains.rider.plugins.fsharp.test.cases.debugger

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.junit5.base.debugger.DebuggerTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SmartStepIntoTest")
class FSharpSmartStepIntoTargetsTest : DebuggerTestBase() {
    override val projectName = "SmartStepIntoTest"

    @Test
    fun testNestedCalls() {
        testSmartStepIntoTargets("NestedCalls.fs", listOf(21, 22, 23, 24, 25, 26, 27, 29, 37, 38))
    }

    @Test
    fun testChainedCalls() {
        testSmartStepIntoTargets("ChainedCalls.fs", listOf(22, 23, 25, 26, 27))
    }

    @Test
    fun testControlFlow() {
        testSmartStepIntoTargets("ControlFlow.fs", listOf(8, 11, 15, 18))
    }

    @Test
    fun testUnionCases() {
        testSmartStepIntoTargets("UnionCases.fs", listOf(13, 14, 15))
    }

    @Test
    fun testValues() {
        testSmartStepIntoTargets("Values.fs", listOf(10, 11))
    }

    @Test
    fun testInline() {
        testSmartStepIntoTargets("Inline.fs", listOf(11))
    }

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testAsync() {
        testSmartStepIntoTargets("Async.fs", listOf(22, 28, 34, 39, 41, 51, 61, 66, 72), false)
    }

    @Test
    @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
    @Solution("CeSteppingTests")
    fun testTask() {
        testSmartStepIntoTargets("Task.fs", listOf(22, 28, 34, 39, 41, 51, 61, 66, 72), false)
    }

}
