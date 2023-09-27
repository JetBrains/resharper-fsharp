namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpRelevanceRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.DecorateItems(context, items) =
        let isCustomOperationPossible =
            lazy
                let refExpr =
                    context.ReparsedContext.Reference |> Option.ofObj
                    |> Option.bind (fun reference ->
                        reference.GetTreeNode().As<IReferenceExpr>() |> Option.ofObj)
                refExpr |> Option.exists isInsideComputationExpressionForCustomOperation

        for item in items do
            let fcsLookupItem = item.As<FcsLookupItem>()
            if isNull fcsLookupItem then () else

            match fcsLookupItem.FcsSymbol with
            | :? FSharpEntity ->
                markRelevance item CLRLookupItemRelevance.TypesAndNamespaces

                if not (Array.isEmpty fcsLookupItem.NamespaceToOpen) then
                    markRelevance item CLRLookupItemRelevance.NotImportedType
                else
                    markRelevance item CLRLookupItemRelevance.ImportedType

            | :? FSharpMemberOrFunctionOrValue as mfv ->
                if not mfv.IsModuleValueOrMember then
                    markRelevance item CLRLookupItemRelevance.LocalVariablesAndParameters else

                if mfv.IsEvent then
                    markRelevance item CLRLookupItemRelevance.Events else

                if mfv.IsExtensionMember then
                    markRelevance item CLRLookupItemRelevance.ExtensionMethods else

                if mfv.IsMember && mfv.IsProperty then
                    markRelevance item CLRLookupItemRelevance.FieldsAndProperties
                else
                    if fcsLookupItem.FcsSymbolUse.IsFromComputationExpression then
                        if isCustomOperationPossible.Value then
                            markRelevance item CLRLookupItemRelevance.ExpectedTypeMatch

                    markRelevance item CLRLookupItemRelevance.Methods

            | :? FSharpField as field when
                    field.DeclaringEntity
                    |> Option.map (fun e -> e.IsEnum)
                    |> Option.defaultValue false ->
                markRelevance item CLRLookupItemRelevance.EnumMembers

            | :? FSharpField ->
                markRelevance item CLRLookupItemRelevance.FieldsAndProperties

            | :? FSharpUnionCase
            | :? FSharpActivePatternCase ->
                markRelevance item CLRLookupItemRelevance.Methods

            | :? FSharpParameter -> markRelevance item CLRLookupItemRelevance.LocalVariablesAndParameters

            | :? FSharpGenericParameter
            | :? FSharpStaticParameter -> markRelevance item CLRLookupItemRelevance.TypesAndNamespaces

            | _ -> ()
