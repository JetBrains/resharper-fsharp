package com.jetbrains.rider.plugins.fsharp.test.cases

import com.jetbrains.rider.test.framework.testData.IRiderTestDataMarker
import java.io.File

@Suppress("unused")
object RiderTestDataMarker : IRiderTestDataMarker {
  override val testDataFromRoot: File
    get() = File("src/test/testData")

  override val pluginDirectoryInUltimate: File
    get() = File("dotnet/Plugins/resharper-fsharp/rider-fsharp")
}
