package model

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*

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

    private val RdFcsHost = aggregatedef("RdFcsHost") {
        sink("fileChecked", string).async
        sink("projectChecked", string).async
        sink("fcsProjectInvalidated", structdef("RdFcsProject") {
            field("projectName", string)
            field("targetFramework", string)
        })
        call("getLastModificationStamp", string, dateTime)
        call("getSourceCache", string, structdef("RdFSharpSource") {
            field("source", string)
            field("timestamp", dateTime)
        }.nullable)
        call("dumpSingleProjectMapping", void, string)
    }

    init {
        field("fSharpInteractiveHost", RdFSharpInteractiveHost)
        field("fcsHost", RdFcsHost)
        property("fcsBusyDelayMs", int)
    }
}
