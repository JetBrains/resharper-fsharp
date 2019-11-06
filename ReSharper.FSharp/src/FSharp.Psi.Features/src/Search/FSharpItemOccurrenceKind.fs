namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open FSharp.Compiler.SourceCodeServices
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<RequireQualifiedAccess>]
module FSharpOccurrenceKinds =
    let import = OccurrenceKind("Module or namespace import", OccurrenceKind.SemanticAxis)
    let pattern = OccurrenceKind("Pattern", OccurrenceKind.SemanticAxis)
    let copyAndUpdate = OccurrenceKind("Copy and update", OccurrenceKind.SemanticAxis)
    let typeExtension = OccurrenceKind("Type extension", OccurrenceKind.SemanticAxis)
    let typeAbbreviation = OccurrenceKind("Type abbreviation", OccurrenceKind.SemanticAxis)
    let typeSpecification = OccurrenceKind("Type specification", OccurrenceKind.SemanticAxis)

// todo: parens everywhere :)
// todo: parens in `let i: (int) = 1`
// todo: patterns in overrides and objExpr

[<SolutionComponent>]
type FSharpItemOccurenceKindProvider() =
    interface IOccurrenceKindProvider with
        member x.GetOccurrenceKinds(occurrence: IOccurrence) =
            match occurrence.As<ReferenceOccurrence>() with
            | null -> EmptyList.Instance :> _
            | referenceOccurrence ->

            match referenceOccurrence.PrimaryReference with
            | :? TypeExtensionReference -> [| FSharpOccurrenceKinds.typeExtension |] :> _

            | :? ReferenceExpressionTypeReference -> [| OccurrenceKind.NewInstanceCreation |] :> _

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

                let namedType = NamedTypeNavigator.GetByReferenceName(typeReferenceName)
                if isNull namedType then EmptyList.Instance :> _ else

                // todo: return type in `a -> b`
                if isNotNull (ReturnTypeInfoNavigator.GetByReturnType(namedType)) then
                    [| FSharpOccurrenceKinds.typeSpecification |] :> _ else

                if isNotNull (AnonRecordFieldNavigator.GetByType(namedType)) ||
                   isNotNull (RecordFieldDeclarationNavigator.GetByType(namedType)) ||
                   isNotNull (CaseFieldDeclarationNavigator.GetByType(namedType)) then
                    [| OccurrenceKind.FieldTypeDeclaration |] :> _ else

                if isNotNull (TypedPatNavigator.GetByType(namedType)) ||
                   isNotNull (TypedExprNavigator.GetByType(namedType)) then
                    [| FSharpOccurrenceKinds.typeSpecification |] :> _ else

                if isNotNull (IsInstPatNavigator.GetByType(namedType)) ||
                   isNotNull (TypeTestExprNavigator.GetByType(namedType)) then
                    [| CSharpSpecificOccurrenceKinds.TypeChecking |] :> _ else

                if isNotNull (CastExprNavigator.GetByType(namedType)) then
                    [| CSharpSpecificOccurrenceKinds.TypeConversions |] :> _ else

                if isNotNull (TypeAbbreviationDeclarationNavigator.GetByAbbreviatedType(namedType)) then
                    [| FSharpOccurrenceKinds.typeAbbreviation |] :> _ else

                if isNotNull (TypeArgumentListNavigator.GetByType(namedType)) ||
                   isNotNull (TupleTypeNavigator.GetByItem(namedType)) ||
                   isNotNull (ArrayTypeNavigator.GetByType(namedType)) then
                    [| CSharpSpecificOccurrenceKinds.TypeArgument |] :> _ else

                EmptyList.Instance :> _

            | :? IExpressionReferenceName as referenceName ->
                if isNotNull (ReferencePatNavigator.GetByReferenceName(referenceName)) ||
                   isNotNull (ParametersOwnerPatNavigator.GetByReferenceName(referenceName)) ||
                   isNotNull (FieldPatNavigator.GetByReferenceName(referenceName)) then
                    [| FSharpOccurrenceKinds.pattern |] :> _ else

                if isNotNull (AnonRecordFieldNavigator.GetByReferenceName(referenceName)) then
                   [| OccurrenceKind.FieldTypeDeclaration |] :> _ else

                EmptyList.Instance :> _

            | :? ITypeInherit ->
                // Inherit type produces ctor reference.
                // We don't want base ctor to show as New Instance Creation.
                EmptyList.Instance :> _

            | :? IReferenceExpr as refExpr ->
                let refExpr = refExpr.IgnoreInnerParens()

                if element :? IConstructor then
                    [| OccurrenceKind.NewInstanceCreation |] :> _ else

                let fsSymbol = symbolReference.GetFSharpSymbol()
                match box fsSymbol with
                | :? FSharpUnionCase ->
                    [| OccurrenceKind.NewInstanceCreation |] :> _

                | :? FSharpEntity as entity when entity.IsFSharpExceptionDeclaration ->
                    if entity.FSharpFields.IsEmpty() && isNull (ReferenceExprNavigator.GetByQualifier(refExpr)) ||
                       isNotNull (PrefixAppExprNavigator.GetByFunctionExpression(refExpr)) then
                        [| OccurrenceKind.NewInstanceCreation |] :> _ else

                    EmptyList.Instance :> _

                | _ -> EmptyList.Instance :> _
            | _ -> EmptyList.Instance :> _

        member x.GetAllPossibleOccurrenceKinds() =
            [| OccurrenceKind.Attribute
               OccurrenceKind.ExtendedType
               OccurrenceKind.NewInstanceCreation
               FSharpOccurrenceKinds.import
               FSharpOccurrenceKinds.pattern
               FSharpOccurrenceKinds.copyAndUpdate
               FSharpOccurrenceKinds.typeExtension
               FSharpOccurrenceKinds.typeAbbreviation
               FSharpOccurrenceKinds.typeSpecification
               CSharpSpecificOccurrenceKinds.TypeArgument
               CSharpSpecificOccurrenceKinds.TypeChecking
               CSharpSpecificOccurrenceKinds.ReturnTypeUsage |] :> _
