namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.EditorServices
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
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Pointers
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText

module ImportInfo =
    let [<Literal>] Id = "Import rule"


type ImportDeclaredElementInfo(declaredElementPointer: IDeclaredElementPointer<IClrDeclaredElement>, text) =
    inherit TextualInfo(text, text)

    new (declaredElement: IClrDeclaredElement, text) =
        let elementPointer = declaredElement.CreateElementPointer()
        ImportDeclaredElementInfo(elementPointer, text)

    member this.DeclaredElement =
        declaredElementPointer.FindDeclaredElement()

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text


type ImportDeclaredElementBehavior(info: ImportDeclaredElementInfo) =
    inherit TextualBehavior<ImportDeclaredElementInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let referenceOwner = TextControlToPsi.GetElement<IFSharpReferenceOwner>(solution, nameRange.EndOffset)
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

        let element = reference.GetElement()
        let psiServices = element.GetPsiServices()
        let solution = psiServices.Solution
        let assemblyReaderShim = solution.GetComponent<IFcsAssemblyReaderShim>()
        let iconManager = solution.GetComponent<PsiIconManager>()
        let autoOpenCache = solution.GetComponent<FSharpAutoOpenCache>()

        let symbolScope = getSymbolScope context.PsiModule false
        let typeElements =
            symbolScope.GetAllTypeElementsGroupedByName()
            |> Seq.filter (fun typeElement -> assemblyReaderShim.IsKnownModule(typeElement.Module)) 

        // todo: try to use nodes from sandbox for better parser recovery
        let fsFile = referenceOwner.GetContainingFileThroughSandBox().As<IFSharpFile>()
        if isNull fsFile then false else

        let openedModulesProvider = OpenedModulesProvider(element.FSharpFile, autoOpenCache)
        let scopes = openedModulesProvider.OpenedModuleScopes

        for typeElement in typeElements do
            if isNotNull (typeElement.GetContainingType()) then () else

            // todo: check scope ranges
            let ns = typeElement.GetContainingNamespace().QualifiedName
            if scopes.ContainsKey(ns) then () else

            let name = typeElement.ShortName
            let info = ImportDeclaredElementInfo(typeElement, name, Ranges = context.Ranges)
            let item =
                // todo: allow calculating icon extensions (now disabled because of the slow unit test icons)
                let icon = iconManager.GetImage(typeElement, context.Language, false)
                let item = ImportRule.createItem info name ns icon
                item
                    .WithBehavior(fun _ -> ImportDeclaredElementBehavior(info))
                    .WithRelevance(CLRLookupItemRelevance.NotImportedType)
            collector.Add(item)

            collector.Add(item)

        true
