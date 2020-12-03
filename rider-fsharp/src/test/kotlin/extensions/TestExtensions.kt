package extensions

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.fsharp.RdEnableOrDisableFeatures
import com.jetbrains.rider.plugins.fsharp.RdFSharpFeatures
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase

fun BaseTestWithSolutionBase.withExperimentalFeatures(project: Project, features: Array<RdFSharpFeatures>, function: () -> Unit) {
    val testHost = project.solution.rdFSharpModel.fsharpTestHost
    testHost.enableOrDisableFeatures.sync(RdEnableOrDisableFeatures(features, true))
    function()
    testHost.enableOrDisableFeatures.sync(RdEnableOrDisableFeatures(features, false))
}
