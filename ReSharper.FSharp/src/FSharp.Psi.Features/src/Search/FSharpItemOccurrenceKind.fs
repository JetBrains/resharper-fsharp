namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpItemOccurrenceKind() =
    static member val Import = OccurrenceKind("Module or namespace import", OccurrenceKind.SemanticAxis)
    static member val Pattern = OccurrenceKind("Pattern", OccurrenceKind.SemanticAxis)
    static member val TypeSpecification = OccurrenceKind("Type specification", OccurrenceKind.SemanticAxis)


[<SolutionComponent>]
type FSharpItemOccurenceKindProvider() =
    interface IOccurrenceKindProvider with
        member x.GetOccurrenceKinds(occurrence: IOccurrence) =
            match occurrence.As<ReferenceOccurrence>() with
            | null -> EmptyList.Instance :> _
            | referenceOccurrence ->

            match referenceOccurrence.PrimaryReference.As<FSharpSymbolReference>() with
            | null -> EmptyList.Instance :> _
            | symbolReference ->

            let referenceNode = symbolReference.GetTreeNode()
            if isNotNull referenceNode && isNotNull (referenceNode.GetContainingNode<ITypeArgumentList>()) then
                [| CSharpSpecificOccurrenceKinds.TypeArgument |] :> _ else

            if isNotNull referenceNode && isNotNull (referenceNode.GetContainingNode<IIsInstPat>()) then
                [| CSharpSpecificOccurrenceKinds.TypeChecking |] :> _ else

            if isNotNull referenceNode &&
               (isNotNull (referenceNode.GetContainingNode<ITypeInherit>()) ||
                isNotNull (referenceNode.GetContainingNode<IInterfaceInherit>())) then
                [| OccurrenceKind.ExtendedType |] :> _ else

            let symbolUse = symbolReference.GetSymbolUse()
            if isNull (box symbolUse) then EmptyList.Instance :> _ else

            if symbolUse.IsFromType then [| FSharpItemOccurrenceKind.TypeSpecification |] :> _ else
            if symbolUse.IsFromPattern then [| FSharpItemOccurrenceKind.Pattern |] :> _ else
            if symbolUse.IsFromOpenStatement then [| FSharpItemOccurrenceKind.Import |] :> _ else

            match symbolUse.Symbol with
            | :? FSharpUnionCase -> [| OccurrenceKind.NewInstanceCreation |] :> _

            | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsConstructor ->
                [| OccurrenceKind.NewInstanceCreation |] :> _

            | _ -> EmptyList.Instance :> _

        member x.GetAllPossibleOccurrenceKinds() =
            [| OccurrenceKind.ExtendedType
               OccurrenceKind.NewInstanceCreation
               FSharpItemOccurrenceKind.Import
               FSharpItemOccurrenceKind.Pattern
               FSharpItemOccurrenceKind.TypeSpecification
               CSharpSpecificOccurrenceKinds.TypeArgument
               CSharpSpecificOccurrenceKinds.TypeChecking |] :> _
