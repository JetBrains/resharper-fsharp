namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Application.Threading
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpImportTypeHelper() =
    let [<Literal>] opName = "FSharpImportTypeHelper.FindTypeCandidates"

    interface IImportTypeHelper with
        member x.FindTypeCandidates(reference, importTypeCacheFactory) =
            let reference = reference.As<FSharpSymbolReference>()
            if isNull reference || reference.IsQualified then Seq.empty else

            let context = reference.GetElement()
            let names = reference.GetAllNames().ResultingList()
            let factory = importTypeCacheFactory.Invoke(context)

            names
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
