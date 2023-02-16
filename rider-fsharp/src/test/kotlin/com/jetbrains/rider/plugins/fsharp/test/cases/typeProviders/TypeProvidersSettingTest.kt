package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.plugins.fsharp.test.withDisabledOutOfProcessTypeProviders
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeNull
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersSettingTest : BaseTestWithSolution() {
  override fun getSolutionDirectoryName() = "TypeProviderLibrary"
  override val restoreNuGetPackages = true
  private val rdFcsHost get() = project.solution.rdFSharpModel.fsharpTestHost

  @Test
  fun disabledTypeProvidersSetting() {
    withDisabledOutOfProcessTypeProviders {
      withOpenedEditor(project, "TypeProviderLibrary2/Library.fs") {
        waitForDaemon()
        rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldBeNull()
        markupAdapter.hasErrors.shouldBeFalse()
      }
    }
  }
}