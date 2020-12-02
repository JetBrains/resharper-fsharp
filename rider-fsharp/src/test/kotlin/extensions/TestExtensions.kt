package extensions

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.fsharp.RdExperimentalFeatures
import com.jetbrains.rider.plugins.fsharp.RdSetFeatures
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase

fun BaseTestWithSolutionBase.withExperimentalFeatures(project: Project, features: Array<RdExperimentalFeatures>, function: () -> Unit) {
    val testHost = project.solution.rdFSharpModel.fsharpTestHost
    testHost.setFeatures.sync(RdSetFeatures(features, true))
    function()
    testHost.setFeatures.sync(RdSetFeatures(features, false))
}
