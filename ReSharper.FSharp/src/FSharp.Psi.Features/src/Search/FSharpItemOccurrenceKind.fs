namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpItemOccurrenceKind() =
    static member val Import = OccurrenceKind("Module or namespace import", OccurrenceKind.SemanticAxis)
    static member val Pattern = OccurrenceKind("Pattern", OccurrenceKind.SemanticAxis)
    static member val TypeSpecification = OccurrenceKind("Type specification", OccurrenceKind.SemanticAxis)


[<SolutionComponent>]
type FSharpItemOccurenceKindProvider() =

    let getTypeUsageKind (node: ITreeNode) =
        let typeArgument = node.GetContainingNode<ITypeArgumentList>() 
        if isNotNull typeArgument then CSharpSpecificOccurrenceKinds.TypeArgument else

        let isInstPat = node.GetContainingNode<IIsInstPat>() 
        if isNotNull isInstPat then CSharpSpecificOccurrenceKinds.TypeChecking else

        let typeTest = node.GetContainingNode<ITypeTestExpr>()
        if isNotNull typeTest && node.IsChildOf(typeTest.Type) then CSharpSpecificOccurrenceKinds.TypeChecking else

        let castExpr = node.GetContainingNode<ICastExpr>()
        if isNotNull castExpr && node.IsChildOf(castExpr.Type) then CSharpSpecificOccurrenceKinds.TypeConversions else

        let interfaceInherit = node.GetContainingNode<IInterfaceInherit>()
        if isNotNull interfaceInherit then OccurrenceKind.ExtendedType else

        null
    
    interface IOccurrenceKindProvider with
        member x.GetOccurrenceKinds(occurrence: IOccurrence) =
            match occurrence.As<ReferenceOccurrence>() with
            | null -> EmptyList.Instance :> _
            | referenceOccurrence ->

            match referenceOccurrence.PrimaryReference.As<FSharpSymbolReference>() with
            | null -> EmptyList.Instance :> _
            | symbolReference ->

            let symbolUse = symbolReference.GetSymbolUse()
            if isNull (box symbolUse) then EmptyList.Instance :> _ else

            let isFromType = symbolUse.IsFromType
            let referenceNode = symbolReference.GetTreeNode()

            let kind =
                if not isFromType then null else
                getTypeUsageKind referenceNode

            if isNotNull kind then [| kind |] :> _ else
            if isFromType then [| FSharpItemOccurrenceKind.TypeSpecification |] :> _ else
            if symbolUse.IsFromPattern then [| FSharpItemOccurrenceKind.Pattern |] :> _ else
            if symbolUse.IsFromOpenStatement then [| FSharpItemOccurrenceKind.Import |] :> _ else

            match symbolUse.Symbol with
            | :? FSharpUnionCase -> [| OccurrenceKind.NewInstanceCreation |] :> _

            | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsConstructor ->
                let typeInherit = referenceNode.GetContainingNode<ITypeInherit>()
                if isNotNull typeInherit then [| OccurrenceKind.ExtendedType |] :> _ else
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
