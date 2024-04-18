package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.CantRunException
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultRunExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.diagnostic.thisLogger
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.fsharp.services.fsi.getFsiRunOptions
import com.jetbrains.rider.run.RiderRunBundle
import com.jetbrains.rider.run.configurations.AsyncExecutorFactory

class FSharpScriptExecutorFactory : AsyncExecutorFactory {
  override suspend fun create(executorId: String, environment: ExecutionEnvironment, lifetime: Lifetime): RunProfileState {
    val project = environment.project
    val configuration = environment.runProfile as FSharpScriptConfiguration
    val (dotNetExecutable, runtimeToExecute) = getFsiRunOptions(project, configuration)

    thisLogger().info("Configuration will be executed on ${runtimeToExecute.javaClass.name}")
    return when (executorId) {
      DefaultRunExecutor.EXECUTOR_ID -> FSharpScriptRunProfileState(dotNetExecutable, runtimeToExecute, environment)
      else -> throw CantRunException(RiderRunBundle.message("dialog.message.unsupported.executor.error", executorId))
    }
  }
}
