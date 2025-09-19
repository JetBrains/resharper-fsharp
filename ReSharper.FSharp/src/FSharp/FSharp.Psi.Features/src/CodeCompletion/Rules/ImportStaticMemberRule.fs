namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open JetBrains.Application
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText

[<Language(typeof<FSharpLanguage>)>]
type ImportStaticMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getReference (context: FSharpCodeCompletionContext) =
        context.ReparsedContext.Reference.As<FSharpSymbolReference>()

    override this.SupportedEvaluationMode = EvaluationMode.Full

    override this.IsAvailable(context) =
        let reference = getReference context
        FSharpImportStaticMemberUtil.isAvailable reference

    override this.AddLookupItems(context, collector) =
        let reference = getReference context
        let referenceOwner = reference.GetElement()
        let accessContext = FSharpAccessContext(referenceOwner)

        let language = context.Language
        let iconManager = context.BasicContext.Solution.GetComponent<PsiIconManager>()

        let seenNamesAndNs = HashSet()
        let openedModulesProvider = OpenedModulesProvider(referenceOwner)

        let qualifierReferenceOwner = reference.QualifierReference.GetElement().As<IFSharpReferenceOwner>()
        if isNull qualifierReferenceOwner then false else

        let qualifierReferenceOwner = qualifierReferenceOwner.TryGetOriginalNodeThroughSandBox()
        if isNull qualifierReferenceOwner then false else

        let typeElements = FSharpImportStaticMemberUtil.getTypeElements None qualifierReferenceOwner.Reference
        for typeElement in typeElements do
            Interruption.Current.CheckAndThrow()

            if typeElement :? IFSharpModule then () else
            if FSharpImportUtil.isTypeElementInScope openedModulesProvider typeElement then () else

            let qualifiedName = typeElement.GetQualifiedName()
            for typeMember in typeElement.GetMembers() do
                if typeMember.IsStatic && AccessUtil.IsSymbolAccessible(typeMember, accessContext) then
                    let name = typeMember.GetSourceName()
                    let info = ImportDeclaredElementInfo(typeMember, name, qualifiedName, context, Ranges = context.Ranges)
                    let item =
                        LookupItemFactory.CreateLookupItem(info)
                            .WithPresentation(fun _ ->
                                let name = RichText(name)
                                LookupUtil.AddInformationText(name, $"(in {qualifiedName})")
                                TextualPresentation(name, info, iconManager.GetImage(typeMember, language, true)))
                            .WithBehavior(fun _ -> ImportDeclaredElementBehavior(info))
                            .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                            .WithRelevance(CLRLookupItemRelevance.ImportedType)

                    if seenNamesAndNs.Add((name, qualifiedName)) then
                        collector.Add(item)

        false
