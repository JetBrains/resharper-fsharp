namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.ProjectModel


type OverrideBehavior(info) =
    inherit TextualBehavior<TextualInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()
        let node = textControl.Document.GetPsiSourceFile(solution).FSharpFile.FindNodeAt(nameRange)
        let memberDeclaration = node.GetContainingNode<IMemberDeclaration>()

        if isNotNull memberDeclaration then
            let range = memberDeclaration.Expression.GetNavigationRange()
            textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
            textControl.Selection.SetRange(range)

module private Utils =
    let (|TypeDeclarationAtEndOfFile|_|) (node: ITreeNode): IFSharpTypeDeclaration option =
        match node with
        | :? IFSharpImplFile as impFile ->
            match Seq.tryLast impFile.FSharpFile.ModuleDeclarationsEnumerable with
            | Some (:? IQualifiableModuleLikeDeclaration as moduleDecl) ->
                match Seq.tryLast moduleDecl.Members with
                | Some (:? ITypeDeclarationGroup as tdg) ->
                    match Seq.tryLast tdg.TypeDeclarationsEnumerable with
                    | Some (:? IFSharpTypeDeclaration as td) ->
                        Some td
                    | _ -> None
                | _ -> None
            | _ -> None
        | _ -> None

[<Language(typeof<FSharpLanguage>)>]
type OverrideMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()
    let findOverridableMembers (context: FSharpCodeCompletionContext) : FSharpGeneratorElement array =
        let node = context.NodeInFile
        if isNull node then Array.empty else
        if not (isWhitespace node) then Array.empty else

        let findOverrideIfAlignedWithMeaningfulSibling (td: IFSharpTypeDeclaration) (meaningfulSibling: ITreeNode) =
            match meaningfulSibling with
            | :? IMemberDeclaration
            | :? IAutoPropertyDeclaration ->
                let caretCoords = context.BasicContext.CaretDocumentOffset.ToDocumentCoords()
                let siblingCoords = meaningfulSibling.GetDocumentRange().StartOffset.ToDocumentCoords()
                if caretCoords.Column <> siblingCoords.Column then Array.empty else
                GenerateOverrides.getOverridableMembers td false |> Array.ofSeq
            | _ -> Array.empty

        match node.Parent with
        | Utils.TypeDeclarationAtEndOfFile td ->
            let meaningfulSibling = Seq.tryLast td.TypeMembersEnumerable |> Option.toObj
            findOverrideIfAlignedWithMeaningfulSibling td meaningfulSibling
        | :? IFSharpTypeDeclaration as td ->
            let nextMeaningfulSibling = node.GetNextMeaningfulSibling()
            findOverrideIfAlignedWithMeaningfulSibling td nextMeaningfulSibling
        | :? IQualifiableModuleLikeDeclaration as moduleLikeDeclaration ->
            let previousSibling = node.GetPreviousMeaningfulSibling()
            match previousSibling with
            | :? ITypeDeclarationGroup as tdg ->
                match Seq.tryLast tdg.TypeDeclarationsEnumerable with
                | Some (:? IFSharpTypeDeclaration as td) ->
                    let previousMeaningfulSibling = Seq.tryLast td.TypeMembersEnumerable |> Option.toObj
                    findOverrideIfAlignedWithMeaningfulSibling td previousMeaningfulSibling
                | _ -> Array.empty
            | _ -> Array.empty
        | :? IObjectModelTypeRepresentation as objModelRepr ->
            if objModelRepr.TypeMembers.IsEmpty then
                let caretCoords = context.BasicContext.CaretDocumentOffset.ToDocumentCoords()
                let beginCoords = objModelRepr.BeginKeyword.GetDocumentRange().EndOffset.ToDocumentCoords()
                let endCoords = objModelRepr.EndKeyword.GetDocumentRange().StartOffset.ToDocumentCoords()
                // Only show entries when the cursor is between the `begin` and `end` keyword
                if beginCoords.Line < caretCoords.Line && caretCoords.Line < endCoords.Line then
                    GenerateOverrides.getOverridableMembers objModelRepr.TypeDeclaration false |> Array.ofSeq
                else
                    Array.empty
            else
                // Align with matching sibling
                let meaningfulSibling = Seq.tryLast objModelRepr.TypeMembersEnumerable |> Option.toObj
                findOverrideIfAlignedWithMeaningfulSibling objModelRepr.TypeDeclaration meaningfulSibling
        | _ -> Array.empty

    override this.IsAvailable(context) =
        // Empty struct/end
        // After union case repr
        // type X = {caret}
        // After record case repr

        let result = findOverridableMembers context
        not (Array.isEmpty result)
        // Next: node.Parent should be ITypeDeclaration
        // Verify the cursor position with NextMeaningfulSibling()
        // And if next meaningful sibling make sense
        // Or previous sibling

    override this.AddLookupItems(context, collector) =
        let solution = context.NodeInFile.GetSolution()
        let iconManager = solution.GetComponent<PsiIconManager>()

        for overrideMember in findOverridableMembers context do
            let memberDeclaration = GenerateOverrides.generateMember context.NodeInFile 0 overrideMember
            let overrideItem =
                let info =
                    let text = memberDeclaration.GetText()
                    TextualInfo(text, text, Ranges = context.Ranges)

                let icon = iconManager.GetImage(memberDeclaration.DeclaredElement, context.NodeInFile.Language, true)

                LookupItemFactory
                    .CreateLookupItem(info)
                        .WithPresentation(fun _ ->
                            // Check what icon C# uses, how it behaves.
                            let signature =
                                let hasOverloads =
                                    isNotNull overrideMember.DeclaredElement
                                    && overrideMember.DeclaredElement.GetDeclarations().Count > 1

                                if not hasOverloads then "" else
                                overrideMember.Mfv.FullType.Format(overrideMember.MfvInstance.DisplayContext)
                                |> sprintf " (%s)"

                            TextualPresentation(
                                RichText($"override %s{overrideMember.Member.ShortName}%s{signature}"),
                                info,
                                image = icon) :> _)
                        .WithBehavior(fun _ -> OverrideBehavior(info))
                        .WithMatcher(fun _ -> TextualMatcher(info))

            collector.Add(overrideItem)
        false
