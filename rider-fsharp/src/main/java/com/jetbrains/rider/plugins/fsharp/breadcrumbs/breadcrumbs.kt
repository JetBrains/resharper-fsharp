package com.jetbrains.rider.plugins.fsharp.breadcrumbs

import com.jetbrains.rider.breadcrumbs.BackendBreadcrumbsInfoProvider
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage

class FSharpBreadcrumbsInfoProvider : BackendBreadcrumbsInfoProvider() {
  override val language get() = FSharpLanguage
}

class FSharpScriptBreadcrumbsInfoProvider : BackendBreadcrumbsInfoProvider() {
  override val language get() = FSharpScriptLanguage
}