package com.jetbrains.resharper.projectView.projectGenerators

import java.io.File

class FSharpLibrary : FSharpProjectGeneratorBase() {
    override fun getName() = "Library"
    override fun getDefaultProjectName() = "Library"
    override fun getTemplateName() = "FSharpLibrary"

    override fun getFilesToRename(projectFile: File): Array<Pair<File, File>> {
        val filesToRename = super.getFilesToRename(projectFile).toMutableList()

        filesToRename.add(Pair(
                File(projectFile.parent, "project.fs"),
                File(projectFile.parent, projectFile.nameWithoutExtension + ".fs")))

        return filesToRename.toTypedArray()
    }
}
