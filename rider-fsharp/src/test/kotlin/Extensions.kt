import com.intellij.execution.process.impl.ProcessListUtil
import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.protocol.protocolHost
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import java.io.PrintStream

fun com.intellij.openapi.editor.Editor.dumpTypeProviders(stream: PrintStream) {
    with(stream) {
        println((project ?: return).solution.rdFSharpModel.fsharpTestHost.dumpTypeProvidersProcess.sync(Unit))
        println("\nSevereHighlighters:")
        dumpSevereHighlighters(this)
    }
}

fun withSettings(project: Project, settings: List<String>, function: () -> Unit) {
    settings.forEach { TestHost.getInstance(project.protocolHost).setSetting(it, "true") }
    try {
        function()
    } finally {
        settings.forEach { TestHost.getInstance(project.protocolHost).setSetting(it, "false") }
    }
}

fun BaseTestWithSolution.withTypeProviders(shadowCopyMode: Boolean = false, function: () -> Unit) {
    val settings = mutableListOf("FSharp/FSharpOptions/FSharpExperimentalFeatures/OutOfProcessTypeProviders/@EntryValue")
    if (shadowCopyMode) settings.add("FSharp/FSharpOptions/FSharpExperimentalFeatures/HostTypeProvidersFromTempFolder/@EntryValue")
    withSettings(project, settings) {
        try {
            function()
        } finally {
            val tpProcessCount = ProcessListUtil
                    .getProcessList()
                    .count { it.executableName.startsWith("JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host") }
            if (tpProcessCount != 1) frameworkLogger.warn("Expected single type providers process, but was $tpProcessCount")
        }
    }
}

fun withEditorConfig(project: Project, function: () -> Unit) {
    withSettings(project, listOf("CodeStyle/EditorConfig/EnableEditorConfigSupport"), function)
}

fun withCultureInfo(project: Project, culture: String, function: () -> Unit) {
    val getCultureInfoAndSetNew = project.solution.rdFSharpModel.fsharpTestHost.getCultureInfoAndSetNew
    val oldCulture = getCultureInfoAndSetNew.sync(culture)
    try {
        function()
    } finally {
        getCultureInfoAndSetNew.sync(oldCulture)
    }
}
