package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
 import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DEFAULT)
class ReferencesOrderTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "ReferencesOrder"

    override val waitForCaches = true
    override val restoreNuGetPackages = true

    @Test()
    fun testReferencesOrder() {
        val references = project.fcsHost.dumpSingleProjectLocalReferences.sync(Unit)
        assert(references == listOf("Library1.dll", "Library2.dll"))
    }
}
