namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.ProjectModel
open JetBrains.Util.NetFX.Media.Colors

type OverrideBehavior(info, types) =
    inherit TextualBehavior<TextualInfo>(info)

    let getExpr (memberDecl: IMemberDeclaration) : IFSharpExpression =
        match memberDecl.Expression with
        | null ->
            memberDecl.AccessorDeclarationsEnumerable
            |> Seq.tryHead
            |> Option.map _.Expression
            |> Option.defaultValue null

        | expr -> expr

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let memberDeclaration =
            TextControlToPsi.GetElement<IMemberDeclaration>(solution, textControl)

        if isNotNull memberDeclaration then
            do
                use writeCookie = WriteLockCookie.Create(memberDeclaration.IsPhysical())

                use transactionCookie =
                    PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, "OverrideBehavior")

                GenerateOverrides.bindTypes types memberDeclaration

            let expr = getExpr memberDeclaration

            if isNotNull expr then
                let range = expr.GetDocumentRange()
                textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
                textControl.Selection.SetRange(range)

module OverrideRuleModule =

    [<Struct>]
    type ExpectedIndent =
        | MemberOwnerIndent of ownerIndent: int
        | SiblingDeclIndent of declIndent: int

    let getCaretCoords (context: FSharpCodeCompletionContext) =
        context.BasicContext.CaretDocumentOffset.ToDocumentCoords()

    let getGeneratorContext (context: FSharpCodeCompletionContext) : FSharpGeneratorContext =
        let basicContext = context.BasicContext
        let range = basicContext.SelectedRange

        if not range.IsEmpty then
            null
        else

            let view = PsiDocumentRangeView.Create(basicContext.SourceFile, range)
            let languageManager = basicContext.Solution.GetComponent<LanguageManager>()

            let generatorContextFactory =
                languageManager.TryGetService<IGeneratorContextFactory>(context.Language)

            generatorContextFactory.TryCreate(GeneratorStandardKinds.Overrides, view).As<FSharpGeneratorContext>()

    let getMemberOwner (context: FSharpCodeCompletionContext) (generatorContext: FSharpGeneratorContext) : ITreeNode =
        let anchor = generatorContext.Anchor

        let caretCoords = getCaretCoords context
        let caretColumn = int caretCoords.Column

        let nearestInterfaceImpl =
            match anchor with
            | null -> null
            | :? IMemberDeclaration as memberDecl ->
                memberDecl.GetContainingNode<IInterfaceImplementation>()
            | _ -> anchor.GetContainingNode<IInterfaceImplementation>(true) 

        match nearestInterfaceImpl with
        | interfaceImpl when isNotNull interfaceImpl && caretColumn > interfaceImpl.Indent ->
            interfaceImpl
        | _ ->
            let repr =
                if isNull anchor then
                    null
                else
                    anchor.GetContainingNode<IObjectModelTypeRepresentation>()

            match repr with
            | null ->
                match generatorContext.TypeDeclaration with
                | :? IObjExpr as objExpr -> objExpr
                | :? IFSharpTypeDeclaration as typeDecl -> typeDecl
                | _ -> null
            | repr -> repr

    let getExpectedIndent (memberOwner: ITreeNode) =
        let members : ITypeBodyMemberDeclaration seq =
            match memberOwner with
            | :? IInterfaceImplementation as impl -> impl.TypeMembersEnumerable |> Seq.cast
            | :? IObjExpr as objExpr -> objExpr.MemberDeclarationsEnumerable |> Seq.cast
            | :? IFSharpTypeDeclaration as typeDecl -> typeDecl.TypeMembersEnumerable
            | :? IObjectModelTypeRepresentation as repr -> repr.TypeMembersEnumerable
            | _ -> TreeNodeEnumerable.Empty

        match Seq.tryHead members with
        | Some memberDecl -> SiblingDeclIndent memberDecl.Indent
        | None -> MemberOwnerIndent memberOwner.Indent

    let mayGenerateOverrides
        (context: FSharpCodeCompletionContext)
        (generatorContext: FSharpGeneratorContext)
        (node: ITreeNode)
        =
        let caretCoords = getCaretCoords context
        let caretLine = caretCoords.Line
        let caretColumn = int caretCoords.Column

        let isInsideOwnerBody (memberOwner: ITreeNode) =
            match memberOwner with
            | :? IObjectModelTypeRepresentation as repr ->
                caretLine > repr.BeginKeyword.StartLine && caretLine < repr.EndKeyword.StartLine

            | :? IFSharpTypeDeclaration as typeDecl ->
                let repr = typeDecl.TypeRepresentation

                (isNull repr || isNotNull repr && caretLine > repr.EndLine)
                &&

                let equalsToken = typeDecl.EqualsToken in
                isNotNull equalsToken && caretLine > equalsToken.StartLine

            | :? IObjExpr as objExpr ->
                let withKeyword = objExpr.WithKeyword
                isNotNull withKeyword && caretLine > withKeyword.StartLine

            | :? IInterfaceImplementation as interfaceImpl ->
                let interfaceKeyword = interfaceImpl.InterfaceKeyword
                caretLine > interfaceKeyword.StartLine

            | _ -> false

        let memberOwner: ITreeNode =
            getMemberOwner context generatorContext

        match node with
        | Whitespace _ ->
            let isCorrectIndent (memberOwner: ITreeNode) =
                let indent = getExpectedIndent memberOwner
                match indent with
                | SiblingDeclIndent declIndent -> declIndent = caretColumn
                | MemberOwnerIndent ownerIndent -> caretColumn > ownerIndent

            let isAligned (memberOwner: ITreeNode) =
                isInsideOwnerBody memberOwner && isCorrectIndent memberOwner

            isNotNull memberOwner && isAligned memberOwner
        // override {selfId}.{caret}
        | TokenType FSharpTokenType.DOT _ ->
            let isCorrectIndent (memberOwner: ITreeNode) (memberDecl: IMemberDeclaration) =
                memberDecl.Indent >= memberOwner.Indent

            let isAligned (memberOwner: ITreeNode) (memberDecl: IMemberDeclaration) =
                isInsideOwnerBody memberOwner && isCorrectIndent memberOwner memberDecl

            let anchor = generatorContext.Anchor

            let memberDecl =
                match anchor with
                | :? IMemberDeclaration as decl -> decl
                | _ -> anchor.GetContainingNode<IMemberDeclaration>()

            isNotNull memberOwner
            && OverridableMemberDeclarationUtil.IsOverride memberDecl
            && isAligned memberOwner memberDecl
        | _ -> false

    let isOverrideRuleAvailable (checkOwner: ITreeNode -> bool) context =
        let generatorContext = getGeneratorContext context
        let node = context.NodeInFile

        (isWhitespace node || isDot node)
        && isNotNull generatorContext
        && isNotNull generatorContext.TypeDeclaration
        && checkOwner (getMemberOwner context generatorContext)
        && mayGenerateOverrides context generatorContext node

    let getOverridableElements (generatorContext: FSharpGeneratorContext) =
        GenerateOverrides.getOverridableMembers false generatorContext.TypeDeclaration
        |> GenerateOverrides.sanitizeMembers

    let createOverrideLookupItem
        (context: FSharpCodeCompletionContext)
        (generatorElement: FSharpGeneratorElement)
        (mayHaveBaseCalls: bool)
        =
        let node = context.NodeInFile
        let elementMember = generatorElement.Member
        let accessor = elementMember.As<IAccessor>()

        let mainMember =
            let owner = if isNotNull accessor then accessor.OwnerMember else null
            if isNotNull owner then owner else elementMember

        let memberDecl, types =
            GenerateOverrides.generateMember node mayHaveBaseCalls generatorElement

        let isDot = isDot node
        let anchor = if isDot then memberDecl.Delimiter.NextSibling else memberDecl.MemberKeyword
        let text = TreeRange(anchor, memberDecl.LastChild).GetText()
        let info = TextualInfo(text, text, Ranges = context.Ranges)

        let presentationText = if isDot then mainMember.ShortName else $"{memberDecl.MemberKeyword.GetText()} {mainMember.ShortName}"

        let icon =
            let iconManager = node.GetSolution().GetComponent<PsiIconManager>()
            iconManager.GetImage(mainMember, node.Language, true)

        LookupItemFactory
            .CreateLookupItem(info)
            .WithPresentation(fun _ ->
                let accessorName =
                    if isNull accessor || accessor.Parameters.Count = 0 then
                        ""
                    else

                        let accessorName =
                            match accessor.Kind with
                            | AccessorKind.GETTER -> "get"
                            | AccessorKind.SETTER -> "set"
                            | _ -> null

                        if isNull accessorName then "" else $" with {accessorName}"

                let signature =
                    if isNotNull accessor && accessor.Parameters.Count = 0 then
                        ""
                    else

                        let mfvInstance = generatorElement.MfvInstance
                        let substitution = mfvInstance.Substitution

                        mfvInstance.Mfv.CurriedParameterGroups
                        |> Seq.map (fun group ->
                            group
                            |> Seq.map _.Type.Instantiate(substitution).Format()
                            |> String.concat ", "
                            |> sprintf "(%s)")
                        |> String.concat " "

                let text = RichText(presentationText)

                text.Append(accessorName, TextStyle(JetFontStyles.Regular, JetSystemColors.GrayText))
                |> ignore

                text.Append(signature, TextStyle(JetFontStyles.Regular, JetSystemColors.GrayText))
                |> ignore

                TextualPresentation(text, info, image = icon))
            .WithBehavior(fun _ -> OverrideBehavior(info, types))
            .WithTextToMatch(presentationText)

    let keepOnlyOverrideItems (collector: IItemsCollector) =
        collector.RemoveWhere(fun (item: ILookupItem) ->
            match item with
            | :? FSharpKeywordLookupItem -> false
            | :? IAspectLookupItemBase as aspectItem -> not (aspectItem.Behavior :? OverrideBehavior)
            | _ -> true)

open OverrideRuleModule

[<Language(typeof<FSharpLanguage>)>]
type OverrideMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable (fun owner -> not (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        let generatorContext = getGeneratorContext context
        let mayHaveBaseCalls =
            GenerateOverrides.mayHaveBaseCalls generatorContext.TypeDeclaration

        let generatorElements = getOverridableElements generatorContext

        for generatorElement in generatorElements do

            let overrideItem =
                createOverrideLookupItem context generatorElement mayHaveBaseCalls

            collector.Add(overrideItem)

        false

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
