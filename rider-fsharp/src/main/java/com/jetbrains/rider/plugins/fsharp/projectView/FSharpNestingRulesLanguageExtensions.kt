package com.jetbrains.rider.plugins.fsharp.projectView

import com.jetbrains.rider.projectView.nesting.RiderNestingRulesLanguageExtensions

class FSharpNestingRulesLanguageExtensions : RiderNestingRulesLanguageExtensions() {
  override fun getExtensions(): List<String> = listOf("fs")
}
