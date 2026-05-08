namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches

[<RequireQualifiedAccess>]
module FSharpImportTypeFix =
    let doAdditionalSorting (candidates: ITypeElement seq) =
        candidates
        |> Seq.sortByDescending (function
            | :? IFSharpTypeElement as fsTypeElement -> fsTypeElement.GetFSharpAccessRights().IsFilePrivate
            | _ -> false
        )

type FSharpImportTypeFix(reference) =
    inherit ImportTypeFix(reference)

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(context.GetPsiModule(), true)

    override this.DoAdditionalOrdering(candidates) =
        FSharpImportTypeFix.doAdditionalSorting candidates


type FSharpReferenceModuleAndTypeFix(reference) =
    inherit ReferenceModuleAndTypeFix(reference)

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(LibrarySymbolScope.FULL)

    override this.DoAdditionalOrdering(candidates) =
        FSharpImportTypeFix.doAdditionalSorting candidates
