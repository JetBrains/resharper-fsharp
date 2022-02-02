package com.jetbrains.rider.test.cases

import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rdclient.util.BackendException
import com.jetbrains.rider.test.TestCaseRunner
import org.testng.annotations.Test

class LogErrorTest : TestCaseRunner() {

    @Test(expectedExceptions = [(BackendException::class)])
    fun testThrowEx() {
        Logger.getInstance("Projected Logger").error(BackendException("a"))
        testCaseErrorsProcessor.throwIfNotEmpty()
    }
}