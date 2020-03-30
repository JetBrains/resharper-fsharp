package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.process.OSProcessHandler
import com.intellij.openapi.util.Key
import java.nio.charset.Charset

class FsiProcessHandler (
        process: Process, commandLine: String?) : OSProcessHandler(process, commandLine, Charsets.UTF_8) {

    private val sandboxInfoUpdaters = mutableListOf<FsiSandboxInfoUpdater>()

    override fun isSilentlyDestroyOnClose(): Boolean = true

    override fun notifyTextAvailable(text: String, outputType: Key<*>) {
        if (text != "SERVER-PROMPT>\n")
            super.notifyTextAvailable(text, outputType)
        else
            sandboxInfoUpdaters.forEach{it.onOutputEnd()}
    }

    fun addSandboxInfoUpdater (sandboxInfoUpdater: FsiSandboxInfoUpdater) {
        addProcessListener(sandboxInfoUpdater.fsiProcessOutputListener)

        sandboxInfoUpdaters.add(sandboxInfoUpdater)
    }

}