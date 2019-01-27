namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpItemOccurrenceKind() =
    static member val Pattern = OccurrenceKind("Pattern", OccurrenceKind.SemanticAxis)
    static member val TypeSpecification = OccurrenceKind("Type specification", OccurrenceKind.SemanticAxis)

[<SolutionComponent>]
type FSharpItemOccurenceKindProvider() =
    interface IOccurrenceKindProvider with
        member x.GetOccurrenceKinds(occurrence: IOccurrence) =
            match occurrence with
            | :? ReferenceOccurrence as referenceOccurrence ->
                match referenceOccurrence.PrimaryReference with
                | :? FSharpSymbolReference as symbolReference ->

                    // todo: mark synType nodes
                    let referenceNode = symbolReference.GetTreeNode()
                    if isNotNull referenceNode && isNotNull (referenceNode.GetContainingNode<IIsInstPat>()) then
                        [| CSharpSpecificOccurrenceKinds.TypeChecking |] :> _ else

                    let symbolUse = symbolReference.GetSymbolUse()
                    if isNull (box symbolUse) then EmptyList.Instance :> _ else

                    if symbolUse.IsFromType then [| FSharpItemOccurrenceKind.TypeSpecification |] :> _ else
                    if symbolUse.IsFromPattern then [| FSharpItemOccurrenceKind.Pattern |] :> _ else

                    match symbolUse.Symbol with
                    | :? FSharpUnionCase -> [| OccurrenceKind.NewInstanceCreation |] :> _

                    | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsConstructor ->
                        [| OccurrenceKind.NewInstanceCreation |] :> _

                    | _ -> EmptyList.Instance :> _
                | _ -> EmptyList.Instance :> _
            | _ -> EmptyList.Instance :> _

        member x.GetAllPossibleOccurrenceKinds() =
            [| OccurrenceKind.NewInstanceCreation
               FSharpItemOccurrenceKind.Pattern
               FSharpItemOccurrenceKind.TypeSpecification
               CSharpSpecificOccurrenceKinds.TypeChecking |] :> _
