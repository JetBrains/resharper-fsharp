package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.openapi.util.text.StringUtil

class CommandHistory {
  class Entry(val visibleText: String, val executableText: String) {
    override fun toString(): String {
      val lines = StringUtil.splitByLines(visibleText)
      return if (lines.size > 1) lines[0] + " ..." else lines[0]
    }
  }

  val entries = arrayListOf<Entry>()
  val size get() = entries.size

  operator fun get(i: Int) = entries[i]

  val listeners = arrayListOf<HistoryUpdateListener>()

  fun addEntry(entry: Entry) {
    if (entry.visibleText.isNotBlank())
      entries.add(entry)
    listeners.forEach { it.onNewEntry(entry) }
  }

}

interface HistoryUpdateListener {
  fun onNewEntry(entry: CommandHistory.Entry)
}
