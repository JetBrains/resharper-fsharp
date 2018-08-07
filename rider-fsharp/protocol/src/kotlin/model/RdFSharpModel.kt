package model

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.generator.nova.PredefinedType.*

@Suppress("unused")
object RdFSharpModel : Ext(SolutionModel.Solution) {

    private val RdFSharpInteractiveHost = aggregatedef("RdFSharpInteractiveHost") {
        call("requestNewFsiSessionInfo", void, structdef("RdFsiSessionInfo") {
            field("fsiPath", string)
            field("args", immutableList(string))
            field("FixArgsForAttach", bool)
        })
        property("moveCaretOnSendLine", bool).readonly
        property("copyRecentToEditor", bool).readonly
    }

    private val RdFSharpCompilerServiceHost = aggregatedef("RdFSharpCompilerServiceHost")  {
        sink("fileChecked", string).async
        sink("projectChecked", string).async
    }

    init {
        field("fSharpInteractiveHost", RdFSharpInteractiveHost)
        field("fSharpCompilerServiceHost", RdFSharpCompilerServiceHost)
    }
}