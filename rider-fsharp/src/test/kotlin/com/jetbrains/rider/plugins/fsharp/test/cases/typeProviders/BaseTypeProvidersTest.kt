package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rider.plugins.fsharp.logs.FSharpLogTraceScenarios
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.base.PerTestSolutionTestBase

abstract class BaseTypeProvidersTest : PerTestSolutionTestBase() {
  override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
    params.restoreNuGetPackages = true
  }

  override val traceScenarios: Set<LogTraceScenario>
    get() = super.traceScenarios + FSharpLogTraceScenarios.FSharpTypeProviders

  protected val rdFcsHost get() = project.solution.rdFSharpModel.fsharpTestHost
}
