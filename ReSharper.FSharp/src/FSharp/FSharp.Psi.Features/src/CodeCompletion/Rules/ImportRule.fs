namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ProjectModel
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
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText

module ImportInfo =
    let [<Literal>] Id = "Import rule"


type ImportInfo(declaredElementPointer: IDeclaredElementPointer<IClrDeclaredElement>, text) =
    inherit TextualInfo(text, text)

    new (declaredElement: IClrDeclaredElement, text) =
        let elementPointer = declaredElement.CreateElementPointer()
        ImportInfo(elementPointer, text)

    member this.DeclaredElement = declaredElementPointer.FindDeclaredElement()

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text


type ImportBehavior(info: ImportInfo) =
    inherit TextualBehavior<ImportInfo>(info)

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


[<Language(typeof<FSharpLanguage>)>]
type ImportRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        not context.IsQualified &&
        context.ReparsedContext.Reference :? FSharpSymbolReference &&

        context.BasicContext.Solution.GetComponent<IFcsAssemblyReaderShim>().IsEnabled

    override this.AddLookupItems(context, collector) =
        let reference = context.ReparsedContext.Reference :?> FSharpSymbolReference

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

        let openedModulesProvider = OpenedModulesProvider(element.FSharpFile, autoOpenCache)
        let scopes = openedModulesProvider.OpenedModuleScopes

        for typeElement in typeElements do
            if isNotNull (typeElement.GetContainingType()) then () else

            // todo: check scope ranges
            let ns = typeElement.GetContainingNamespace().QualifiedName
            if scopes.ContainsKey(ns) then () else

            let name = typeElement.ShortName
            let info = ImportInfo(typeElement, name, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let name = RichText(name)
                        LookupUtil.AddInformationText(name, $"(in {ns})")
                        TextualPresentation(name, info, iconManager.GetImage(typeElement, element.Language, true)))
                    .WithBehavior(fun _ -> ImportBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.ImportedType)

            collector.Add(item)

        true
