package com.jetbrains.rider.projectView.projectGenerators

import com.jetbrains.resharper.icons.ReSharperProjectModelIcons

abstract class FSharpProjectGeneratorBase : RiderZipProjectGenerator() {
    override fun getCategory() = ".NET F#"
    override fun getProjectTypeMainGuid() = "F2A71F9B-5D33-465A-A702-920D77279786"
    override fun getProjectTypeGuidList() = "{F2A71F9B-5D33-465A-A702-920D77279786}"
    override fun getDefaultProjectExtension() = "fsproj"
    override fun getLogo() = ReSharperProjectModelIcons.FsharpProject
}
