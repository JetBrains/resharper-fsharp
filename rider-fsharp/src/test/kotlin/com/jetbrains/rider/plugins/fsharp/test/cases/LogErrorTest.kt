package com.jetbrains.rider.plugins.fsharp.test.cases

import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rd.platform.diagnostics.BackendException
import com.jetbrains.rider.test.TestLoggerManager
import com.jetbrains.rider.test.IntegrationTestCaseRunner
import org.testng.annotations.Test

class LogErrorTest : IntegrationTestCaseRunner() {

  @Test(expectedExceptions = [(BackendException::class)])
  fun testThrowEx() {
    Logger.getInstance("Projected Logger").error(BackendException("a"))
    TestLoggerManager.testErrorsProcessor.throwIfNotEmpty()
  }
}