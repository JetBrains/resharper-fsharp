package com.jetbrains.rider.plugins.fsharp.logs

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario

object FSharpLogTraceScenarios {
    object FSharpProjectModel : LogTraceScenario(
        "JetBrains.ReSharper.Plugins.FSharp.Checker.FcsCheckerService",
        "JetBrains.ReSharper.Plugins.FSharp.Checker.FcsProjectProvider",
        "JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader.AssemblyReaderShim")

    object FSharpFileSystem : LogTraceScenario(
            "JetBrains.ReSharper.Plugins.FSharp.DelegatingFileSystemShim",
            "JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem.FSharpSourceCache",
            "JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem.AssemblyInfoShim")
}
