package typeProviders

import com.jetbrains.rider.plugins.fsharp.logs.FSharpLogTraceScenarios
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution

abstract class BaseTypeProvidersTest : BaseTestWithSolution() {
    override val restoreNuGetPackages = true
    override val traceCategories: List<String>
        get() = super.traceCategories + listOf(FSharpLogTraceScenarios.TypeProvidersTraceScenarioName)

    protected val rdFcsHost get() = project.solution.rdFSharpModel.fsharpTestHost
}
