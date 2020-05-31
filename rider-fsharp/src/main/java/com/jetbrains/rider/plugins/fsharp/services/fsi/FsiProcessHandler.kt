package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessOutputTypes
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.util.Key

class FsiProcessHandler (
        private val fsiInputOutputProcessor: FsiInputOutputProcessor,
        process: Process,
        commandLine: String?) : OSProcessHandler(process, commandLine, Charsets.UTF_8) {

    private val sandboxInfoUpdaters = mutableListOf<FsiSandboxInfoUpdater>()
    private val fsiProcessOutputListeners = mutableListOf<FsiSandboxInfoUpdater.FsiSandboxInfoUpdaterProcessOutputListener>()

    override fun isSilentlyDestroyOnClose(): Boolean = true

    override fun notifyTextAvailable(text: String, outputType: Key<*>) {
        if (text != "SERVER-PROMPT>\n") {
            when (outputType) {
                ProcessOutputTypes.STDOUT -> {
                    fsiInputOutputProcessor.printOutputText(text, ConsoleViewContentType.NORMAL_OUTPUT)
                }
                ProcessOutputTypes.STDERR -> {
                    fsiInputOutputProcessor.printOutputText(text, ConsoleViewContentType.ERROR_OUTPUT)
                }
                else -> {
                    super.notifyTextAvailable(text, outputType)
                }
            }

            fsiProcessOutputListeners.forEach { it.onTextAvailable(ProcessEvent(this, text), outputType) }
        }
        else {
            sandboxInfoUpdaters.forEach { it.onOutputEnd() }

            fsiInputOutputProcessor.onServerPrompt()
        }
    }

    override fun startNotify() {
        super.startNotify()

        fsiProcessOutputListeners.forEach { it.startNotified(ProcessEvent(this,  "")) }
    }

    override fun notifyProcessTerminated(exitCode: Int) {
        super.notifyProcessTerminated(exitCode)

        fsiProcessOutputListeners.forEach { it.processTerminated(ProcessEvent(this, "")) }
    }

    fun addSandboxInfoUpdater (sandboxInfoUpdater: FsiSandboxInfoUpdater) {
        fsiProcessOutputListeners.add(sandboxInfoUpdater.fsiProcessOutputListener)

        sandboxInfoUpdaters.add(sandboxInfoUpdater)
    }
}
