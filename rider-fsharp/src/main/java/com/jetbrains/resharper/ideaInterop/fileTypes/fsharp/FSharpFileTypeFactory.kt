package com.jetbrains.resharper.ideaInterop.fileTypes.fsharp

import com.intellij.openapi.fileTypes.ExtensionFileNameMatcher
import com.intellij.openapi.fileTypes.FileNameMatcher
import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

class FSharpFileTypeFactory : FileTypeFactory() {
    private val FSharpFileExtensions: List<String> = arrayListOf("fs", "fsi", "fsx", "fsscript", "ml", "mli")
    private val FSharpFileNameMatchers: List<FileNameMatcher> = FSharpFileExtensions.map(::ExtensionFileNameMatcher)

    override fun createFileTypes(consumer: FileTypeConsumer) {
        for (matcher in FSharpFileNameMatchers)
            consumer.consume(FSharpFileType, matcher)
        consumer.consume(FSharpFileType)
    }
}