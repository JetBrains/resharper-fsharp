namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Psi.Caches

type FSharpImportTypeFix(reference) =
    inherit ImportTypeFix(reference)

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(context.GetPsiModule(), true)


type FSharpReferenceModuleAndTypeFix(reference) =
    inherit ReferenceModuleAndTypeFix(reference)

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(LibrarySymbolScope.FULL)
