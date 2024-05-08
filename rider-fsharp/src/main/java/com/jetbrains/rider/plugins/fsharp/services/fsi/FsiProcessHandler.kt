package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessListener
import com.intellij.execution.process.ProcessOutputTypes
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.rd.util.launchOnUi
import com.intellij.openapi.util.Key
import com.intellij.util.concurrency.ThreadingAssertions
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.channels.consumeEach
import kotlinx.coroutines.channels.trySendBlocking

class FsiProcessHandler(
  parentLifetime: Lifetime,
  private val fsiInputOutputProcessor: FsiInputOutputProcessor,
  process: Process,
  commandLine: String?
) : OSProcessHandler(process, commandLine, Charsets.UTF_8) {

  private val sandboxInfoUpdaters = mutableListOf<FsiSandboxInfoUpdater>()
  private val fsiProcessOutputListeners =
    mutableListOf<FsiSandboxInfoUpdater.FsiSandboxInfoUpdaterProcessOutputListener>()

  private val lifetime = LifetimeDefinition(parentLifetime)

  override fun isSilentlyDestroyOnClose(): Boolean = true

  private val channel = Channel<Pair<String, Key<*>>>(0).also {
    lifetime.onTermination { it.close() }
    lifetime.launchOnUi {
      it.consumeEach { (text, outputType) ->
        if (text != "> ") {
          when (outputType) {
            ProcessOutputTypes.STDOUT -> {
              fsiInputOutputProcessor.printOutputText(text, ConsoleViewContentType.NORMAL_OUTPUT)
            }

            ProcessOutputTypes.STDERR -> {
              fsiInputOutputProcessor.printOutputText(text, ConsoleViewContentType.ERROR_OUTPUT)
            }
          }

          fsiProcessOutputListeners.forEach { it.onTextAvailable(ProcessEvent(this@FsiProcessHandler, text), outputType) }
        } else {
          sandboxInfoUpdaters.forEach { it.onOutputEnd() }
          fsiInputOutputProcessor.onServerPrompt()
        }
      }
    }
  }

  override fun notifyTextAvailable(text: String, outputType: Key<*>) {
    when (outputType) {
      ProcessOutputTypes.STDOUT,
      ProcessOutputTypes.STDERR -> {
        ThreadingAssertions.assertBackgroundThread()
        channel.trySendBlocking(text to outputType)
      }

      else -> super.notifyTextAvailable(text, outputType)
    }
  }

  override fun startNotify() {
    super.startNotify()

    fsiProcessOutputListeners.forEach { it.startNotified(ProcessEvent(this, "")) }
  }

  override fun addProcessListener(listener: ProcessListener) {
    super.addProcessListener(listener)
    if (isStartNotified && process.isAlive)
      listener.startNotified(ProcessEvent(this))
  }

  override fun notifyProcessTerminated(exitCode: Int) {
    super.notifyProcessTerminated(exitCode)
    lifetime.terminate()
    fsiProcessOutputListeners.forEach { it.processTerminated(ProcessEvent(this, "")) }
  }

  fun addSandboxInfoUpdater(sandboxInfoUpdater: FsiSandboxInfoUpdater) {
    fsiProcessOutputListeners.add(sandboxInfoUpdater.fsiProcessOutputListener)

    sandboxInfoUpdaters.add(sandboxInfoUpdater)
  }
}
