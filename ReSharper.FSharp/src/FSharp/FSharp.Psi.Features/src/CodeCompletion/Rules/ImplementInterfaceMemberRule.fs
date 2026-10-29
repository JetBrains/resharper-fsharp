namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules.OverrideMemberRule
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

type ImplementAllMembersBehavior(info, context: FSharpCodeCompletionContext) =
    inherit TextualBehavior<TextualInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        use pinCheckResultsCookie =
            Assertion.Assert(context.ParseAndCheckResults.IsValueCreated)
            textControl.GetFSharpFile(solution).PinTypeCheckResults(context.ParseAndCheckResults.Value)

        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let node = textControl.Document.GetPsiSourceFile(solution).FSharpFile.FindNodeAt(nameRange)
        let memberDecl = node.NextSibling
        let interfaceImpl = node.GetContainingNode<IInterfaceImplementation>() |> Option.ofObj
        let typeDecl = node.GetContainingNode<IFSharpTypeElementDeclaration>()

        let generatorElements = GenerateOverrides.generateInterfaceMembers typeDecl interfaceImpl

        let memberDecls =
            use writeCookie = WriteLockCookie.Create(memberDecl.IsPhysical())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, nameof ImplementAllMembersBehavior)

            let decls = GenerateOverrides.addMembers generatorElements typeDecl memberDecl
            let deleteRangeStart =
                skipMatchingNodesAfter isInlineSpaceOrComment (memberDecl.GetPreviousMeaningfulSibling())
            ModificationUtil.DeleteChildRange(deleteRangeStart, memberDecl)
            decls

        let expr = GenerateOverrides.getGeneratedBodyToSelect memberDecls
        if isNotNull expr then
            let range = expr.GetDocumentRange()
            textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
            textControl.Selection.SetRange(range)

[<Language(typeof<FSharpLanguage>)>]
type ImplementInterfaceMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let [<Literal>] Relevance =
        CLRLookupItemRelevance.GenerateItems
        ||| CLRLookupItemRelevance.ExpectedTypeMatch

    let getInterfaceTypeName (typeDecl: IFSharpTypeElementDeclaration) (interfaceImpl: IInterfaceImplementation option) =
        match interfaceImpl with
        | Some impl -> impl.TypeName.GetText()
        | None ->
            match typeDecl with
            | :? IObjExpr as objExpr -> objExpr.TypeName.GetText()
            | _ -> "???"

    let createImplementAllMembersItem
        (context: FSharpCodeCompletionContext)
        (typeDecl: IFSharpTypeElementDeclaration)
        (interfaceImpl: IInterfaceImplementation option) =
        let text = $"Implement {getInterfaceTypeName typeDecl interfaceImpl}"
        let info = TextualInfo("member _", text, Ranges = context.Ranges)

        LookupItemFactory
            .CreateLookupItem(info)
            .WithPresentation(fun _ -> TextualPresentation(RichText(text), info))
            .WithBehavior(fun _ -> ImplementAllMembersBehavior(info, context))
            .WithTextToMatch(text)
            .WithRelevance(Relevance)

    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable GeneratorStandardKinds.MissingMembers

    override this.AddLookupItems(context, collector) =
        let generatorContext = getGeneratorContext context
        let typeDecl = generatorContext.TypeDeclaration
        let interfaceImpl =
            (getMemberOwner context generatorContext).As<IInterfaceImplementation>()
            |> Option.ofObj

        let generatorElements = GenerateOverrides.generateInterfaceMembers typeDecl interfaceImpl

        for generatorElement in generatorElements do

            let item =
                createOverrideLookupItem context generatorContext generatorElement false

            collector.Add(item)

        if isWhitespace context.NodeInFile && generatorElements |> (Seq.tryHead >> _.IsSome) then
            context.ParseAndCheckResults.Value |> ignore
            let implAllItem = createImplementAllMembersItem context typeDecl interfaceImpl
            implAllItem.Placement.Location <- PlacementLocation.Top

            collector.Add(implAllItem)

        false

    override this.TransformItems(context, collector) =
        collector |> keepOnlyOverrideItems (fun item ->
            match item with
            | :? IAspectLookupItemBase as aspectItem ->
                aspectItem.Behavior :? ImplementAllMembersBehavior
            | _ -> false)
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
