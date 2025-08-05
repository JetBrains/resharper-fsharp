namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpRelevanceRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let isObjectEntity (fcsEntity: FSharpEntity) =
        match fcsEntity.LogicalName with
        | "Object" ->
            match fcsEntity.Namespace with
            | Some "System" -> true
            | _ -> false
        | _ -> false

    let emphasize (item: ILookupItem) =
        match item with
        | :? FcsLookupItem as fcsLookupItem ->
            fcsLookupItem.Emphasize()
        | _ -> ()

    override this.DecorateItems(context, items) =
        let isCustomOperationPossible =
            lazy
                let refExpr =
                    context.ReparsedContext.Reference |> Option.ofObj
                    |> Option.bind (fun reference ->
                        reference.GetTreeNode().As<IReferenceExpr>() |> Option.ofObj)
                refExpr |> Option.exists isInsideComputationExpressionForCustomOperation

        let fcsType = getQualifierType context

        for item in items do
            let info =
                match item with
                | :? IFcsLookupItemInfo as info -> info
                | :? IAspectLookupItem<ILookupItemInfo> as item -> item.Info.As<IFcsLookupItemInfo>()
                | _ -> null

            if isNull info then () else

            match info.FcsSymbol with
            | :? FSharpEntity ->
                markRelevance item CLRLookupItemRelevance.TypesAndNamespaces

                if not (Array.isEmpty info.NamespaceToOpen) then
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

                if mfv.IsMember then
                    if mfv.IsProperty then
                        markRelevance item CLRLookupItemRelevance.FieldsAndProperties
                    else
                        if info.FcsSymbolUse.IsFromComputationExpression then
                            if isCustomOperationPossible.Value then
                                markRelevance item CLRLookupItemRelevance.ExpectedTypeMatch

                        markRelevance item CLRLookupItemRelevance.Methods

                    if isNull fcsType then () else

                    let fcsType = fcsType.ErasedType
                    if fcsType.HasTypeDefinition then
                        let contextFcsEntity = fcsType.TypeDefinition

                        match mfv.DeclaringEntity with
                        | None -> ()
                        | Some fcsEntity ->

                        if fcsEntity.Equals(contextFcsEntity) then
                            markRelevance item CLRLookupItemRelevance.MemberOfCurrentType
                            emphasize item

                        elif isObjectEntity fcsEntity then
                            markRelevance item CLRLookupItemRelevance.MemberOfObject

                        else
                            markRelevance item CLRLookupItemRelevance.MemberOfBaseType

            | :? FSharpField as field when
                    field.DeclaringEntity
                    |> Option.map (fun e -> e.IsEnum)
                    |> Option.defaultValue false ->
                markRelevance item CLRLookupItemRelevance.EnumMembers
                emphasize item

            | :? FSharpField ->
                markRelevance item CLRLookupItemRelevance.FieldsAndProperties
                emphasize item

            | :? FSharpUnionCase
            | :? FSharpActivePatternCase ->
                markRelevance item CLRLookupItemRelevance.Methods

            | :? FSharpParameter -> markRelevance item CLRLookupItemRelevance.LocalVariablesAndParameters

            | :? FSharpGenericParameter
            | :? FSharpStaticParameter -> markRelevance item CLRLookupItemRelevance.TypesAndNamespaces

            | _ -> ()
