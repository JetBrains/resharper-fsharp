package com.jetbrains.rider.plugins.fsharp.test

import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.protocol.protocolHost
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import java.io.PrintStream

fun com.intellij.openapi.editor.Editor.dumpTypeProviders(stream: PrintStream) {
    with(stream) {
        println((project ?: return).solution.rdFSharpModel.fsharpTestHost.dumpTypeProvidersProcess.sync(Unit))
        println("\nSevereHighlighters:")
        dumpSevereHighlighters(this)
    }
}

fun withSetting(project: Project, setting: String, enterValue: String, exitValue: String, function: () -> Unit) {
    TestHost.getInstance(project.protocolHost).setSetting(setting, enterValue)
    try {
        function()
    } finally {
        TestHost.getInstance(project.protocolHost).setSetting(setting, exitValue)
    }
}

fun BaseTestWithSolution.withDisabledOutOfProcessTypeProviders(function: () -> Unit) {
    withSetting(project, "FSharp/FSharpOptions/FSharpExperimentalFeatures/OutOfProcessTypeProviders/@EntryValue", "false", "true") {
        function()
    }
}

fun withEditorConfig(project: Project, function: () -> Unit) {
    withSetting(project, "CodeStyle/EditorConfig/EnableEditorConfigSupport", "true", "false", function)
}

fun withCultureInfo(project: Project, culture: String, function: () -> Unit) {
    val getCultureInfoAndSetNew = project.fcsHost.getCultureInfoAndSetNew
    val oldCulture = getCultureInfoAndSetNew.sync(culture)
    try {
        function()
    } finally {
        getCultureInfoAndSetNew.sync(oldCulture)
    }
}

val Project.fcsHost get() = this.solution.rdFSharpModel.fsharpTestHost
