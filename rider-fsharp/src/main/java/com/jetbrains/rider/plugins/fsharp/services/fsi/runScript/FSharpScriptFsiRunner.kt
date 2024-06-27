package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.configurations.RunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultRunExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.showRunContent
import com.intellij.execution.ui.RunContentDescriptor
import com.intellij.openapi.application.EDT
import com.jetbrains.rider.debugger.DotNetProgramRunner
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class FSharpScriptFsiRunner : DotNetProgramRunner() {
  override fun canRun(executorId: String, runConfiguration: RunProfile) =
    executorId == DefaultRunExecutor.EXECUTOR_ID && runConfiguration is FSharpScriptConfiguration

  override suspend fun executeAsync(environment: ExecutionEnvironment, state: RunProfileState): RunContentDescriptor? {
    if (state !is FSharpScriptRunProfileState) return null
    val result = withContext(Dispatchers.EDT) {
      state.executeSuspending(environment.executor)
    }
    return showRunContent(result, environment)
  }
}
