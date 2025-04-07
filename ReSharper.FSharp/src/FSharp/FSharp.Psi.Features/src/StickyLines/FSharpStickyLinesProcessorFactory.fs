namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StickyLines

open JetBrains.ReSharper.Feature.Services.StickyLines.Processor
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

type FSharpStickyLinesProcessor() =
    inherit TreeNodeVisitor<StickyLinesConsumer>()

    let addRegion (context: StickyLinesConsumer) debugText (getStartNode: #ITreeNode -> ITreeNode) (node: #ITreeNode) =
        let range =
            let wholeRange = node.GetDocumentRange()
            match getStartNode node with
            | null -> wholeRange
            | startNode ->

            let startOffset = startNode.GetDocumentStartOffset()
            wholeRange.SetStartTo(&startOffset)

        context.AddStickyScrollRegion(range, debugText)

    let addRegions (context: StickyLinesConsumer) debugText getNodeStart (nodes: TreeNodeEnumerable<_>) =
        Seq.iter (addRegion context debugText getNodeStart) nodes

    interface IStickyLinesProcessor with
        member this.InteriorShouldBeProcessed(_, _) = true
        member this.IsProcessingFinished _ = false
        member this.ProcessAfterInterior(_, _) = ()

        member this.ProcessBeforeInterior(element, context) =
            let fsTreeNode = element.As<IFSharpTreeNode>()
            if isNotNull fsTreeNode then
                fsTreeNode.Accept(this, context)

    override this.VisitNestedModuleDeclaration(moduleDecl, context) =
        moduleDecl |> addRegion context "nestedModule" _.NameIdentifier

    override this.VisitTypeDeclarationGroup(typeDeclGroup, context) =
        typeDeclGroup.TypeDeclarationsEnumerable |> addRegions context "typeDecl" _.NameIdentifier

    override this.VisitMemberDeclaration(memberDecl, context) =
        memberDecl |> addRegion context "memberDecl" _.NameIdentifier

    override this.VisitAutoPropertyDeclaration(autoPropDecl, context) =
        autoPropDecl |> addRegion context "autoPropDecl" _.NameIdentifier

    override this.VisitAccessorDeclaration(accessorDecl, context) =
        accessorDecl |> addRegion context "accessorDecl" _.Identifier

    override this.VisitTypeExtensionDeclaration(typeDecl, context) =
        typeDecl |> addRegion context "typeExtension" _.NameIdentifier

    override this.VisitInterfaceImplementation(interfaceImpl, context) =
        interfaceImpl |> addRegion context "interfaceImpl" _.NameIdentifier

    override this.VisitLetBindingsDeclaration(bindings, context) =
        bindings.BindingsEnumerable |> addRegions context "binding" _.HeadPattern

    override this.VisitLetOrUseExpr(letExpr, context) =
        letExpr.BindingsEnumerable |> addRegions context "binding" _.HeadPattern

    override this.VisitUnionCaseDeclaration(unionCaseDecl, context) =
        unionCaseDecl |> addRegion context "unionCaseDecl" _.NameIdentifier

    override this.VisitObjExpr(objExpr, context) =
        objExpr |> addRegion context "binding" _.NameIdentifier

    override this.VisitForEachExpr(forExpr, context) =
        forExpr |> addRegion context "forExpr" _.Pattern

    override this.VisitForExpr(forExpr, context) =
        forExpr |> addRegion context "forExpr" _.Identifier

    override this.VisitLambdaExpr(lambdaExpr, context) =
        lambdaExpr |> addRegion context "lambdaExpr" _.Parameters

    override this.VisitMatchExpr(matchExpr, context) =
        matchExpr |> addRegion context "matchExpr" _.FirstChild

    override this.VisitMatchLambdaExpr(matchLambdaExpr, context) =
        matchLambdaExpr |> addRegion context "matchLambdaExpr" _.FirstChild

    override this.VisitIfThenElseExpr(ifExpr, context) =
        ifExpr |> addRegion context "ifExpr" _.FirstChild

        let elseExpr = ifExpr.ElseExpr
        if isNotNull elseExpr then
            elseExpr |> addRegion context "elseExpr" (fun _ -> ifExpr.ElseKeyword)

    override this.VisitElifExpr(elifExpr, context) =
        elifExpr |> addRegion context "elifExpr" (fun _ -> elifExpr.IfKeyword)

    override this.VisitMatchClause(matchClause, context) =
        matchClause |> addRegion context "matchClause" _.Pattern

    override this.VisitSecondaryConstructorDeclaration(secondaryCtorDecl, context) =
        secondaryCtorDecl |> addRegion context "secondaryCtorDecl" _.NewKeyword

    override this.VisitWhileExpr(whileExpr, context) =
        whileExpr |> addRegion context "whileExpr" _.ConditionExpr


[<Language(typeof<FSharpLanguage>)>]
type FSharpStickyLinesProcessorFactory() =
    interface IStickyLinesProcessorFactory with
        member this.CreateProcessor() = FSharpStickyLinesProcessor()
