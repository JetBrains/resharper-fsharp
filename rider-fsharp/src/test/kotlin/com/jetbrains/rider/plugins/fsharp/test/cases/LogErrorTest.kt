package com.jetbrains.rider.plugins.fsharp.test.cases

import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rdclient.util.BackendException
import com.jetbrains.rider.test.TestCaseRunner
import com.jetbrains.rider.test.IntegrationTestCaseRunner
import org.testng.annotations.Test

class LogErrorTest : IntegrationTestCaseRunner() {

  @Test(expectedExceptions = [(BackendException::class)])
  fun testThrowEx() {
    Logger.getInstance("Projected Logger").error(BackendException("a"))
    testCaseErrorsProcessor.throwIfNotEmpty()
  }
}