namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.EditorServices
open JetBrains.Application
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Pointers
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util.Extension

[<RequireQualifiedAccess>]
module ImportInfo =
    let [<Literal>] Id = "Import rule"

    let getDescription (context: FSharpCodeCompletionContext) typeElement memberName useExprRules =
        let typeNames = toSourceNameList typeElement
        let names = typeNames @ Option.toList memberName
        let treeNode = context.NodeInFile.As<IFSharpTreeNode>()
        if isNull treeNode then null else

        let sourceFile = treeNode.GetSourceFile()
        let offset = context.BasicContext.CaretDocumentOffset
        let coords = offset.ToDocumentCoords()
        match treeNode.CheckerService.ResolveNameAtLocation(sourceFile, names, coords, useExprRules, "ImportModuleMemberInfo.GetDescription") with
        | None -> null
        | Some fcsSymbolUse ->

        match treeNode.FSharpFile.GetParseAndCheckResults(true, "ImportModuleMemberInfo.GetDescription") with
        | None -> null
        | Some { CheckResults = checkResults } ->

        let _, range = treeNode.TryGetFcsRange()
        let description = checkResults.GetDescription(fcsSymbolUse.Symbol, [], false, range)
        description
        |> FcsLookupCandidate.getOverloads
        |> List.tryHead
        |> Option.map (FcsLookupCandidate.getDescription context.XmlDocService context.PsiModule)
        |> Option.defaultValue null


type ImportDeclaredElementInfo(declaredElementPointer: IDeclaredElementPointer<IClrDeclaredElement>, text, context: FSharpCodeCompletionContext) =
    inherit TextualInfo(text, text)

    new (declaredElement: IClrDeclaredElement, text, context: FSharpCodeCompletionContext) =
        let elementPointer = declaredElement.CreateElementPointer()
        ImportDeclaredElementInfo(elementPointer, text, context)

    member this.DeclaredElement =
        declaredElementPointer.FindDeclaredElement()

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text

    interface IDescriptionProvidingLookupItem with
        member this.GetDescription() =
            ImportInfo.getDescription context this.DeclaredElement None false


type ImportDeclaredElementBehavior(info: ImportDeclaredElementInfo) =
    inherit TextualBehavior<ImportDeclaredElementInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let fsFile = TextControlToPsi.GetElement<IFSharpFile>(solution, textControl)
        let referenceOwner = fsFile.GetNode<IFSharpReferenceOwner>(nameRange.StartOffset)
        use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())
        use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, ImportInfo.Id)

        let declaredElement = info.DeclaredElement
        if isNotNull declaredElement then
            let reference = referenceOwner.Reference
            FSharpBindUtil.bindDeclaredElementToReference referenceOwner reference declaredElement ImportInfo.Id


module ImportRule =
    let createItem info (name: string) ns icon =
        LookupItemFactory.CreateLookupItem(info)
            .WithPresentation(fun _ ->
                let name = RichText(name)
                LookupUtil.AddInformationText(name, $"(in {ns})")

                TextualPresentation(name, info, icon))
            .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)


[<Language(typeof<FSharpLanguage>)>]
type ImportRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let relevance = CLRLookupItemRelevance.TypesAndNamespaces ||| CLRLookupItemRelevance.NotImportedType

    let isNestedType (typeElement: ITypeElement) =
         match typeElement.GetContainingType() with
         | null -> false
         | :? IFSharpModule -> false
         | _ -> true

    let isAllowed (typeElement: ITypeElement) =
        not (isFSharpAssembly typeElement.Module) &&
        not (isNestedType typeElement) &&

        let accessRightsOwner = typeElement.As<IAccessRightsOwner>()
        isNotNull accessRightsOwner && accessRightsOwner.GetAccessRights() = AccessRights.PUBLIC

    let getSourceName (typeElement: ITypeElement) =
         match typeElement with
         | :? IFSharpDeclaredElement as fsDeclaredElement -> fsDeclaredElement.SourceName
         | _ -> typeElement.ShortName

    override this.SupportedEvaluationMode = EvaluationMode.LightAndFull

    override this.IsAvailable(context) =
        context.EnableImportCompletion &&

        not context.IsQualified &&
        context.ReparsedContext.Reference :? FSharpSymbolReference &&

        context.BasicContext.Solution.GetComponent<IFcsAssemblyReaderShim>().IsEnabled

    override this.AddLookupItems(context, collector) =
        let reparsedContext = context.ReparsedContext
        let reference = reparsedContext.Reference :?> FSharpSymbolReference

        let referenceOwner = reference.GetElement()
        if not referenceOwner.ReferenceContext.HasValue then false else

        match reparsedContext.GetFcsContext().CompletionContext with
        | Some(CompletionContext.Invalid) -> false
        | _ ->

        if FSharpCodeCompletionContext.isFullEvaluationDisabled context.BasicContext then false else

        let element = reference.GetElement()
        let psiServices = element.GetPsiServices()
        let solution = psiServices.Solution
        let iconManager = solution.GetComponent<PsiIconManager>()
        let autoOpenCache = solution.GetComponent<FSharpAutoOpenCache>()

        // todo: try to use nodes from sandbox for better parser recovery
        let fsFile = referenceOwner.GetContainingFileThroughSandBox().As<IFSharpFile>()
        if isNull fsFile then false else

        let openedModulesProvider = OpenedModulesProvider(element.FSharpFile, autoOpenCache)
        let scopes = openedModulesProvider.OpenedModuleScopes

        let isAttributeReferenceContext = context.IsInAttributeContext

        let symbolScope = getSymbolScope context.PsiModule true
        
        let getNsQualifiedName =
             let moduleQualifiedNames = Dictionary()
             fun (typeElement: ITypeElement) ->
                 let moduleToOpen: IClrDeclaredElement =
                     match typeElement.GetContainingType() with
                     | :? IFSharpModule as fsModule -> fsModule
                     | _ -> typeElement.GetContainingNamespace()

                 match moduleQualifiedNames.TryGetValue(moduleToOpen) with
                 | true, qualifiedName -> qualifiedName
                 | _ ->
                     match moduleToOpen with
                     | :? IFSharpModule as fsModule -> fsModule.QualifiedSourceName
                     | :? INamespace as ns -> ns.QualifiedName
                     | _ -> moduleToOpen.ShortName

        ("", symbolScope.GetAllTypeElementsGroupedByName())
        ||> Seq.fold (fun prevTypeName typeElement ->
            Interruption.Current.CheckAndThrow();

            if not (isAllowed typeElement) then prevTypeName else

            // todo: check scope ranges
            let ns = typeElement |> getNsQualifiedName
            if scopes.ContainsKey(ns) then prevTypeName else

            let name =
                let shortName = getSourceName typeElement
                if isAttributeReferenceContext then shortName.SubstringBeforeLast("Attribute") else shortName

            if name = prevTypeName then prevTypeName else

            let info = ImportDeclaredElementInfo(typeElement, name, context, Ranges = context.Ranges)
            let item =
                // todo: allow calculating icon extensions (now disabled because of the slow unit test icons)
                let icon = iconManager.GetImage(typeElement, context.Language, false)
                let item = ImportRule.createItem info name ns icon
                item
                    .WithBehavior(fun _ -> ImportDeclaredElementBehavior(info))
                    .WithRelevance(relevance)

            collector.Add(item)
            name
        ) |> ignore

        true
