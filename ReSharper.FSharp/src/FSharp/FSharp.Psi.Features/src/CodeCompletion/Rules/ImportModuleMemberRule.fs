namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type ImportModuleMemberInfo(typeElement: ITypeElement, name: string, context: FSharpCodeCompletionContext) =
    inherit TextualInfo(name, name)

    member this.TypeElement = typeElement

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text

    interface IDescriptionProvidingLookupItem with
        member this.GetDescription() =
            let typeNames = toSourceNameList typeElement
            let names = typeNames @ [name]
            let treeNode = context.NodeInFile.As<IFSharpTreeNode>()
            if isNull treeNode then null else

            match treeNode.CheckerService.ResolveNameAtLocation(treeNode, names, true, "ImportModuleMemberInfo.GetDescription") with
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

type ImportModuleMemberBehavior(info: ImportModuleMemberInfo) =
    inherit TextualBehavior<ImportModuleMemberInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let referenceOwner = TextControlToPsi.GetElement<IFSharpReferenceOwner>(solution, nameRange.EndOffset)
        use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())
        use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, ImportInfo.Id)

        let typeElement = info.TypeElement
        let moduleToImport = ModuleToImport.DeclaredElement(getModuleToOpenFromContainingType typeElement)
        addOpen (referenceOwner.GetDocumentStartOffset()) referenceOwner.FSharpFile moduleToImport
        referenceOwner.Reference.SetRequiredQualifiersForContainingType(typeElement, referenceOwner)


[<Language(typeof<FSharpLanguage>)>]
type ImportModuleMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let shouldIgnore (typeElement: ITypeElement) (ns: string) =
        ns.StartsWith("Microsoft.FSharp.", StringComparison.Ordinal) &&
        typeElement.Module.Name = "FSharp.Core"

    override this.IsAvailable(context) =
        not context.IsQualified &&
        context.ReparsedContext.Reference :? FSharpSymbolReference &&

        context.BasicContext.Solution.GetComponent<IFcsAssemblyReaderShim>().IsEnabled

    override this.AddLookupItems(context, collector) =
        let reference = context.ReparsedContext.Reference :?> FSharpSymbolReference
        let referenceOwner = reference.GetElement()

        let referenceContext = referenceOwner.ReferenceContext
        if not referenceContext.HasValue then false else

        let fsFile = referenceOwner.GetContainingFileThroughSandBox().As<IFSharpFile>()
        if isNull fsFile then false else

        let autoOpenCache = referenceOwner.GetPsiServices().Solution.GetComponent<FSharpAutoOpenCache>()
        let scopes = OpenedModulesProvider(fsFile, autoOpenCache).OpenedModuleScopes
        let symbolScope = getSymbolScope context.PsiModule false

        let values =
            match referenceContext.Value with
            | FSharpReferenceContext.Expression ->
                let treeNode = context.ReparsedContext.TreeNode
                context.GetOrCreateDataUnderLock(LocalValuesRule.valuesKey, treeNode, LocalValuesRule.getLocalValues)

            | _ -> EmptyDictionary.Instance

        for typeElement in symbolScope.GetAllTypeElementsGroupedByName() do
            let fsTypeElement = typeElement.As<IFSharpTypeElement>()
            if isNull fsTypeElement || fsTypeElement.RequiresQualifiedAccess() then () else

            // todo: check scope ranges
            // todo: better check if imported

            let ns =
                match fsTypeElement with
                | :? IFSharpModule as fsModule ->
                    Some fsModule.QualifiedSourceName

                | _ ->
                    match fsTypeElement.GetContainingType() with
                    | :? IFSharpModule as fsModule ->
                        Some(fsModule.QualifiedSourceName + "." + fsTypeElement.SourceName)

                    | _ ->
                        None

            match ns with
            | None -> ()
            | Some ns when scopes.ContainsKey(ns) || shouldIgnore typeElement ns -> ()
            | Some ns ->

            let members =
                let addIcon icon names =
                    names |> Array.map (fun name -> name, icon)

                match fsTypeElement with
                | :? IFSharpModule as fsModule ->
                    [|
                        match referenceContext.Value with
                        | FSharpReferenceContext.Expression ->
                            yield! addIcon PsiSymbolsThemedIcons.Const.Id fsModule.LiteralNames
                            yield! addIcon PsiSymbolsThemedIcons.Property.Id fsModule.ValueNames
                            yield! addIcon PsiSymbolsThemedIcons.Method.Id fsModule.FunctionNames

                        | FSharpReferenceContext.Pattern ->
                            yield! addIcon PsiSymbolsThemedIcons.Const.Id fsModule.LiteralNames
                            yield! addIcon PsiSymbolsThemedIcons.Method.Id fsModule.ActivePatternCaseNames

                        | _ -> ()
                    |]

                | _ ->
                    fsTypeElement.GetUnionCaseNames()
                    |> addIcon PsiSymbolsThemedIcons.EnumMember.Id

            for name, icon in members do
                if values.ContainsKey(name) then () else

                let info = ImportModuleMemberInfo(typeElement, name, context, Ranges = context.Ranges)
                let item = ImportRule.createItem info name ns icon
                let item =
                    item
                        .WithBehavior(fun _ -> ImportModuleMemberBehavior(info))
                        .WithRelevance(CLRLookupItemRelevance.NotImportedType)

                collector.Add(item)

        true
