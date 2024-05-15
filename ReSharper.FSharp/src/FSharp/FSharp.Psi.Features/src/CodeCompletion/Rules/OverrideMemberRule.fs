namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.ProjectModel
open JetBrains.Util.Media
open JetBrains.Util.NetFX.Media.Colors

type OverrideBehavior(info) =
    inherit TextualBehavior<TextualInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()
        let node = textControl.Document.GetPsiSourceFile(solution).FSharpFile.FindNodeAt(nameRange)
        let memberDeclaration = node.GetContainingNode<IMemberDeclaration>()

        if isNotNull memberDeclaration then
            let range = memberDeclaration.Expression.GetDocumentRange()
            textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
            textControl.Selection.SetRange(range)


[<Language(typeof<FSharpLanguage>)>]
type OverrideMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getCaretCoords (context: FSharpCodeCompletionContext) =
        context.BasicContext.CaretDocumentOffset.ToDocumentCoords()

    let getGeneratorContext (context: FSharpCodeCompletionContext) : FSharpGeneratorContext =
        let basicContext = context.BasicContext
        let range = basicContext.SelectedRange
        if not range.IsEmpty then null else

        let view = PsiDocumentRangeView.Create(basicContext.SourceFile, range)
        let languageManager = basicContext.Solution.GetComponent<LanguageManager>()
        let generatorContextFactory = languageManager.TryGetService<IGeneratorContextFactory>(context.Language)

        generatorContextFactory.TryCreate(GeneratorStandardKinds.Overrides, view).As<FSharpGeneratorContext>()

    let mayGenerateOverrides (context: FSharpCodeCompletionContext) (generatorContext: FSharpGeneratorContext) =
        let node = context.NodeInFile
        isWhitespace node &&

        let caretCoords = getCaretCoords context
        let caretLine = caretCoords.Line
        let caretColumn = int caretCoords.Column

        let isInsideOwnerBody (memberOwner: ITreeNode) =
            match memberOwner with
            | :? IObjectModelTypeRepresentation as repr ->
                caretLine > repr.BeginKeyword.StartLine && caretLine < repr.EndKeyword.StartLine

            | :? IFSharpTypeDeclaration as typeDecl ->
                let repr = typeDecl.TypeRepresentation
                (isNull repr || isNotNull repr && caretLine > repr.EndLine) &&

                let equalsToken = typeDecl.EqualsToken
                isNotNull equalsToken && caretLine > equalsToken.StartLine

            | :? IObjExpr as objExpr ->
                let equalsToken = objExpr.WithKeyword
                isNotNull equalsToken && caretLine > equalsToken.StartLine

            | _ -> false

        let isCorrectIndent (memberOwner: ITreeNode) (members: ITypeBodyMemberDeclaration seq) =
            match Seq.tryHead members with
            | Some memberDecl -> memberDecl.Indent = caretColumn
            | None -> caretColumn > memberOwner.Indent

        let isAligned (memberOwner: ITreeNode) (members: ITypeBodyMemberDeclaration seq) =
            isInsideOwnerBody memberOwner && isCorrectIndent memberOwner members

        let (memberOwner: ITreeNode), (members: ITypeBodyMemberDeclaration seq) =
            let anchor = generatorContext.Anchor
            let repr = if isNull anchor then null else anchor.GetContainingNode<IObjectModelTypeRepresentation>()

            match repr with
            | null ->
                match generatorContext.TypeDeclaration with
                | :? IObjExpr as objExpr -> objExpr, objExpr.MemberDeclarationsEnumerable |> Seq.cast
                | :? IFSharpTypeDeclaration as typeDecl -> typeDecl, typeDecl.TypeMembersEnumerable
                | _ -> null, TreeNodeEnumerable.Empty
            | repr -> repr, repr.TypeMembersEnumerable

        isNotNull memberOwner && isAligned memberOwner members

    override this.IsAvailable(context) =
        let generatorContext = getGeneratorContext context
        isNotNull generatorContext &&
        isNotNull generatorContext.TypeDeclaration &&
        mayGenerateOverrides context generatorContext

    override this.AddLookupItems(context, collector) =
        let iconManager = context.NodeInFile.GetSolution().GetComponent<PsiIconManager>()
        let generatorContext = getGeneratorContext context
        let mayHaveBaseCalls = GenerateOverrides.mayHaveBaseCalls generatorContext.TypeDeclaration

        let generatorElements =
            GenerateOverrides.getOverridableMembers generatorContext.TypeDeclaration false
            |> GenerateOverrides.sanitizeMembers

        for generatorElement in generatorElements do
            let elementMember = generatorElement.Member
            let accessor = elementMember.As<IAccessor>()

            let mainMember =
                let owner = if isNotNull accessor then accessor.OwnerMember else null
                if isNotNull owner then owner else elementMember

            let memberDecl = GenerateOverrides.generateMember context.NodeInFile mayHaveBaseCalls 0 generatorElement
            let overrideItem =
                let info =
                    let text = memberDecl.GetText()
                    TextualInfo(text, text, Ranges = context.Ranges)

                let name = mainMember.ShortName
                let presentationText = $"override {name}"
                let icon = iconManager.GetImage(mainMember, context.NodeInFile.Language, true)

                LookupItemFactory
                    .CreateLookupItem(info)
                        .WithPresentation(fun _ ->
                            

                            let accessorName =
                                if isNull accessor || accessor.Parameters.Count = 0 then "" else

                                let accessorName =
                                    match accessor.Kind with
                                    | AccessorKind.GETTER -> "get"
                                    | AccessorKind.SETTER -> "set"
                                    | _ -> null

                                if isNull accessorName then "" else $" with {accessorName}"

                            let signature =
                                if isNotNull accessor && accessor.Parameters.Count = 0 then "" else
 
                                let mfvInstance = generatorElement.MfvInstance
                                let substitution = mfvInstance.Substitution
                                let displayContext = mfvInstance.DisplayContext

                                mfvInstance.Mfv.CurriedParameterGroups
                                |> Seq.map (fun group ->
                                    group
                                    |> Seq.map (fun p -> p.Type.Instantiate(substitution).Format(displayContext))
                                    |> String.concat ", "
                                    |> sprintf "(%s)"
                                )
                                |> String.concat " "

                            let text = RichText(presentationText)
                            text.Append(accessorName, TextStyle(JetFontStyles.Regular, JetSystemColors.GrayText)) |> ignore
                            text.Append(signature, TextStyle(JetFontStyles.Regular, JetSystemColors.GrayText)) |> ignore
                            TextualPresentation(text, info, image = icon)
                        )
                        .WithBehavior(fun _ -> OverrideBehavior(info))
                        .WithMatcher(fun _ -> TextualMatcher(presentationText, info))

            collector.Add(overrideItem)

        false

    override this.TransformItems(context, collector) =
        collector.RemoveWhere(fun (item: ILookupItem) ->
            match item with
            | :? FSharpKeywordLookupItem -> false
            | :? IAspectLookupItemBase as aspectItem ->
                not (aspectItem.Behavior :? OverrideBehavior)
            | _ -> true
        )

        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
