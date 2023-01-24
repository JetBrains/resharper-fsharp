package com.jetbrains.rider.projectView.projectTypes

class FSharpProjectTypeProvider : RiderProjectTypesProvider {
  companion object {
    val FSharpProjectType = RiderProjectType("fsproj", "{F2A71F9B-5D33-465A-A702-920D77279786}")
  }

  override fun getProjectType() = listOf(FSharpProjectType)
}
