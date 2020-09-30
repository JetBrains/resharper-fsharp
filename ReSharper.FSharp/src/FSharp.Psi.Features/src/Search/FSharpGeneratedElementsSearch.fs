namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open System.Collections.Generic
open JetBrains.Application
open JetBrains.Application.DataContext
open JetBrains.ReSharper.Features.Navigation.Features.FindUsages
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.Util

[<ShellFeaturePart>]
type FSharpGeneratedElementsSearch() =
    inherit FindUsagesContextSearch()

    override x.IsContextApplicable(dataContext: IDataContext) =
        match dataContext.GetData(PsiDataConstants.DECLARED_ELEMENTS) with
        | null -> false
        | elements -> elements |> Seq.forall (fun el -> el :? IFSharpGeneratedFromUnionCase)

    override x.GetElementCandidates(context: IDataContext, _, _) =
        match context.GetData(PsiDataConstants.DECLARED_ELEMENTS) with
        | null -> EmptyList.Instance :> _
        | elements ->

        let result = List()
        for element in elements do
            match element.As<IFSharpGeneratedFromUnionCase>() with
            | null -> ()
            | generated -> result.Add(DeclaredElementInstance(generated.OriginElement))
        result :> _
