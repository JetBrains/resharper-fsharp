package com.jetbrains.resharper.projectView.projectGenerators

class FSharpConsoleApplication : FSharpProjectGeneratorBase() {
    override fun getName() = "Console Application"
    override fun getDefaultProjectName() = "ConsoleApplication"
    override fun getTemplateName() = "FSharpConsoleApplication"
}
