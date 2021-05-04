import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.protocol.protocolHost
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import java.io.PrintStream

fun com.intellij.openapi.editor.Editor.dumpTypeProviders(stream: PrintStream) {
    with(stream) {
        println((project ?: return).solution.rdFSharpModel.fsharpTestHost.dumpTypeProvidersProcess.sync(Unit))
        println("\nSevereHighlighters:")
        dumpSevereHighlighters(this)
    }
}

fun withSetting(project: Project, setting: String, function: () -> Unit) {
    TestHost.getInstance(project.protocolHost).setSetting(setting, "true")
    try {
        function()
    } finally {
        TestHost.getInstance(project.protocolHost).setSetting(setting, "false")
    }
}

fun BaseTestWithSolution.withTypeProviders(function: () -> Unit) {
    withSetting(project, "FSharp/FSharpOptions/FSharpExperimentalFeatures/OutOfProcessTypeProviders/@EntryValue", function)
}

fun withEditorConfig(project: Project, function: () -> Unit) {
    withSetting(project, "CodeStyle/EditorConfig/EnableEditorConfigSupport", function)
}
