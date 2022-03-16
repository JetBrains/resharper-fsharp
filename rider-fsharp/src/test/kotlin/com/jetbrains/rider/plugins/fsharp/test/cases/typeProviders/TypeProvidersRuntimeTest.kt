package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.framework.withOutOfProcessTypeProviders
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.*
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
class TypeProvidersRuntimeTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "CoreTypeProviderLibrary"
    override val restoreNuGetPackages = true

    @Test
    @TestEnvironment(
        toolset = ToolsetVersion.TOOLSET_16,
        coreVersion = CoreVersion.DOT_NET_CORE_3_1,
        solution = "TypeProviderLibrary")
    fun framework461() = doTest(".NET Framework 4.8")
    
    @Test(enabled = false)
    @TestEnvironment(
        toolset = ToolsetVersion.TOOLSET_16_CORE, 
        coreVersion = CoreVersion.DOT_NET_CORE_2_1,
        additionalCoreVersions = [CoreVersion.DOT_NET_CORE_3_1]
    )
    fun core21() = doTest(".NET Core 3.1")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
    fun core31() = doTest(".NET Core 3.1")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_5)
    fun net5() = doTest(".NET 5")

    @Test
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6)
    fun net6() = doTest(".NET 6")

    @Test(enabled = false)
    @TestEnvironment(
        toolset = ToolsetVersion.TOOLSET_16_CORE,
        coreVersion = CoreVersion.DOT_NET_CORE_3_1,
        solution = "FscTypeProviderLibrary"
    )
    fun fsc() = doTest(".NET Framework 4.8")

    private fun doTest(expectedRuntime: String) {
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, "TypeProviderLibrary/Library.fs") {
                waitForDaemon()
                val typeProvidersRuntimeVersion = this.project!!.fcsHost
                    .typeProvidersRuntimeVersion.sync(Unit)
                    .shouldNotBeNull()
                typeProvidersRuntimeVersion
                    .startsWith(expectedRuntime)
                    .shouldBeTrue("Expected runtime starts with: $expectedRuntime, actual: $typeProvidersRuntimeVersion")
                markupAdapter.hasErrors.shouldBeFalse()
            }
        }
    }
}
