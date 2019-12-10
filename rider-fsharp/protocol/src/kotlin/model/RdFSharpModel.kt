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
        property("moveCaretOnSendLine", bool).readonly
        property("copyRecentToEditor", bool).readonly
    }

    private val RdFSharpCompilerServiceHost = aggregatedef("RdFSharpCompilerServiceHost") {
        sink("fileChecked", string).async
        sink("projectChecked", string).async
        call("getLastModificationStamp", string, dateTime)
        call("getSourceCache", string, structdef("RdFSharpSource") {
            field("source", string)
            field("timestamp", dateTime)
        }.nullable)
        call("dumpSingleProjectMapping", void, string)
    }

    init {
        field("fSharpInteractiveHost", RdFSharpInteractiveHost)
        field("fSharpCompilerServiceHost", RdFSharpCompilerServiceHost)
        property("enableExperimentalFeatures", bool)
    }
}
