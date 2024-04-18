package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.configurations.RunProfile
import com.intellij.execution.executors.DefaultRunExecutor
import com.jetbrains.rider.debugger.DotNetProgramRunner

class FSharpScriptFsiRunner : DotNetProgramRunner() {
  override fun canRun(executorId: String, runConfiguration: RunProfile) =
    executorId == DefaultRunExecutor.EXECUTOR_ID && runConfiguration is FSharpScriptConfiguration
}
