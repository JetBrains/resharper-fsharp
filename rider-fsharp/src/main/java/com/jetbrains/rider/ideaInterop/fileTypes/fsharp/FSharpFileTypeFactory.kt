package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

abstract class FSharpFileTypeFactoryBase(vararg val extensions: String) : FileTypeFactory() {
    override fun createFileTypes(consumer: FileTypeConsumer) {
        for (matcher in extensions)
            consumer.consume(FSharpScriptFileType, matcher)
    }
}

class FSharpFileTypeFactory : FSharpFileTypeFactoryBase("fs", "fsi", "ml", "mli")
class FSharpScriptFileTypeFactory : FSharpFileTypeFactoryBase("fsx", "fsscript")
