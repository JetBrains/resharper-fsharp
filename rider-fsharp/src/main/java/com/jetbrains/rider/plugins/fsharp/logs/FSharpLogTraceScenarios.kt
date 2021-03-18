package com.jetbrains.rider.plugins.fsharp.logs

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario

object FSharpLogTraceScenarios {
    object FSharpFcsReactorMonitor : LogTraceScenario("JetBrains.ReSharper.Plugins.FSharp.FcsReactorMonitor")
    object FSharpFcsProjectProvider : LogTraceScenario("JetBrains.ReSharper.Plugins.FSharp.Checker.FcsProjectProvider")

    object FSharpFileSystemShim : LogTraceScenario(
            "JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem.FSharpSourceCache",
            "JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem.AssemblyInfoShim",
            "JetBrains.ReSharper.Plugins.FSharp.DelegatingFileSystemShim")
}
