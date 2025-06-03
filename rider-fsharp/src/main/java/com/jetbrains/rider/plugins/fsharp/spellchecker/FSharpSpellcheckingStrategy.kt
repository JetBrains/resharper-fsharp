package com.jetbrains.rider.plugins.fsharp.spellchecker

import com.intellij.rider.rdclient.dotnet.spellchecker.strategy.BackendLanguageSpellcheckingStrategy
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpSpellcheckingStrategy : BackendLanguageSpellcheckingStrategy(FSharpLanguage)
