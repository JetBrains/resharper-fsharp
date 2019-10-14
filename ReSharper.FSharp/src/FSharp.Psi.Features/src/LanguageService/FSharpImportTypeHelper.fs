namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Application.Threading
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resolve

[<Language(typeof<FSharpLanguage>)>]
type FSharpImportTypeHelper() =
    let isAllowed (reference: IReference) =
        match reference.As<FSharpSymbolReference>() with
        | null -> false
        | reference ->

        match reference.GetElement() with
        | :? IReferenceExpr as refExpr -> isNull refExpr.Qualifier
        | :? IReferenceName as referenceName -> isNull referenceName.Qualifier
        | _ -> false

    interface IImportTypeHelper with
        member x.FindTypeCandidates(reference, importTypeCacheFactory) =
            if not (isAllowed reference) then Seq.empty else

            let reference = reference :?> FSharpSymbolReference
            if isNull reference then Seq.empty else

            let factory = importTypeCacheFactory.Invoke(reference.GetElement())

            reference.GetAllNames().ResultingList()
            |> Seq.collect factory.Invoke
            |> Seq.filter (fun clrDeclaredElement -> clrDeclaredElement :? ITypeElement)
            |> Seq.cast

        member x.ReferenceTargetCanBeType _ = true
        member x.ReferenceTargetIsUnlikelyBeType _ = false


[<Language(typeof<FSharpLanguage>)>]
type FSharpQuickFixUtilComponent() =
    interface IQuickFixUtilComponent with
        member x.BindTo(reference, typeElement, _, _) =
            // todo: fix addOpen
            let reference = reference :?> FSharpSymbolReference
            let fsFile = reference.GetElement().FSharpFile

            let sourceFile = fsFile.GetSourceFile()
            let document = fsFile.GetSourceFile().Document
            let coords = document.GetCoordsByOffset(reference.GetTreeTextRange().StartOffset.Offset)
            let ns = typeElement.GetContainingNamespace().QualifiedName

            // todo: rewrite addOpen to change psi instead
            sourceFile.GetSolution().Locks.QueueReadLock("Hack for import during psi transaction", fun _ ->
                addOpen coords fsFile null ns)

            reference :> _

        member x.AddImportsForExtensionMethod(reference, _) = reference


// todo: ExtensionMethodImportUtilBase
