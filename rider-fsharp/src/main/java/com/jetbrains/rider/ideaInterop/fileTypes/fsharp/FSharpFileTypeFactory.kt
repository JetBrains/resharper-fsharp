package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

abstract class FSharpFileTypeFactoryBase(private val fileType: FileType, private vararg val extensions: String) : FileTypeFactory() {
    override fun createFileTypes(consumer: FileTypeConsumer) = extensions.forEach { consumer.consume(fileType, it) }
}

class FSharpFileTypeFactory : FSharpFileTypeFactoryBase(FSharpFileType, "fs", "fsi", "ml", "mli")
class FSharpScriptFileTypeFactory : FSharpFileTypeFactoryBase(FSharpScriptFileType, "fsx", "fsscript")
