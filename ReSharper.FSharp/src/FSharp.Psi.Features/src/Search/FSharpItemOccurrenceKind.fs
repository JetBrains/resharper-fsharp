namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open FSharp.Compiler.SourceCodeServices
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

type FSharpItemOccurrenceKind() =
    static member val Import = OccurrenceKind("Module or namespace import", OccurrenceKind.SemanticAxis)
    static member val Pattern = OccurrenceKind("Pattern", OccurrenceKind.SemanticAxis)
    static member val TypeSpecification = OccurrenceKind("Type specification", OccurrenceKind.SemanticAxis)
    static member val TypeExtension = OccurrenceKind("Type extension", OccurrenceKind.SemanticAxis)
    static member val CopyAndUpdate = OccurrenceKind("Copy and update", OccurrenceKind.SemanticAxis)


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

        null

    /// Is checked when IsPattern, IsFromType, etc are false.
    let isInstanceCreation (node: ITreeNode) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.IsConstructor
        | :? FSharpUnionCase -> true

        | :? FSharpEntity as entity when entity.IsFSharpExceptionDeclaration ->
            match node.As<ITokenNode>() with
            | null -> false
            | tokenNode ->

            match node.GetContainingNode<ILongIdentifier>() with
            | null -> true
            | longIdentifier -> longIdentifier.IdentifierToken == tokenNode

        | _ -> false

    interface IOccurrenceKindProvider with
        member x.GetOccurrenceKinds(occurrence: IOccurrence) =
            match occurrence.As<ReferenceOccurrence>() with
            | null -> EmptyList.Instance :> _
            | referenceOccurrence ->

            match referenceOccurrence.PrimaryReference with
            | :? AttributeTypeReference -> [| OccurrenceKind.Attribute |] :> _
            | :? OpenStatementReference -> [| FSharpItemOccurrenceKind.Import |] :> _
            | :? TypeExtensionReference -> [| FSharpItemOccurrenceKind.TypeExtension |] :> _
//            | :? BaseTypeReference -> [| OccurrenceKind.ExtendedType |] :> _

            | :? RecordCtorReference as recordCtorReference ->
                match recordCtorReference.RecordExpr.CopyInfoExpression with
                | null -> [| OccurrenceKind.NewInstanceCreation |] :> _
                | _ ->    [| FSharpItemOccurrenceKind.CopyAndUpdate |] :> _

            | reference ->

            match reference.As<FSharpSymbolReference>() with
            | null -> EmptyList.Instance :> _
            | symbolReference ->

            let symbolUse = symbolReference.GetSymbolUse()
            if isNull (box symbolUse) then EmptyList.Instance :> _ else

            let isFromType = symbolUse.IsFromType
            let node = symbolReference.GetTreeNode()

            let kind =
                if not isFromType then null else
                getTypeUsageKind node

            if isNotNull kind then [| kind |] :> _ else
            if isFromType then [| FSharpItemOccurrenceKind.TypeSpecification |] :> _ else
            if symbolUse.IsFromPattern then [| FSharpItemOccurrenceKind.Pattern |] :> _ else
            if isInstanceCreation node symbolUse.Symbol then [| OccurrenceKind.NewInstanceCreation |] :> _ else

            EmptyList.Instance :> _

        member x.GetAllPossibleOccurrenceKinds() =
            [| OccurrenceKind.Attribute
               OccurrenceKind.ExtendedType
               OccurrenceKind.NewInstanceCreation
               FSharpItemOccurrenceKind.Import
               FSharpItemOccurrenceKind.Pattern
               FSharpItemOccurrenceKind.TypeExtension
               FSharpItemOccurrenceKind.TypeSpecification
               CSharpSpecificOccurrenceKinds.TypeArgument
               CSharpSpecificOccurrenceKinds.TypeChecking |] :> _
