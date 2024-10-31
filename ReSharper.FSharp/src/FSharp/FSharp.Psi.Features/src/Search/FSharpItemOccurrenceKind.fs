namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open FSharp.Compiler.Symbols
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Occurrences
open JetBrains.ReSharper.Feature.Services.Resources
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<RequireQualifiedAccess>]
module FSharpOccurrenceKinds =
    let copyAndUpdate = OccurrenceKind.CreateSemantic("Copy and update")
    let import = OccurrenceKind.CreateSemantic("Module or namespace import")
    let nonTailRecursiveInvocation = OccurrenceKind.CreateSemantic("Non-tail recursive application")
    let partialApplication = OccurrenceKind.CreateSemantic("Partial application")
    let partialRecursiveApplication = OccurrenceKind.CreateSemantic("Partial recursive application")
    let pattern = OccurrenceKind.CreateSemantic("Pattern")
    let recursiveInvocation = OccurrenceKind.CreateSemantic("Recursive invocation")
    let typeAbbreviation = OccurrenceKind.CreateSemantic("Type abbreviation") // todo: add icon
    let typeExtension = OccurrenceKind.CreateSemantic("Type extension")
    let typeSpecification = OccurrenceKind.CreateSemantic("Type specification")

    let icons =
        [| copyAndUpdate, ServicesNavigationThemedIcons.UsageInstanceCreation.Id
           import, ServicesNavigationThemedIcons.UsageInUsings.Id
           nonTailRecursiveInvocation, ServicesNavigationThemedIcons.UsageRecursionProblematic.Id
           partialApplication, FSharpIcons.UsagePartialApplication.Id
           partialRecursiveApplication, ServicesNavigationThemedIcons.UsageRecursionPartialCall.Id
           pattern, ServicesNavigationThemedIcons.UsagePatternChecking.Id
           recursiveInvocation, ServicesNavigationThemedIcons.UsageRecursion.Id
           typeExtension, ServicesNavigationThemedIcons.UsageExtensionMethod.Id |]
        |> dict


[<ShellComponent>]
type FsharpSpecificOccurrenceKindIconsProvider() =
    interface IOccurrenceKindIconProvider with
        member this.GetImageId(occurrenceKind) =
            FSharpOccurrenceKinds.icons.TryGetValue(occurrenceKind)

        member this.GetPriority _ =
            0


// todo: parens everywhere :)
// todo: parens in `let i: (int) = 1`
// todo: patterns in overrides and objExpr

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FSharpItemOccurenceKindProvider() =
    interface IOccurrenceKindProvider with
        member x.GetOccurrenceKinds(occurrence: IOccurrence) =
            match occurrence.As<ReferenceOccurrence>() with
            | null -> EmptyList.Instance :> _
            | referenceOccurrence ->

            match referenceOccurrence.PrimaryReference with
            | :? TypeExtensionReference -> [| FSharpOccurrenceKinds.typeExtension |] :> _

            | :? ReferenceExpressionTypeReference as refExprReference ->
                match refExprReference.Resolve().DeclaredElement with
                | null -> EmptyList.Instance :> _
                | _ -> [| OccurrenceKind.NewInstanceCreation |] :> _

            | :? RecordCtorReference as recordCtorReference ->
                match recordCtorReference.RecordExpr.CopyInfoExpression with
                | null -> [| OccurrenceKind.NewInstanceCreation |] :> _
                | _ ->    [| FSharpOccurrenceKinds.copyAndUpdate |] :> _

            | reference ->

            let element = referenceOccurrence.Target
            let symbolReference = reference.As<FSharpSymbolReference>()
            if isNull symbolReference then EmptyList.Instance :> _ else

            match symbolReference.GetElement() with
            | :? ITypeReferenceName as typeReferenceName ->
                if isNotNull (AttributeNavigator.GetByReferenceName(typeReferenceName)) then
                    [| OccurrenceKind.Attribute |] :> _ else

                if isNotNull (InheritMemberNavigator.GetByTypeName(typeReferenceName)) ||
                   isNotNull (InterfaceImplementationNavigator.GetByTypeName(typeReferenceName)) ||
                   isNotNull (ObjExprNavigator.GetByTypeName(typeReferenceName)) then
                    [| OccurrenceKind.ExtendedType |] :> _ else

                if isNotNull (OpenStatementNavigator.GetByReferenceName(typeReferenceName)) then
                    [| FSharpOccurrenceKinds.import |] :> _ else

                if isNotNull (NewExprNavigator.GetByTypeName(typeReferenceName)) then
                    [| OccurrenceKind.NewInstanceCreation |] :> _ else

                let namedTypeUsage = NamedTypeUsageNavigator.GetByReferenceName(typeReferenceName)
                if isNull namedTypeUsage then EmptyList.Instance :> _ else

                // todo: return type in `a -> b`
                if isNotNull (ReturnTypeInfoNavigator.GetByReturnType(namedTypeUsage)) then
                    [| FSharpOccurrenceKinds.typeSpecification |] :> _ else

                if isNotNull (AnonRecordFieldNavigator.GetByTypeUsage(namedTypeUsage)) ||
                   isNotNull (RecordFieldDeclarationNavigator.GetByTypeUsage(namedTypeUsage)) ||
                   isNotNull (CaseFieldDeclarationNavigator.GetByTypeUsage(namedTypeUsage)) then
                    [| OccurrenceKind.FieldTypeDeclaration |] :> _ else

                if isNotNull (TypedPatNavigator.GetByTypeUsage(namedTypeUsage)) ||
                   isNotNull (TypedExprNavigator.GetByTypeUsage(namedTypeUsage)) then
                    [| FSharpOccurrenceKinds.typeSpecification |] :> _ else

                if isNotNull (IsInstPatNavigator.GetByTypeUsage(namedTypeUsage)) ||
                   isNotNull (TypeTestExprNavigator.GetByTypeUsage(namedTypeUsage)) then
                    [| CSharpSpecificOccurrenceKinds.TypeChecking |] :> _ else

                if isNotNull (CastExprNavigator.GetByTypeUsage(namedTypeUsage)) then
                    [| CSharpSpecificOccurrenceKinds.TypeConversions |] :> _ else

                if isNotNull (TypeAbbreviationRepresentationNavigator.GetByAbbreviatedType(namedTypeUsage)) then
                    [| FSharpOccurrenceKinds.typeAbbreviation |] :> _ else

                if isNotNull (TypeArgumentListNavigator.GetByTypeUsage(namedTypeUsage)) ||
                   isNotNull (TupleTypeUsageNavigator.GetByItem(namedTypeUsage)) ||
                   isNotNull (ArrayTypeUsageNavigator.GetByTypeUsage(namedTypeUsage)) then
                    [| CSharpSpecificOccurrenceKinds.TypeArgument |] :> _ else

                EmptyList.Instance :> _

            | :? IExpressionReferenceName as referenceName ->
                let referencePat = ReferencePatNavigator.GetByReferenceName(referenceName)
                if isNotNull referencePat && referencePat.IsDeclaration then EmptyList.Instance :> _ else

                if isNotNull referencePat ||
                   isNotNull (ParametersOwnerPatNavigator.GetByReferenceName(referenceName)) ||
                   isNotNull (FieldPatNavigator.GetByReferenceName(referenceName)) then
                    [| FSharpOccurrenceKinds.pattern |] :> _ else

                if isNotNull (AnonRecordFieldNavigator.GetByReferenceName(referenceName)) then
                   [| OccurrenceKind.FieldTypeDeclaration |] :> _ else

                EmptyList.Instance :> _

            | :? ITypeInherit ->
                [| OccurrenceKind.ExtendedType |] :> _

            | :? IReferenceExpr as refExpr ->
                if element :? IConstructor then
                    [| OccurrenceKind.NewInstanceCreation |] :> _ else

                match symbolReference.GetFcsSymbol() with
                | :? FSharpUnionCase ->
                    [| OccurrenceKind.NewInstanceCreation |] :> _

                | :? FSharpEntity as entity when entity.IsFSharpExceptionDeclaration ->
                    if entity.FSharpFields.IsEmpty() && isNull (ReferenceExprNavigator.GetByQualifier(refExpr)) ||
                       isNotNull (PrefixAppExprNavigator.GetByFunctionExpression(refExpr)) then
                        [| OccurrenceKind.NewInstanceCreation |] :> _ else

                    EmptyList.Instance :> _

                | :? FSharpMemberOrFunctionOrValue as mfv when mfv.FullType.IsFunctionType ->
                    let isRecursive = FSharpResolveUtil.isRecursiveApplication element refExpr
                    let isInvocation = FSharpResolveUtil.isInvocation mfv refExpr

                    match isInvocation, isRecursive with
                    | false, false -> [| FSharpOccurrenceKinds.partialApplication |] :> _
                    | true, false -> [| OccurrenceKind.Invocation |] :> _
                    | false, true -> [| FSharpOccurrenceKinds.partialRecursiveApplication |] :> _

                    | true, true ->
                        let appExpr = getOutermostPrefixAppExpr refExpr
                        if FSharpResolveUtil.isInTailRecursivePosition appExpr then
                            [| FSharpOccurrenceKinds.recursiveInvocation |] :> _
                        else
                            [| FSharpOccurrenceKinds.nonTailRecursiveInvocation |] :> _

                | _ -> EmptyList.Instance :> _

            | _ -> EmptyList.Instance :> _

        member x.GetAllPossibleOccurrenceKinds() =
            [| OccurrenceKind.Attribute
               OccurrenceKind.ExtendedType
               OccurrenceKind.Invocation
               OccurrenceKind.NewInstanceCreation
               FSharpOccurrenceKinds.copyAndUpdate
               FSharpOccurrenceKinds.import
               FSharpOccurrenceKinds.nonTailRecursiveInvocation
               FSharpOccurrenceKinds.partialApplication
               FSharpOccurrenceKinds.partialRecursiveApplication
               FSharpOccurrenceKinds.pattern
               FSharpOccurrenceKinds.recursiveInvocation
               FSharpOccurrenceKinds.typeExtension
               FSharpOccurrenceKinds.typeAbbreviation
               FSharpOccurrenceKinds.typeSpecification
               CSharpSpecificOccurrenceKinds.TypeArgument
               CSharpSpecificOccurrenceKinds.TypeChecking
               CSharpSpecificOccurrenceKinds.ReturnTypeUsage |] :> _
