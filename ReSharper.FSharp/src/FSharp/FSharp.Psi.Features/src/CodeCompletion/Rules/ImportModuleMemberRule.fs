namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System
open FSharp.Compiler.EditorServices
open JetBrains.Application
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
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
            ImportInfo.getDescription context typeElement (Some name) true

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

    let shouldIgnore (ns: string) =
        ns.StartsWith("Microsoft.FSharp.", StringComparison.Ordinal)

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

        match reparsedContext.GetFcsContext().CompletionContext with
        | Some(CompletionContext.Invalid) -> false
        | _ ->

        let referenceContext = referenceOwner.ReferenceContext
        if not referenceContext.HasValue then false else

        let openedModulesProvider = OpenedModulesProvider(referenceOwner)
        let symbolScope = getSymbolScope context.PsiModule false

        let values =
            match referenceContext.Value with
            | FSharpReferenceContext.Expression ->
                let treeNode = reparsedContext.TreeNode
                context.GetOrCreateDataUnderLock(LocalValuesUtil.valuesKey, treeNode, LocalValuesUtil.getLocalValues)

            | _ -> EmptyDictionary.Instance

        for typeElement in symbolScope.GetAllTypeElementsGroupedByName() do
            Interruption.Current.CheckAndThrow()

            if FSharpImportUtil.areMembersInScope openedModulesProvider typeElement then () else

            let fsTypeElement = typeElement.As<IFSharpTypeElement>()
            if isNull fsTypeElement || fsTypeElement.RequiresQualifiedAccess() then () else

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
            | Some ns when shouldIgnore ns -> ()
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
                let item = FSharpImportTypeElementRule.createItem info name ns icon
                let item =
                    item
                        .WithBehavior(fun _ -> ImportModuleMemberBehavior(info))
                        .WithRelevance(CLRLookupItemRelevance.NotImportedType)

                collector.Add(item)

        true
