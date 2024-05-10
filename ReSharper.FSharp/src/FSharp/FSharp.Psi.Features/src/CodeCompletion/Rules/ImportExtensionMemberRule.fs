namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.UI.RichText
open JetBrains.Util.Extension

[<Language(typeof<FSharpLanguage>)>]
type ImportExtensionMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getQualifierExpr (context: FSharpCodeCompletionContext) =
        FSharpExtensionMemberUtil.getQualifierExpr context.ReparsedContext.Reference

    override this.IsAvailable(context) =
        context.IsQualified &&

        let qualifierExpr = getQualifierExpr context
        isNotNull qualifierExpr &&

        let fcsType = qualifierExpr.TryGetFcsType()
        isNotNull fcsType

    override this.AddLookupItems(context, collector) =
        let qualifierExpr = getQualifierExpr context
        let fcsType = qualifierExpr.TryGetFcsType()
        let members = FSharpExtensionMemberUtil.getExtensionMembers qualifierExpr fcsType

        let iconManager = context.BasicContext.Solution.GetComponent<PsiIconManager>()

        for method in members do
            let name = method.ShortName.SubstringAfter("get_").SubstringAfter("set_")

            let ns =
                match method.ContainingType with
                | :? IFSharpModule as fsModule ->
                    fsModule.QualifiedSourceName

                | containingType ->
                    containingType.GetContainingNamespace().QualifiedName 

            let info = ImportInfo(method, name, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let name = RichText(name)
                        LookupUtil.AddInformationText(name, $"(in {ns})")
                        TextualPresentation(name, info, iconManager.GetImage(method, method.PresentationLanguage, true)))
                    .WithBehavior(fun _ -> ImportBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.ImportedType)

            collector.Add(item)

        false
