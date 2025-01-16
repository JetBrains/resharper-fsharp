package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.framework.withDisabledOutOfProcessTypeProviders
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeNull
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Solution("TypeProviderLibrary")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL)
class TypeProvidersSettingTest : BaseTypeProvidersTest() {
  @Test
  fun disabledTypeProvidersSetting() {
    withDisabledOutOfProcessTypeProviders {
      withOpenedEditor("TypeProviderLibrary2/Library.fs") {
        waitForDaemon()
        rdFcsHost.typeProvidersRuntimeVersion.sync(Unit).shouldBeNull()
        markupAdapter.hasErrors.shouldBeFalse()
      }
    }
  }
}
