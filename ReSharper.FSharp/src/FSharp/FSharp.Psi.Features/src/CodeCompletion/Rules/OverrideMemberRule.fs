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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.ProjectModel
open JetBrains.Util.Extension
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

    let getMemberOwnerAndItsMembers
        (context: FSharpCodeCompletionContext)
        (generatorContext: FSharpGeneratorContext)
        : ITreeNode * ITypeBodyMemberDeclaration seq =
        let anchor = generatorContext.Anchor

        let caretCoords = getCaretCoords context
        let caretColumn = int caretCoords.Column

        let (|IsInInterfaceImplScope|_|) (impl: IInterfaceImplementation) =
            let isInScope =
                isNotNull impl
                && let indent = impl.Indent in
                   caretColumn > indent

            if isInScope then Some impl else None

        let nearestInterfaceImpl =
            match anchor with
            | :? IMemberDeclaration as memberDecl ->
                memberDecl.GetContainingNode<IInterfaceImplementation>()
            | :? IInterfaceImplementation as impl -> impl
            | null -> null
            | _ -> anchor.GetContainingNode<IInterfaceImplementation>()

        match nearestInterfaceImpl with
        | IsInInterfaceImplScope interfaceImpl -> interfaceImpl, interfaceImpl.TypeMembersEnumerable |> Seq.cast
        | _ ->
            let repr =
                if isNull anchor then
                    null
                else
                    anchor.GetContainingNode<IObjectModelTypeRepresentation>()

            match repr with
            | null ->
                match generatorContext.TypeDeclaration with
                | :? IObjExpr as objExpr -> objExpr, objExpr.MemberDeclarationsEnumerable |> Seq.cast
                | :? IFSharpTypeDeclaration as typeDecl -> typeDecl, typeDecl.TypeMembersEnumerable
                | _ -> null, TreeNodeEnumerable.Empty
            | repr -> repr, repr.TypeMembersEnumerable

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

        let (memberOwner: ITreeNode), (members: ITypeBodyMemberDeclaration seq) =
            getMemberOwnerAndItsMembers context generatorContext

        match node with
        | Whitespace _ ->
            let isCorrectIndent (memberOwner: ITreeNode) (members: ITypeBodyMemberDeclaration seq) =
                match Seq.tryHead members with
                | Some memberDecl -> memberDecl.Indent = caretColumn
                | None -> caretColumn > memberOwner.Indent

            let isAligned (memberOwner: ITreeNode) (members: ITypeBodyMemberDeclaration seq) =
                isInsideOwnerBody memberOwner && isCorrectIndent memberOwner members

            isNotNull memberOwner && isAligned memberOwner members
        // override {selfId}.{caret}
        | TokenType FSharpTokenType.DOT _ ->
            let isCorrectIndent (memberOwner: ITreeNode) (memberDecl: IMemberDeclaration) =
                memberDecl.Indent >= memberOwner.Indent

            let isAligned (memberOwner: ITreeNode) (memberDecl: IMemberDeclaration) =
                isInsideOwnerBody memberOwner && isCorrectIndent memberOwner memberDecl
                
            let isOverrideDecl (memberOwner: ITreeNode) (memberDecl: IMemberDeclaration) =
                let memberKeyword = memberDecl.MemberKeyword
                match memberKeyword with
                | TokenType FSharpTokenType.OVERRIDE _ -> true
                | TokenType FSharpTokenType.MEMBER _ -> memberOwner :? IInterfaceImplementation
                | _ -> false

            let anchor = generatorContext.Anchor

            let memberDecl =
                match anchor with
                | :? IMemberDeclaration as decl -> decl
                | _ -> anchor.GetContainingNode<IMemberDeclaration>()

            isNotNull memberDecl
            && isNotNull memberOwner
            && isOverrideDecl memberOwner memberDecl
            && isAligned memberOwner memberDecl
        | _ -> false

    let inline getMemberOwner context =
        (getMemberOwnerAndItsMembers context >> fst)

    let isOverrideRuleAvailable (checkNode: ITreeNode -> bool) (checkOwner: ITreeNode -> bool) context =
        let generatorContext = getGeneratorContext context
        let node = context.NodeInFile

        checkNode node
        && isNotNull generatorContext
        && isNotNull generatorContext.TypeDeclaration
        && checkOwner (getMemberOwner context generatorContext)
        && mayGenerateOverrides context generatorContext node

    let getOverridableElements (generatorContext: FSharpGeneratorContext) =
        GenerateOverrides.getOverridableMembers false generatorContext.TypeDeclaration
        |> GenerateOverrides.sanitizeMembers

    let createOverrideLookupItem
        (context: FSharpCodeCompletionContext)
        (getPresentationText: IOverridableMember -> string)
        (generatorElement: FSharpGeneratorElement)
        (info: TextualInfo)
        types
        =
        let iconManager = context.NodeInFile.GetSolution().GetComponent<PsiIconManager>()
        let elementMember = generatorElement.Member
        let accessor = elementMember.As<IAccessor>()

        let mainMember =
            let owner = if isNotNull accessor then accessor.OwnerMember else null
            if isNotNull owner then owner else elementMember

        let icon = iconManager.GetImage(mainMember, context.NodeInFile.Language, true)

        let presentationText = getPresentationText mainMember

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

    let addOverrideItems
        (context: FSharpCodeCompletionContext)
        (collector: IItemsCollector)
        (getPresentationText: IOverridableMember -> string)
        (getTextualInfo: IMemberDeclaration -> TextualInfo)
        =
        let generatorContext = getGeneratorContext context

        let mayHaveBaseCalls =
            GenerateOverrides.mayHaveBaseCalls generatorContext.TypeDeclaration

        let generatorElements = getOverridableElements generatorContext

        for generatorElement in generatorElements do
            let memberDecl, types =
                GenerateOverrides.generateMember context.NodeInFile mayHaveBaseCalls generatorElement

            let info = getTextualInfo memberDecl

            let overrideItem =
                createOverrideLookupItem context getPresentationText generatorElement info types

            collector.Add(overrideItem)

        false

open OverrideRuleModule

[<Language(typeof<FSharpLanguage>)>]
type OverrideMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable isWhitespace (fun owner -> not (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        addOverrideItems context collector (fun mainMember -> $"override {mainMember.ShortName}") (fun memberDecl ->
            let text = memberDecl.GetText()
            TextualInfo(text, text, Ranges = context.Ranges))

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext

[<Language(typeof<FSharpLanguage>)>]
type QualifiedOverrideRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable isDot (fun owner -> not (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        addOverrideItems context collector _.ShortName (fun memberDecl ->
            let fullText = memberDecl.GetText()
            let selfId = memberDecl.SelfId.GetText()
            let text = fullText.RemoveStart($"{memberDecl.MemberKeyword.GetText()} {selfId}.")
            TextualInfo(text, text, Ranges = context.Ranges))

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
