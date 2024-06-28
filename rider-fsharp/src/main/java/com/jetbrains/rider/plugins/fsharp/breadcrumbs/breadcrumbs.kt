package com.jetbrains.rider.plugins.fsharp.breadcrumbs

import com.jetbrains.rider.breadcrumbs.BackendBreadcrumbsInfoProvider
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpBreadcrumbsInfoProvider : BackendBreadcrumbsInfoProvider() {
  override val language get() = FSharpLanguage

  override fun isShownByDefault(): Boolean = false
}
