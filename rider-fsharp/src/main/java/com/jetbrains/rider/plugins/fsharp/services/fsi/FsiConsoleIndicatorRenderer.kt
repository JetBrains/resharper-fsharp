package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.openapi.editor.markup.GutterIconRenderer

class FsiConsoleIndicatorRenderer(private val iconWithTooltip: IconWithTooltip) : GutterIconRenderer() {
    override fun getIcon() = iconWithTooltip.icon
    override fun getTooltipText() = iconWithTooltip.tooltip

    override fun equals(other: Any?) = icon == (other as? FsiConsoleIndicatorRenderer)?.icon
    override fun hashCode() = icon.hashCode()
}
