package com.jetbrains.rider.plugins.fsharp.logs

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario

object FSharpLogTraceScenarios {
    object FcsReactorMonitor : LogTraceScenario("JetBrains.ReSharper.Plugins.FSharp.FcsReactorMonitor")
    object FcsProjectProvider : LogTraceScenario("JetBrains.ReSharper.Plugins.FSharp.Checker.FcsProjectProvider")
}
