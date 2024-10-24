package com.jetbrains.rider.plugins.fsharp.logs

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario

class FSharpLogTraceScenarios {
  object FSharpProjectModel : LogTraceScenario(
    "JetBrains.ReSharper.Plugins.FSharp.Checker.FcsCheckerService",
    "JetBrains.ReSharper.Plugins.FSharp.Checker.FcsProjectProvider",
  )

  object FSharpFcsRequests : LogTraceScenario(
    "JetBrains.ReSharper.Plugins.FSharp.FSharpAsyncUtil",
    "JetBrains.ReSharper.Plugins.FSharp.FSharpReadLockRequestsQueue",
  )

  object FSharpInteropMetadata : LogTraceScenario(
    "JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader.AssemblyReaderShim",
    "JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader.ProjectFcsModuleReader"
  )

  object FSharpFileSystem : LogTraceScenario(
    "JetBrains.ReSharper.Plugins.FSharp.DelegatingFileSystemShim",
    "JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem.FSharpSourceCache",
    "JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem.AssemblyInfoShim"
  )

  object FSharpTypeProviders : LogTraceScenario(
    "JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host"
  )
}
