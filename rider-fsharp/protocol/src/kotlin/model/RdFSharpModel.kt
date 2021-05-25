package model

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator

@Suppress("unused")
object RdFSharpModel : Ext(SolutionModel.Solution) {

    private val fsiRuntime = enum("RdFsiRuntime") {
        +"NetFramework"
        +"Mono"
        +"Core"
    }

    private val RdFSharpInteractiveHost = aggregatedef("RdFSharpInteractiveHost") {
        call("requestNewFsiSessionInfo", void, structdef("RdFsiSessionInfo") {
            field("fsiPath", string)
            field("runtime", fsiRuntime)
            field("isCustomTool", bool)
            field("args", immutableList(string))
            field("fixArgsForAttach", bool)
        })
        call("getProjectReferences", int, immutableList(string))
        field("fsiTools", aggregatedef("RdFSharpInteractiveTools") {
            call("prepareCommands", structdef("RdFsiPrepareCommandsArgs") {
                field("firstCommandIndex", int)
                field("commands", immutableList(string))
            }, immutableList(string))
        })
        property("moveCaretOnSendLine", bool).readonly
        property("moveCaretOnSendSelection", bool).readonly
        property("copyRecentToEditor", bool).readonly
    }

    private val RdFSharpTestHost = aggregatedef("RdFSharpTestHost") {
        sink("fileChecked", string).async
        sink("fcsProjectInvalidated", structdef("RdFcsProject") {
            field("projectName", string)
            field("targetFramework", string)
        })
        call("getLastModificationStamp", string, dateTime)
        call("getCultureInfoAndSetNew", string, string)
        call("getSourceCache", string, structdef("RdFSharpSource") {
            field("source", string)
            field("timestamp", dateTime)
        }.nullable)
        call("dumpSingleProjectMapping", void, string)
        call("dumpSingleProjectLocalReferences", void, immutableList(string))
        call("typeProvidersRuntimeVersion", void, string.nullable)
        call("dumpTypeProvidersProcess", void, string)
    }

    private val RdFSharpTypeProvidersHost = aggregatedef("RdFSharpTypeProvidersHost") {
        call("restartTypeProviders", void, void)
        call("isLaunched", void, bool)
    }

    init {

        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.plugins.fsharp")
        setting(CSharp50Generator.Namespace, "JetBrains.ReSharper.Plugins.FSharp")

        field("fSharpInteractiveHost", RdFSharpInteractiveHost)
        field("fSharpTestHost", RdFSharpTestHost)
        field("fSharpTypeProvidersHost", RdFSharpTypeProvidersHost)
        property("fcsBusyDelayMs", int)
    }
}
