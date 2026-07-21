package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.framework.withDisabledOutOfProcessTypeProviders
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeNull
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.Mono
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Tag(TeamCityTags.Plugins.FSharp)
@Solution("TypeProviderLibrary")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL, mono = Mono.UNIX_ONLY)
class TypeProvidersSettingTest : BaseTypeProvidersTest() {
  @Mute("RIDER-139900")
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
