namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText
open JetBrains.Util.Extension

[<Language(typeof<FSharpLanguage>)>]
type ImportExtensionMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.SupportedEvaluationMode = EvaluationMode.LightAndFull

    override this.IsAvailable(context) =
        context.EnableImportCompletion &&
        context.IsQualified &&

        let qualifierExpr = getQualifierExpr context
        isNotNull qualifierExpr &&

        let fcsType = qualifierExpr.TryGetFcsType()
        isNotNull fcsType &&

        match qualifierExpr with
        | :? IReferenceExpr as refExpr ->
            match refExpr.Reference.GetFcsSymbol() with
            | :? FSharpMemberOrFunctionOrValue as mfv -> not mfv.IsConstructor
            | _ -> true
        | _ -> true

    override this.AddLookupItems(context, collector) =
        let qualifierExpr = getQualifierExpr context
        let fcsType = qualifierExpr.TryGetFcsType()
        let members = FSharpExtensionMemberUtil.getExtensionMembers qualifierExpr fcsType None

        let iconManager = context.BasicContext.Solution.GetComponent<PsiIconManager>()

        let members =
            members |> Seq.groupBy (fun typeMember ->
                let name = typeMember.ShortName.SubstringAfterLast(".").SubstringAfter("get_").SubstringAfter("set_")

                let ns =
                    match typeMember.ContainingType with
                    | :? IFSharpModule as fsModule ->
                        fsModule.QualifiedSourceName

                    | containingType ->
                        containingType.GetContainingNamespace().QualifiedName

                ns, name
            )

        for (ns, name), typeMembers in members do
            // todo: use all candidates for signatures
            let typeMember = typeMembers |> Seq.head

            let info = ImportDeclaredElementInfo(typeMember, name, context, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let name = RichText(name)
                        LookupUtil.AddInformationText(name, $"(in {ns})")
                        TextualPresentation(name, info, iconManager.GetImage(typeMember, typeMember.PresentationLanguage, true)))
                    .WithBehavior(fun _ -> ImportDeclaredElementBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.ImportedType)

            collector.Add(item)

        false

[<Language(typeof<FSharpLanguage>)>]
type ImportStaticMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getReference context =
        match getQualifierExpr context with
        | :? IReferenceExpr as refExpr -> refExpr.Reference
        | _ -> null

    override this.SupportedEvaluationMode = EvaluationMode.LightAndFull

    override this.IsAvailable(context) =
        let reference = getReference context
        FSharpImportStaticMemberUtil.isAvailable reference

    // todo: sandboxed node issues?
    override this.AddLookupItems(context, collector) =
        let reference = getReference context
        let accessContext = FSharpAccessContext(reference.GetElement())

        let typeElements = FSharpImportStaticMemberUtil.getTypeElements false reference
        for typeElement in typeElements do
            let ns = getNs typeElement
            for typeMember in typeElement.GetMembers() do
                if typeMember.IsStatic && AccessUtil.IsSymbolAccessible(typeMember, accessContext) then
                    let name = typeMember.ShortName
                    let info = ImportDeclaredElementInfo(typeMember, name, context, Ranges = context.Ranges)
                    let item =
                        LookupItemFactory.CreateLookupItem(info)
                            .WithPresentation(fun _ ->
                                let name = RichText(name)
                                LookupUtil.AddInformationText(name, $"(in {ns})")
                                TextualPresentation(name, info, iconManager.GetImage(typeMember, typeMember.PresentationLanguage, true)))
                            .WithBehavior(fun _ -> ImportDeclaredElementBehavior(info))
                            .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                            .WithRelevance(CLRLookupItemRelevance.ImportedType)

                    collector.Add(item)

        false
