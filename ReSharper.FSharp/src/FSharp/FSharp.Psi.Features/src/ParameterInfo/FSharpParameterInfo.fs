namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo

open System
open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open JetBrains.Application.Threading
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.ReSharper.Feature.Services.ParameterInfo.Sorting
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Psi.Xml.XmlDocComments
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.Util.Extension

module FcsParameterInfoCandidate =
    let canBeNullAttrTypeName = clrTypeName "JetBrains.Annotations.CanBeNullAttribute"
    let notNullAttrTypeName = clrTypeName "JetBrains.Annotations.NotNullAttribute"

    let isSupportedDefaultValueType (parameterType: IType) =
        let valueType =
            match parameterType.GetNullableUnderlyingType() with
            | null -> parameterType
            | underlyingType -> underlyingType

        not (valueType.IsDecimal() || valueType.IsDateTime())

    let getEffectiveDefaultValue (parameter: IParameter) =
        let defaultValue = parameter.GetDefaultValue()
        if defaultValue.IsBadValue then defaultValue else

        let parameterType = parameter.Type
        if defaultValue.IsConstant && isSupportedDefaultValueType parameterType then defaultValue else
        if parameterType.IsNullable() then DefaultValue(parameterType, ConstantValue.Null(parameterType)) else
        DefaultValue(parameterType, parameterType, ConstantValue.DefaultOf(parameterType))


type NamedArgsLayout =
    { DisplayOrder: int[]
      ArgPositions: int[]
      PositionalCount: int
      BracketedFrom: int }


type IFcsParameterInfoCandidate =
    abstract Symbol: FSharpSymbol
    abstract ParameterGroupCounts: IList<int>
    abstract ParameterOwner: IParametersOwner
    abstract IsParamArray: parameterIndex: int -> bool
    abstract TryGetNamedArgIndex: namedArgs: string[] * slot: int -> int option


module NamedArgsLayout =
    let tryCreate (parameterNames: string[]) (namedArgs: string[]) : NamedArgsLayout option =
        if Array.forall isNull namedArgs then None else

        let count = parameterNames.Length
        let positionalCount = namedArgs |> Array.takeWhile isNull |> Array.length
        let displayedPositionalCount = min count positionalCount

        let namedParameters =
            namedArgs
            |> Array.skip positionalCount
            |> Array.choose (fun name -> if isNull name then None else Some(Array.IndexOf(parameterNames, name)))
            |> Array.filter (fun parameter -> parameter >= displayedPositionalCount)
            |> Array.distinct

        let unusedParameters = [| displayedPositionalCount .. count - 1 |] |> Array.except namedParameters
        let nextUnusedPosition = if unusedParameters.IsEmpty() then -1 else displayedPositionalCount + namedParameters.Length

        let argPositions =
            Array.append namedArgs [| null |]
            |> Array.mapi (fun arg name ->
                if arg < positionalCount then -1
                elif isNull name then nextUnusedPosition
                else

                match Array.IndexOf(namedParameters, Array.IndexOf(parameterNames, name)) with
                | -1 -> -1
                | position -> displayedPositionalCount + position)

        Some { DisplayOrder = Array.concat [ [| 0 .. displayedPositionalCount - 1 |]; namedParameters; unusedParameters ]
               ArgPositions = argPositions
               PositionalCount = positionalCount
               BracketedFrom = if namedParameters.IsEmpty() then count else displayedPositionalCount }


type ParameterInfoArgument =
    | Expression of IFSharpExpression
    | Pattern of IFSharpPattern

    member this.Node: IFSharpTreeNode =
        match this with
        | Expression expr -> expr
        | Pattern pat -> pat

    member this.IgnoreSingleInnerParens() =
        match this with
        | Expression expr -> Expression(expr.IgnoreInnerParens(true))
        | Pattern pat -> Pattern(pat.IgnoreInnerParens(true))

    member this.TryGetArgumentList() =
        match this with
        | Expression expr ->
            match expr.IgnoreInnerParens(true) with
            | :? ITupleExpr as tupleExpr when not tupleExpr.IsStruct -> Some(TupleExpr tupleExpr)
            | _ -> None

        | Pattern pat ->
            match pat.IgnoreInnerParens(true) with
            | :? ITuplePat as tuplePat when not tuplePat.IsStruct -> Some(TuplePat tuplePat)
            | :? INamedUnionCaseFieldsPat as fieldsPat -> Some(NamedFieldsPat fieldsPat)
            | _ -> None

    member this.GetSlot(caretOffset: DocumentOffset) =
        match this.TryGetArgumentList() with
        | Some argList -> argList.GetSlot(caretOffset)
        | _ -> 0

    member this.SlotCount =
        match this.TryGetArgumentList() with
        | Some argList -> Seq.length argList.Separators + 1
        | _ -> if this.Node :? IUnitExpr || this.Node :? IUnitPat then 0 else 1

    member this.NamedArgs: string[] =
        match this.TryGetArgumentList() with
        | Some argList -> argList.NamedArgs
        | _ ->

        match this with
        | Expression expr ->
            match FSharpArgumentsUtil.TryGetNamedArgName(expr.IgnoreInnerParens(true)) with
            | null -> [||]
            | name -> [| name |]
        | _ -> [||]

and ParameterInfoArgumentList =
    | TupleExpr of ITupleExpr
    | TuplePat of ITuplePat
    | NamedFieldsPat of INamedUnionCaseFieldsPat

    member this.Separators: ITreeNode seq =
        match this with
        | TupleExpr tupleExpr -> Seq.cast tupleExpr.CommasEnumerable
        | TuplePat tuplePat -> Seq.cast tuplePat.CommasEnumerable
        | NamedFieldsPat fieldsPat -> Seq.cast fieldsPat.SeparatorsEnumerable

    member this.GetSlot(caretOffset: DocumentOffset) =
        this.Separators
        |> Seq.takeWhile (fun separator -> separator.GetDocumentStartOffset().Offset < caretOffset.Offset)
        |> Seq.length

    member this.NamedArgs: string[] =
        match this with
        | TupleExpr tupleExpr -> tupleExpr.ExpressionsEnumerable |> Seq.map FSharpArgumentsUtil.TryGetNamedArgName |> Array.ofSeq
        | TuplePat _ -> [||]
        | NamedFieldsPat fieldsPat -> fieldsPat.FieldPatternsEnumerable |> Seq.map (fun fieldPat -> fieldPat.ShortName) |> Array.ofSeq

[<AllowNullLiteral>]
type IFSharpParameterInfoContext =
    inherit IParameterInfoContext

    abstract PsiModule: IPsiModule
    abstract CheckResults: FSharpCheckFileResults
    abstract FcsRange: range
    abstract MainSymbol: FSharpSymbol
    abstract Reference: FSharpSymbolReference

    abstract ArgGroups: ParameterInfoArgument list
    abstract UnnamedArgCount: int
    abstract ExpectingMoreArgs: caretOffset: DocumentOffset * allowAtLastArgEnd: bool -> bool


[<AbstractClass>]
type FcsParameterInfoCandidateBase<'TSymbol, 'TParameter when 'TSymbol :> FSharpSymbol>
        (symbol: 'TSymbol, symbolUse: FSharpSymbolUse, fsContext: IFSharpParameterInfoContext) =
    let displayContext = symbolUse.DisplayContext.WithShortTypeNames(true)

    let getMainElement () = fsContext.MainSymbol.GetDeclaredElement(fsContext.Reference)
    let getElement () = symbol.GetDeclaredElement(fsContext.Reference)
    let getParametersOwner () = (getElement ()).As<IParametersOwnerWithAttributes>()

    let getParameterIncludingThis index =
        let parametersOwner = getParametersOwner ()
        if isNull parametersOwner then null else

        let parameters = parametersOwner.Parameters
        if parameters.Count <= index then null else parameters[index]

    member this.IsExtensionMember = this.ExtendedType.IsSome
    member this.Symbol = symbol
    member this.DeclaredElement = getElement ()

    abstract ParameterGroups: IList<IList<'TParameter>>
    abstract XmlDoc: FSharpXmlDoc

    abstract GetParamName: 'TParameter -> string option
    abstract GetParamType: 'TParameter -> FSharpType

    abstract IsOptionalParam: 'TParameter -> bool
    default this.IsOptionalParam _ = false

    abstract ExtendedType: FSharpType option
    default this.ExtendedType = None

    abstract ReturnType: FSharpType option
    default this.ReturnType = None

    abstract SkipLastGroupDescription: bool
    default this.SkipLastGroupDescription = false

    abstract CanHaveNamedArguments: bool
    default this.CanHaveNamedArguments = false

    abstract IsFilteredOut: bool
    default this.IsFilteredOut = false

    member this.GetParameter(index) =
        let index = if this.IsExtensionMember then index + 1 else index
        getParameterIncludingThis index

    member this.IsParamArray(index) =
        match this.GetParameter(index) with
        | null -> false
        | parameter -> parameter.IsParameterArray

    member this.ParameterNames =
        this.ParameterGroups[0]
        |> Seq.map (this.GetParamName >> Option.toObj)
        |> Array.ofSeq

    member this.TryGetNamedArgsLayout(namedArgs: string[]) =
        if not this.CanHaveNamedArguments then None else
        NamedArgsLayout.tryCreate this.ParameterNames namedArgs

    member this.TryGetNamedArgIndex(namedArgs: string[], slot: int) =
        match this.TryGetNamedArgsLayout(namedArgs) with
        | Some layout when slot >= layout.PositionalCount -> Array.tryItem slot layout.ArgPositions
        | _ -> None

    interface IFcsParameterInfoCandidate with
        member this.ParameterGroupCounts =
            this.ParameterGroups
            |> Array.ofSeq
            |> Array.map Seq.length :> _

        member this.ParameterOwner = getParametersOwner ()
        member this.Symbol = this.Symbol
        member this.IsParamArray(parameterIndex) = this.IsParamArray(parameterIndex)
        member this.TryGetNamedArgIndex(namedArgs, slot) = this.TryGetNamedArgIndex(namedArgs, slot)

    interface ICandidate with
        member this.GetDescription _ =
            match fsContext.CheckResults.GetDescription(symbol, [], false, fsContext.FcsRange) with
            | ToolTipText [ ToolTipElement.Group [ elementData ] ] ->
                let referenceOwner = fsContext.Reference.GetElement()
                let xmlDocService = referenceOwner.GetSolution().GetComponent<FSharpXmlDocService>().NotNull()
                xmlDocService.GetXmlDocSummary(elementData.XmlDoc, Some symbol, fsContext.PsiModule)
            | _ -> null

        member this.GetParametersInfo(paramInfos, paramArrayIndex) =
            paramArrayIndex <- -1

            let paramGroups = this.ParameterGroups
            let paramGroups =
                if this.SkipLastGroupDescription then
                    let paramGroups = List(paramGroups)
                    paramGroups.RemoveAt(paramGroups.Count - 1)
                    paramGroups :> IList<_>
                else
                    paramGroups

            let curriedParamsCount = paramGroups |> Seq.sumBy Seq.length
            let groupParameters = paramGroups.Count
            let paramsCount = curriedParamsCount + groupParameters

            let paramInfos =
                paramInfos <- Array.zeroCreate paramsCount
                paramInfos

            let parametersOwner = getParametersOwner ()
            if isNull parametersOwner then () else

            let parameters = parametersOwner.Parameters
            if parameters.Count = 0 then () else

            paramGroups
            |> Seq.concat
            |> Seq.iteri (fun index _ ->
                let parameter = this.GetParameter(index)
                if isNull parameter then () else

                let name = parameter.ShortName

                let summary =
                    if parameter.PresentationLanguage.Is<FSharpLanguage>() then
                        // todo: implement providing xml in declared element, remove this code
                        match this.XmlDoc with
                        | FSharpXmlDoc.FromXmlText xmlDoc ->
                            let xmlDoc = XmlDocCommentsUtil.TryReconstructXmlDoc(xmlDoc.UnprocessedLines, FSharpXmlDocLanguage.Instance)
                            XMLDocUtil.ExtractParameterSummary(xmlDoc, name)
                        | _ -> null
                    else
                        parameter.GetXMLDescriptionSummary(true)

                let description = XmlDocRichTextPresenter.Run(summary, false, CSharpLanguage.Instance)
                paramInfos[index] <- ParamPresentationInfo(Name = name, Description = description)
            )

        member this.GetSignature(namedArguments, _, parameterRanges, mapToOriginalOrder, extensionMethodInfo) =
            let referenceOwner = fsContext.Reference.GetElement()

            let paramGroups = this.ParameterGroups
            if paramGroups.Count = 0 then RichText() else

            let curriedParamsCount = paramGroups |> Seq.sumBy Seq.length
            let groupParameters = paramGroups.Count

            // Add additional group parameters to highlight group ranges
            let paramsCount = curriedParamsCount + groupParameters
            parameterRanges <- Array.zeroCreate paramsCount
            mapToOriginalOrder <- Array.init paramsCount id

            let namedArgsLayout = this.TryGetNamedArgsLayout(namedArguments)

            let text = RichText()

            let appendNullabilityAttribute (attrOwner: IAttributesOwner) (attrName: IClrTypeName) =
                let hasAttrInstance = attrOwner.HasAttributeInstance(attrName, true)
                if hasAttrInstance then
                    let attrShortName = attrName.ShortName.SubstringBeforeLast("Attribute")

                    text.Append("[", TextStyle.Default) |> ignore
                    text.Append(attrShortName, TextStyle FSharpHighlightingAttributeIds.Class) |> ignore
                    text.Append("] ", TextStyle.Default) |> ignore

                hasAttrInstance

            let appendNullabilityAttribute (attrOwner: IAttributesOwner) =
                appendNullabilityAttribute attrOwner FcsParameterInfoCandidate.canBeNullAttrTypeName ||
                appendNullabilityAttribute attrOwner FcsParameterInfoCandidate.notNullAttrTypeName

            use _ = ReadLockCookie.Create()
            use _ = CompilationContextCookie.GetOrCreate(referenceOwner.GetPsiModule().GetContextFromModule())

            let mutable paramIndex = 0
            for i = 0 to paramGroups.Count - 1 do
                text.Append("(", TextStyle.Default) |> ignore

                let paramGroup = paramGroups[i]
                if paramGroup.Count = 0 && not this.IsExtensionMember then
                    text.Append("<no parameters>", TextStyle.Default) |> ignore

                let groupStart = text.Length

                if paramIndex = 0 && this.IsExtensionMember then
                    let parameter = getParameterIncludingThis paramIndex
                    if isNotNull parameter then
                        appendNullabilityAttribute parameter |> ignore

                    text.Append("this", TextStyle FSharpHighlightingAttributeIds.Keyword) |> ignore
                    text.Append(" ", TextStyle.Default) |> ignore

                    match this.ExtendedType with
                    | Some extendedType ->
                        // todo: type arg is not provided by FCS, add it to the symbols API
                        text.Append(extendedType.FormatRichText(displayContext) |> ofFcsRichText) |> ignore
                        if paramGroup.Count > 0 then
                            text.Append(", ", TextStyle.Default) |> ignore
                    | _ -> ()

                let groupStartIndex = paramIndex
                let displayOrder, bracketedFrom =
                    match namedArgsLayout with
                    | Some layout -> layout.DisplayOrder, layout.BracketedFrom
                    | _ -> Array.init paramGroup.Count id, paramGroup.Count

                for displayIndex = 0 to paramGroup.Count - 1 do
                    let groupParamIndex = displayOrder[displayIndex]
                    let fcsParameter = paramGroup[groupParamIndex]
                    let parameter = this.GetParameter(groupStartIndex + groupParamIndex)
                    mapToOriginalOrder[paramIndex] <- groupStartIndex + groupParamIndex

                    let paramStart = text.Length

                    if isNotNull parameter then
                        appendNullabilityAttribute parameter |> ignore

                    let isBracketed = displayIndex >= bracketedFrom
                    if isBracketed then
                        text.Append("[", TextStyle.Default) |> ignore

                    if isNotNull parameter && parameter.IsParameterArray then
                        text.Append("params", TextStyle FSharpHighlightingAttributeIds.Keyword) |> ignore
                        text.Append(" ", TextStyle.Default) |> ignore

                    if this.IsOptionalParam(fcsParameter) && not parameter.IsOptional then
                        text.Append("?", TextStyle.Default) |> ignore

                    match this.GetParamName(fcsParameter) with
                    | Some name ->
                        text.Append(name, TextStyle FSharpHighlightingAttributeIds.Parameter) |> ignore
                        text.Append(": ", TextStyle.Default) |> ignore
                    | _ -> ()

                    let fcsParameterType =
                        let fcsParameterType = this.GetParamType(fcsParameter)
                        if not (this.IsOptionalParam(fcsParameter)) then fcsParameterType else

                        if fcsParameterType.IsFSharpOption then
                            fcsParameterType.GenericArguments
                            |> Seq.tryExactlyOne
                            |> Option.defaultValue fcsParameterType
                        else
                            fcsParameterType

                    text.Append(fcsParameterType.FormatRichText(displayContext) |> ofFcsRichText) |> ignore

                    if isNotNull parameter && parameter.IsOptional then
                        text.Append(" = ", TextStyle.Default) |> ignore

                        let defaultValue = FcsParameterInfoCandidate.getEffectiveDefaultValue parameter
                        if defaultValue.IsDefaultValue && not defaultValue.IsConstant then
                            text.Append("default", TextStyle FSharpHighlightingAttributeIds.Keyword) |> ignore
                        else
                            text.Append(defaultValue.ConstantValue.GetPresentation(FSharpLanguage.Instance, TypePresentationStyle.Default)) |> ignore

                    if isBracketed then
                        text.Append("]", TextStyle.Default) |> ignore

                    let paramEnd = text.Length
                    parameterRanges[paramIndex] <- TextRange(paramStart, paramEnd)

                    if displayIndex < paramGroup.Count - 1 then
                        text.Append(", ", TextStyle.Default) |> ignore

                    paramIndex <- paramIndex + 1

                let groupEnd = text.Length
                parameterRanges[curriedParamsCount + i] <- TextRange(groupStart, groupEnd)

                text.Append(")", TextStyle.Default) |> ignore

                if i < paramGroups.Count - 1 then
                    text.Append(" ", TextStyle.Default) |> ignore

            match this.ReturnType with
            | Some returnType ->
                text.Append(" : ", TextStyle.Default) |> ignore

                let parametersOwner = getParametersOwner ()
                if isNotNull parametersOwner then
                    appendNullabilityAttribute parametersOwner |> ignore

                text.Append(returnType.FormatRichText(displayContext) |> ofFcsRichText) |> ignore
            | _ -> ()

            text

        member this.Matches _ =
            let element = getElement ()
            let mainElement = getMainElement ()

            isNotNull mainElement && mainElement.Equals(element) ||
            symbol.IsEffectivelySameAs(fsContext.MainSymbol)

        member this.IsFilteredOut with get () = this.IsFilteredOut and set _ = ()
        member this.IsObsolete = false
        member this.ObsoleteDescription = RichTextBlock()
        member this.PositionalParameterCount = 0

// keeps the original symbol order
type FallbackCandidateSortStrategy private() =
    inherit CandidateSortStrategy(Int32.MaxValue)
    static member Instance = FallbackCandidateSortStrategy()    
    override this.Sort(items) = ()

type FcsMfvParameterInfoCandidate(mfv, symbolUse, fsContext) =
    inherit FcsParameterInfoCandidateBase<FSharpMemberOrFunctionOrValue, FSharpParameter>(mfv, symbolUse, fsContext)

    let isCustomOp = mfv.Attributes.HasAttributeInstance(FSharpPredefinedType.customOperationAttrTypeName)

    override this.IsFilteredOut =
        if not this.CanHaveNamedArguments || fsContext.Candidates.Length < 2 then false else

        if not (acceptsUnnamedArgs mfv.CurriedParameterGroups[0] fsContext.UnnamedArgCount) then true else

        let parameterNames = this.ParameterNames
        let isCandidateParameterName name =
            fsContext.Candidates
            |> Array.exists (function
                | :? FcsMfvParameterInfoCandidate as candidate -> Array.contains name candidate.ParameterNames
                | _ -> false)

        fsContext.NamedArguments
        |> Array.exists (fun name ->
            isNotNull name && not (Array.contains name parameterNames) && isCandidateParameterName name)

    override val ExtendedType = if mfv.IsExtensionMember then Some mfv.ApparentEnclosingType else None
    override val ReturnType = if mfv.IsConstructor then None else Some mfv.ReturnParameter.Type
    override val XmlDoc = mfv.XmlDoc

    override val ParameterGroups =
        if not isCustomOp then
            mfv.CurriedParameterGroups
        else
            let firstGroup = mfv.CurriedParameterGroups[0]
            if firstGroup.IsEmpty() then [||] else

            firstGroup
            |> Seq.tail
            |> Seq.map (fun p -> Array.singleton p :> IList<_>)
            |> Array.ofSeq :> _

    override this.GetParamName(parameter) = parameter.Name
    override this.GetParamType(parameter) = parameter.Type
    override this.IsOptionalParam(parameter) = parameter.IsOptionalArg
    override this.CanHaveNamedArguments = canHaveNamedArguments mfv

    interface ISupportsStructuredSortCandidate with
        member this.GetStrategyAndSortItem() =
            match this.DeclaredElement with
            | :? IClrDeclaredElement as element ->
                let declaredElementInstance = DeclaredElementInstance(element, element.IdSubstitution)
                CandidateSortStrategy.GetStrategyAndSortItem(declaredElementInstance, this, FallbackCandidateSortStrategy.Instance)
            | _ ->
                let sortItem = SortItem("", this)
                (FallbackCandidateSortStrategy.Instance, sortItem)

[<AbstractClass>]
type FcsUnionCaseParameterInfoCandidateBase<'TSymbol when 'TSymbol :> FSharpSymbol>(unionCase, symbolUse, fsContext) =
    inherit FcsParameterInfoCandidateBase<'TSymbol, FSharpField>(unionCase, symbolUse, fsContext)

    abstract Parameters: IList<FSharpField>

    override this.ParameterGroups =
        let fields = this.Parameters
        let result = List()
        result.Add(fields)
        result

    override this.GetParamName(field) =
        if field.IsNameGenerated then None else Some field.Name

    override this.GetParamType(field) = field.FieldType
    override this.IsOptionalParam _ = false
    override this.CanHaveNamedArguments = true


type FcsUnionCaseParameterInfoCandidate(unionCase, symbolUse, fsContext) =
    inherit FcsUnionCaseParameterInfoCandidateBase<FSharpUnionCase>(unionCase, symbolUse, fsContext)

    override this.Parameters = unionCase.Fields
    override this.XmlDoc = unionCase.XmlDoc


type FcsExceptionParameterInfoCandidate(entity, symbolUse, fsContext) =
    inherit FcsUnionCaseParameterInfoCandidateBase<FSharpEntity>(entity, symbolUse, fsContext)

    override this.Parameters = entity.FSharpFields
    override this.XmlDoc = entity.XmlDoc


type FcsDelegateParameterInfoCandidate(fcsEntityInstance: FcsEntityInstance, symbolUse, fsContext) =
    inherit FcsParameterInfoCandidateBase<FSharpEntity, FSharpType>(fcsEntityInstance.Entity, symbolUse, fsContext)

    let mfv =
        fcsEntityInstance.Entity.MembersFunctionsAndValues
        |> Seq.tryFind (fun mfv -> mfv.LogicalName = "Invoke")

    override val ParameterGroups =
        mfv
        |> Option.map (fun mfv -> [| [| mfv.FullType |] :> IList<_> |] :> IList<_>)
        |> Option.defaultValue EmptyList.InstanceList

    override val XmlDoc = fcsEntityInstance.Entity.XmlDoc

    override this.GetParamName _ = None
    override this.GetParamType(fcsType) = fcsType.Instantiate(fcsEntityInstance.Substitution)


type FcsActivePatternMfvParameterInfoCandidate(apc: FSharpActivePatternCase, mfv: FSharpMemberOrFunctionOrValue, symbolUse, fsContext) =
    inherit FcsParameterInfoCandidateBase<FSharpMemberOrFunctionOrValue, string option * FSharpType>(mfv, symbolUse, fsContext)

    override this.ParameterGroups =
        let activePatternGroup = apc.Group
        let names = activePatternGroup.Names

        let returnType =
            let mfvReturnType = mfv.ReturnParameter.Type
            match activePatternGroup.IsTotal with
            | true when
                    names.Count > 1 && apc.Index < names.Count &&
                    apc.Index < mfvReturnType.GenericArguments.Count && mfvReturnType.IsFSharpChoice ->
                Some mfvReturnType.GenericArguments[apc.Index]

            | false when
                    mfvReturnType.IsFSharpOption ||

                    mfvReturnType.IsFSharpValueOption &&
                    mfv.ReturnParameter.Attributes.HasAttributeInstance(FSharpPredefinedType.structAttrTypeName) ->
                let optionArgType = mfvReturnType.GenericArguments[0]
                if optionArgType.IsUnitType then None else Some optionArgType

            | _ -> Some mfvReturnType

        let parameterGroups =
            mfv.CurriedParameterGroups
            |> Seq.map (fun group -> List(group |> Seq.map (fun p -> p.Name, p.Type)) :> IList<_>)
            |> List

        parameterGroups.RemoveAt(parameterGroups.Count - 1)

        returnType |> Option.iter (fun returnType ->
            let returnGroup =
                if returnType.IsTupleType then
                    returnType.GenericArguments |> Seq.map (fun t -> None, t)
                else
                    [None, returnType]
            parameterGroups.Add(List(returnGroup)))

        parameterGroups

    override this.GetParamName((name, _)) = name
    override this.GetParamType((_, paramType)) = paramType

    override this.XmlDoc = mfv.XmlDoc
    override this.SkipLastGroupDescription = true


[<AllowNullLiteral; AbstractClass>]
type FSharpParameterInfoContextBase<'TNode when 'TNode :> IFSharpTreeNode>(caretOffset: DocumentOffset, context: 'TNode,
        reference: FSharpSymbolReference, symbolUses: FSharpSymbolUse list, checkResults,
        referenceEndOffset: DocumentOffset, mainSymbol: FSharpSymbol) as this =
    let psiModule = context.GetPsiModule()
    let documentRange = DocumentRange(&referenceEndOffset)
    let fcsRange = FSharpRangeUtil.ofDocumentRange documentRange

    let mutable candidatesSource =
        let fsContext = this :> IFSharpParameterInfoContext
        symbolUses
        |> List.choose (fun item ->
            match item.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv when not (mfv.CurriedParameterGroups.IsEmpty()) ->
                Some(FcsMfvParameterInfoCandidate(mfv, item, fsContext) :> ICandidate)

            | :? FSharpUnionCase as uc when not (uc.Fields.IsEmpty()) ->
                Some(FcsUnionCaseParameterInfoCandidate(uc, item, fsContext))

            | :? FSharpEntity as e when e.IsFSharpExceptionDeclaration && not (e.FSharpFields.IsEmpty()) ->
                Some(FcsExceptionParameterInfoCandidate(e, item, fsContext))

            | :? FSharpEntity as e when e.IsDelegate ->
                let fcsEntityInstance =
                    if e.GenericParameters.IsEmpty() then
                        let fcsType = e.AsType()
                        FcsEntityInstance.create fcsType |> Some
                    else
                        let getTypeExpr =
                            match e.IsFSharp, box context with
                            | true, (:? IPrefixAppExpr as prefixAppExpr) ->
                                match prefixAppExpr.ArgumentExpression with
                                | :? IUnitExpr -> context :> IFSharpTreeNode
                                | argExpr -> argExpr :> _
                            | _ -> context :> _

                        if isNull getTypeExpr then None else

                        let exprType = getTypeExpr.TryGetFcsType()
                        if isNull exprType then None else

                        FcsEntityInstance.create exprType |> Some

                fcsEntityInstance
                |> Option.map (fun fcsEntityInstance ->
                    FcsDelegateParameterInfoCandidate(fcsEntityInstance, item, fsContext))

            | :? FSharpActivePatternCase as apc ->
                match apc.Group.DeclaringEntity with
                | Some entity ->
                    entity.MembersFunctionsAndValues
                    |> Seq.tryFind (fun mfv -> Range.rangeContainsRange mfv.DeclarationLocation apc.DeclarationLocation)
                | _ ->
                    let psiModule = context.GetPsiModule()
                    let referenceOwner = reference.GetElement()
                    apc.GetActivePatternCaseElement(psiModule, referenceOwner).As<ILocalReferencePat>()
                    |> Option.ofObj
                    |> Option.bind (fun refPat ->
                        let mfv = refPat.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
                        if isNull mfv then None else Some mfv)
                |> Option.map (fun mfv -> FcsActivePatternMfvParameterInfoCandidate(apc, mfv, item, fsContext))

            | _ -> None)
        |> List.filter (function
            | :? FcsActivePatternMfvParameterInfoCandidate as c -> c.ParameterGroups.Count > 0
            | _ -> true)
        |> Array.ofList
    
    // StructuredSort will call the context instance back; therefore, sorting should happen after construction
    let candidatesLazy =
        lazy
            CandidateSort.StructuredSort(candidatesSource, FallbackCandidateSortStrategy.Instance)
            candidatesSource

    abstract ArgGroups: ParameterInfoArgument list

    member this.NamedArgs =
        match this.ArgGroups, mainSymbol with
        | [ Expression _ as arg ], FieldsOwner fields -> getFieldNamedArgs fields arg.NamedArgs
        | [ arg ], _ -> arg.NamedArgs
        | _ -> [||]

    member this.UnnamedArgCount =
        match this.ArgGroups with
        | [ arg ] -> arg.SlotCount - (this.NamedArgs |> Array.filter isNotNull).Length
        | _ -> 0

    interface IFSharpParameterInfoContext with
        member this.Range =
            context.GetDocumentRange()

        member val ShouldSuggestSuppress = false with get, set
        member val IsForced = false with get, set
            
        member this.GetArgument(candidate) =
            let candidate = candidate :?> IFcsParameterInfoCandidate
            let parameterGroups = candidate.ParameterGroupCounts
            let allParametersCount = parameterGroups |> Seq.sum
            let invalidArg = allParametersCount + parameterGroups.Count

            if invalidArg = 0 then invalidArg else

            let args = this.ArgGroups

            let tryGetNamedArgIndex (arg: ParameterInfoArgument) =
                candidate.TryGetNamedArgIndex(this.NamedArgs, arg.GetSlot(caretOffset))
                |> Option.map (fun index -> if index < 0 then invalidArg else index)

            let rec loop argIndex (acc: int) (args: ParameterInfoArgument list) =
                match args with
                | [] ->
                    if argIndex >= parameterGroups.Count then
                        invalidArg
                    elif argIndex < parameterGroups.Count && parameterGroups[argIndex] = 1 then
                        acc
                    else
                        allParametersCount + argIndex

                | arg :: args ->
                    let argRange = arg.Node.GetDocumentRange()
                    let argEnd = argRange.EndOffset
                    let argStart = argRange.StartOffset

                    let isAtNextArgStart () =
                        match args with
                        | nextArg :: _ -> nextArg.Node.GetDocumentStartOffset() = caretOffset
                        | _ -> false
                    let argRange = arg.Node.GetDocumentRange()

                    if caretOffset.Offset > argEnd.Offset || caretOffset = argEnd && isAtNextArgStart () then
                        if argIndex < parameterGroups.Count then
                            loop (argIndex + 1) (acc + parameterGroups[argIndex]) args
                        else
                            allParametersCount + argIndex

                    elif argRange.Contains(caretOffset) && argStart <> caretOffset && argEnd <> caretOffset && argIndex < parameterGroups.Count then
                        match tryGetNamedArgIndex arg with
                        | Some index -> index
                        | _ ->

                        if parameterGroups[argIndex] = 1 then acc else
                        if arg.Node :? IUnitExpr && caretOffset.Offset < argEnd.Offset then acc else

                        match arg.TryGetArgumentList() with
                        | Some argList ->
                            let slot = argList.GetSlot(caretOffset)
                            let paramGroup = parameterGroups[argIndex]
                            if slot >= paramGroup then
                                if paramGroup = 0 then invalidArg else

                                let lastParamIndex = acc + paramGroup - 1
                                if candidate.IsParamArray(lastParamIndex) then lastParamIndex else invalidArg
                            else
                                let maxArgOffset = max 0 (parameterGroups[argIndex] - 1)
                                let currentArgOffset = min maxArgOffset slot
                                acc + currentArgOffset
                        | _ ->
                            let innerArg = arg.IgnoreSingleInnerParens().Node
                            if innerArg != arg then
                                acc
                            else
                                allParametersCount + argIndex

                    elif argIndex < parameterGroups.Count && parameterGroups[argIndex] = 1 then
                        acc

                    else
                        allParametersCount + argIndex

            loop 0 0 args

        member this.ArgGroups = this.ArgGroups
        member this.Candidates = candidatesLazy.Value

        member this.DefaultCandidate =
            let tryToFindCandidate matches =
                candidatesLazy.Value
                |> Array.tryFind (function
                    | :? IFcsParameterInfoCandidate as candidate -> matches candidate.Symbol
                    | _ -> false)
                |> Option.defaultValue null

            let toFindCandidateFromFcsSymbol () =
                tryToFindCandidate (fun symbol -> symbol.IsEffectivelySameAs(mainSymbol))

            match mainSymbol.GetDeclaredElement(reference) with
            | null -> toFindCandidateFromFcsSymbol ()
            | mainElement ->

            match tryToFindCandidate (fun symbol -> mainElement.Equals(symbol.GetDeclaredElement(reference))) with
            | null -> toFindCandidateFromFcsSymbol ()
            | candidate -> candidate

        member this.NamedArguments = this.NamedArgs

        member this.ParameterListNodeType = null
        member this.ParameterNodeTypes = null
        member this.NamedArguments with set _ = ()

        member this.ExpectingMoreArgs(caretOffset, allowAtLastArgEnd) =
            let argGroups = this.ArgGroups
            if argGroups.IsEmpty then true else

            let lastArg = List.last argGroups
            let offset = caretOffset.Offset
            let lastArgEnd = lastArg.Node.GetTreeEndOffset().Offset
            let argGroupsLength = argGroups.Length

            let removeParenRange =
                argGroupsLength = 1 &&

                let expr = argGroups[0].Node
                (expr :? IParenExpr || expr :? IUnitExpr) &&

                candidatesLazy.Value
                |> Array.forall (function
                    :? IFcsParameterInfoCandidate as c ->
                        c.ParameterGroupCounts.Count = 1 &&

                        match c.Symbol with
                        | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.IsMember
                        | _ -> false
                    | _ -> false)

            if removeParenRange then
                let parenExpr = argGroups[0].As<IParenExpr>()
                let range =
                    argGroups[0].Node.GetDocumentRange()
                        .TrimLeft(if isNotNull parenExpr && isNull parenExpr.LeftParen then 0 else 1)
                        .TrimRight(if isNotNull parenExpr && isNull parenExpr.RightParen then 0 else 1)
                range.Contains(caretOffset)
            else
                if allowAtLastArgEnd && offset <= lastArgEnd || offset < lastArgEnd then true else

                candidatesLazy.Value
                |> Array.exists (function
                    | :? IFcsParameterInfoCandidate as c -> c.ParameterGroupCounts.Count > argGroupsLength
                    | _ -> false)

        member this.UnnamedArgCount = this.UnnamedArgCount
        member this.CheckResults = checkResults
        member this.FcsRange = fcsRange
        member this.MainSymbol = mainSymbol
        member this.Reference = reference
        member this.PsiModule = psiModule


[<AllowNullLiteral>]
type FSharpPrefixAppParameterInfoContext(caretOffset, context, reference, symbolUses, checkResults, referenceEndOffset,
        mainSymbol) =
    inherit FSharpParameterInfoContextBase<IFSharpExpression>(caretOffset, context, reference, symbolUses, checkResults,
        referenceEndOffset, mainSymbol)

    override this.ArgGroups =
        let rec getArgs (expr: IFSharpExpression) acc =
            match expr with
            | :? IPrefixAppExpr as prefixAppExpr ->
                match prefixAppExpr.ArgumentExpression with
                | null -> getArgs prefixAppExpr acc
                | argExpr -> getArgs prefixAppExpr.FunctionExpression (argExpr :: acc)
            | _ -> acc

        getArgs context []
        |> List.map ParameterInfoArgument.Expression

[<AllowNullLiteral>]
type FSharpTypeReferenceCtorParameterInfoContext(caretOffset, context, argExpr: IFSharpExpression, reference,
        symbolUses, checkResults, referenceEndOffset, mainSymbol) =
    inherit FSharpParameterInfoContextBase<IFSharpReferenceOwner>(caretOffset, context, reference, symbolUses,
        checkResults, referenceEndOffset, mainSymbol)

    override this.ArgGroups =
        [ if isNotNull argExpr then ParameterInfoArgument.Expression argExpr ]


[<AllowNullLiteral>]
type FSharpPatternParameterInfoContext(caretOffset, pat: IFSharpPattern, reference,
        symbolUses, checkResults, referenceEndOffset, mainSymbol) =
    inherit FSharpParameterInfoContextBase<IFSharpPattern>(caretOffset, pat, reference, symbolUses,
        checkResults, referenceEndOffset, mainSymbol)

    override this.ArgGroups =
        match pat with
        | :? IParametersOwnerPat as parameterOwnerPat ->
            parameterOwnerPat.ParametersEnumerable
            |> List.ofSeq
            |> List.map ParameterInfoArgument.Pattern
        | _ -> []


[<ParameterInfoContextFactory(typeof<FSharpLanguage>)>]
type FSharpParameterInfoContextFactory() =
    let popupChars = [| ' '; '('; ',' |]
    let mutable shouldSuggestSuppress_Value = false

    let checkNodeBeforeNodeTypes =
        NodeTypeSet(
            FSharpTokenType.COMMA,
            FSharpTokenType.COLON,
            FSharpTokenType.COLON_QMARK,
            FSharpTokenType.COLON_QMARK_GREATER,
            FSharpTokenType.GREATER_RBRACK,
            FSharpTokenType.ELIF,
            FSharpTokenType.ELSE,
            FSharpTokenType.END,
            FSharpTokenType.IN,
            FSharpTokenType.RPAREN,
            FSharpTokenType.RBRACE,
            FSharpTokenType.RBRACK,
            FSharpTokenType.BAR_RBRACK,
            FSharpTokenType.THEN,
            FSharpTokenType.TO,
            FSharpTokenType.SEMICOLON
            )

    let rec getTokenAtOffset isAutoPopup allowRetry (caretOffset: DocumentOffset) (solution: ISolution) =
        let fsFile = solution.GetPsiServices().GetPsiFile<FSharpLanguage>(caretOffset).As<IFSharpFile>()
        if isNull fsFile then null else

        // todo: get token from caretOffset - 1, try looking at the next token in nested contexts?

        match fsFile.FindTokenAt(caretOffset), allowRetry with
        | null, false -> null
        | null, true ->
            let caretOffset = caretOffset.Shift(-1)
            getTokenAtOffset isAutoPopup false caretOffset solution
        | token, _ ->

        let token =
            if not checkNodeBeforeNodeTypes[getTokenType token] || token.GetDocumentStartOffset() <> caretOffset then
                token
            else
                let prevSibling = token.GetPreviousToken()
                if getTokenType prevSibling == FSharpTokenType.RPAREN then
                    token
                else
                    prevSibling

        if isNull token then null else

        let rec isInsideComment checkPrevious (token: ITreeNode) =
            token.IsCommentToken() && caretOffset.Offset > token.GetTreeStartOffset().Offset ||
            checkPrevious && getTokenType token == FSharpTokenType.NEW_LINE && isInsideComment false token.PrevSibling

        if isAutoPopup && isInsideComment true token then null else

        let token = token.GetPreviousMeaningfulToken(true)
        if isNull token then null else token

    let getTypeReferenceName (token: ITokenNode) =
        if not (token :? FSharpIdentifierToken) then null else
        token.Parent.As<ITypeReferenceName>()

    let isInTypeReferenceConstructorNode (referenceName: ITypeReferenceName) =
        isNotNull (NewExprNavigator.GetByTypeName(referenceName)) ||
        isNotNull (AttributeNavigator.GetByReferenceName(referenceName)) ||
        isNotNull (InheritMemberNavigator.GetByTypeName(referenceName))

    let isArgExpression (expr: IFSharpExpression) =
        isNotNull (AppLikeExprNavigator.GetByArgumentExpression(expr)) ||
        isNotNull (AttributeNavigator.GetByExpression(expr)) ||
        isNotNull (TypeInheritNavigator.GetByCtorArgExpression(expr))

    let shouldShowPopup (caretOffset: DocumentOffset) (contextRange: DocumentRange) =
        contextRange.Contains(caretOffset) ||

        caretOffset.Offset >= contextRange.StartOffset.Offset &&
        caretOffset.ToDocumentCoords().Column > contextRange.StartOffset.ToDocumentCoords().Column

    let rec tryCreateContext isAutoPopup (caretOffset: DocumentOffset) (expr: IFSharpExpression) =
        let range = expr.GetDocumentRange()
        let expr =
            match expr with
            | :? IParenExpr as parenExpr when
                    caretOffset.Offset > range.StartOffset.Offset && caretOffset.Offset < range.EndOffset.Offset ||
                    isNull (PrefixAppExprNavigator.GetByArgumentExpression(parenExpr)) ->
                // Only use the inner expression when inside parens
                match parenExpr.InnerExpression with
                | :? IPrefixAppExpr as prefixAppExpr when prefixAppExpr.GetDocumentEndOffset() = caretOffset ->
                    match prefixAppExpr.ArgumentExpression with
                    | :? IParenExpr | :? IUnitExpr -> expr
                    | _ -> prefixAppExpr
                | innerExpr -> innerExpr
            | _ -> expr

        if isNull expr then null else

        match expr with
        | :? IPrefixAppExpr as appExpr ->
            // todo: allow on non-refExpr invoked expressions (lambdas, other apps)
            let reference =
                match appExpr.InvokedReferenceExpression with
                | null -> null
                | refExpr -> refExpr.Reference

            match createFromExpression isAutoPopup caretOffset reference appExpr with
            | null -> tryCreateFromParentExpr isAutoPopup caretOffset expr
            | context -> context

        | :? IReferenceExpr as refExpr ->
            // todo: enable for non-predefined operators?
            let binaryAppExpr = BinaryAppExprNavigator.GetByOperator(refExpr)
            if isNotNull binaryAppExpr then
                if FSharpArgumentsUtil.HasNamedArgStructure(binaryAppExpr) then
                    tryCreateFromParentExpr isAutoPopup caretOffset binaryAppExpr
                else
                    null
            else

            match PrefixAppExprNavigator.GetByArgumentExpression(refExpr) with
            | null ->
                let context = createFromExpression isAutoPopup caretOffset refExpr.Reference refExpr
                if isNull context && not isAutoPopup && caretOffset = refExpr.GetDocumentEndOffset() then
                    tryCreateFromParentExpr isAutoPopup caretOffset refExpr
                else
                    context

            | prefixAppExpr ->
                tryCreateContext isAutoPopup caretOffset prefixAppExpr

        | _ ->
            tryCreateFromParentExpr isAutoPopup caretOffset expr

    // todo: identifier end in f<int>
    and getSymbols (reference: FSharpSymbolReference) =
        let symbolUse = reference.GetSymbolUse()
        if isNull symbolUse then None else

        let isApplicable (fcsSymbol: FSharpSymbol) =
            match fcsSymbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                not mfv.IsProperty && mfv.CurriedParameterGroups.Count > 0

            | :? FSharpActivePatternCase
            | :? FSharpEntity
            | :? FSharpUnionCase -> true
            | _ -> false

        let symbol = symbolUse.Symbol
        if not (isApplicable symbol) then None else

        getMethodCandidates reference "FSharpParameterInfoContextFactory.getMethods"
        |> Option.map (fun (checkResults, symbolUses) -> checkResults, symbol, symbolUses)

    and create (caretOffset: DocumentOffset) (reference: FSharpSymbolReference) (context: IFSharpTreeNode) =
        if isNull reference || isNull context then null else

        let endOffset = DocumentOffset(caretOffset.Document, reference.GetTreeTextRange().EndOffset.Offset)
        if not (shouldShowPopup caretOffset (context.GetDocumentRange())) then null else

        match getSymbols reference with
        | Some(checkResults, symbol, symbolUses) ->
            FSharpPrefixAppParameterInfoContext(caretOffset, context :?> IFSharpExpression, reference, symbolUses,
                checkResults, endOffset, symbol) :> IFSharpParameterInfoContext
        | _ -> null

    and createFromPattern isAutoPopup (caretOffset: DocumentOffset) (pat: IReferenceNameOwnerPat) =
        let reference = pat.Reference
        if isNull reference || isNull pat then null else
        if not (shouldShowPopup caretOffset (pat.GetDocumentRange())) then null else

        let range = reference.GetElement().GetTreeTextRange()
        let endOffset = DocumentOffset(caretOffset.Document, range.EndOffset.Offset)

        if caretOffset.Offset >= range.StartOffset.Offset && caretOffset.Offset < range.EndOffset.Offset ||
                caretOffset = endOffset && isNotNull (ParametersOwnerPatNavigator.GetByParameter(pat)) then
            tryCreateFromParentPat isAutoPopup caretOffset pat else

        let symbolUse = reference.GetSymbolUse()
        if isNull symbolUse then null else

        let isApplicable =
            match symbolUse.Symbol with
            | :? FSharpEntity as e -> e.IsFSharpExceptionDeclaration
            | :? FSharpActivePatternCase
            | :? FSharpUnionCase -> true
            | _ -> false

        if not isApplicable then null else

        match pat.FSharpFile.GetParseAndCheckResults(true, "FSharpParameterInfoContextFactory.createFromPattern") with
        | Some results ->
            FSharpPatternParameterInfoContext(caretOffset, pat, reference, [symbolUse],
                results.CheckResults, endOffset, symbolUse.Symbol) :> IFSharpParameterInfoContext
        | _ -> null

    and tryCreateContextFromPattern isAutoPopup (caretOffset: DocumentOffset) (pat: IFSharpPattern) : IFSharpParameterInfoContext =
        if isNull pat then null else

        let range = pat.GetDocumentRange()
        let pat =
            match pat with
            | :? IParenPat as parenPat when
                    caretOffset.Offset > range.StartOffset.Offset && caretOffset.Offset < range.EndOffset.Offset ||
                    isNull (ParametersOwnerPatNavigator.GetByParameter(parenPat)) ->
                // Only use the inner node when inside parens
                match parenPat.Pattern with
                | :? IParametersOwnerPat as parametersOwnerPat when
                    parametersOwnerPat.GetDocumentEndOffset() = caretOffset &&
                    parametersOwnerPat.ParametersEnumerable.Any() ->
                    match Seq.last parametersOwnerPat.ParametersEnumerable with
                    | :? IParenPat | :? IUnitPat -> pat
                    | _ -> parametersOwnerPat
                | innerExpr -> innerExpr
            | _ -> pat

        match pat with
        | :? IParametersOwnerPat as parametersOwnerPat ->
            createFromPattern isAutoPopup caretOffset parametersOwnerPat

        | :? IReferencePat as refPat ->
            match ParametersOwnerPatNavigator.GetByParameter(refPat) with
            | null ->
                let context = createFromPattern isAutoPopup caretOffset refPat
                if isNull context && not isAutoPopup && caretOffset = refPat.GetDocumentEndOffset() then
                    tryCreateFromParentPat isAutoPopup caretOffset refPat
                else
                    context
            | parametersOwnerPat ->
                tryCreateContextFromPattern isAutoPopup caretOffset parametersOwnerPat

        | _ ->
            tryCreateFromParentPat isAutoPopup caretOffset pat

    and createFromTypeReference (caretOffset: DocumentOffset) (reference: FSharpSymbolReference)
            (argExpr: IFSharpExpression) =
        if isNull reference then null else

        let context = reference.GetElement()
        let endOffset = DocumentOffset(caretOffset.Document, reference.GetTreeTextRange().EndOffset.Offset)
        if not (shouldShowPopup caretOffset (context.GetDocumentRange())) then null else

        if caretOffset.Offset <= endOffset.Offset &&
                (isNull argExpr || caretOffset.Offset < argExpr.GetTreeStartOffset().Offset) then
            // Inside invoked type reference name, try to get context from a parent node instead
            let parentExpr = context.GetContainingNode<IFSharpExpression>()
            tryCreateFromParentExpr false caretOffset parentExpr else

        match getSymbols reference with
        | Some(checkResults, symbol, symbolUses) ->
            FSharpTypeReferenceCtorParameterInfoContext(caretOffset, context, argExpr, reference, symbolUses,
                checkResults, endOffset, symbol) :> IFSharpParameterInfoContext
        | _ -> null

    and tryCreateFromTypeReference (caretOffset: DocumentOffset) (token: ITokenNode) : IFSharpParameterInfoContext =
        let referenceName = getTypeReferenceName token
        if not (isInTypeReferenceConstructorNode referenceName) then null else

        createFromTypeReference caretOffset referenceName.Reference null

    and createFromExpression isAutoPopup (caretOffset: DocumentOffset) (reference: FSharpSymbolReference)
            (contextExpr: IFSharpExpression) =
        if isNull reference || isNull contextExpr then null else

        let range = reference.GetElement().GetTreeTextRange()
        let endOffset = DocumentOffset(caretOffset.Document, range.EndOffset.Offset)
        let appExpr = getOutermostPrefixAppExpr contextExpr
        let appExpr = appExpr.IgnoreParentParens()

        if caretOffset.Offset >= range.StartOffset.Offset && caretOffset.Offset < range.EndOffset.Offset ||
                caretOffset = endOffset && isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(appExpr)) then
            // Inside invoked function name, try to get context from a parent expression instead
            tryCreateFromParentExpr isAutoPopup caretOffset appExpr else

        let appExpr = getOutermostPrefixAppExpr contextExpr

        create caretOffset reference appExpr

    and tryCreateFromParentExpr isAutoPopup caretOffset (expr: IFSharpExpression) =
        if isNull expr then null else

        let expr = expr.IgnoreParentParens()

        let reference, argExpr =
            let typeInherit = TypeInheritNavigator.GetByCtorArgExpression(expr)
            if isNotNull typeInherit then
                typeInherit.Reference, typeInherit.CtorArgExpression else

            let attribute = AttributeNavigator.GetByExpression(expr)
            if isNotNull attribute then
                attribute.Reference, attribute.Expression else

            let newExpr = NewExprNavigator.GetByArgumentExpression(expr)
            if isNotNull newExpr then
                newExpr.Reference, newExpr.ArgumentExpression else

            let objExpr = ObjExprNavigator.GetByArgExpression(expr)
            if isNotNull objExpr then
                objExpr.TypeName.Reference, objExpr.ArgExpression else

            null, null

        if isNotNull reference && isNotNull argExpr then
            createFromTypeReference caretOffset reference argExpr else

        let parentExpr = expr.GetContainingNode<IFSharpExpression>()
        tryCreateContext isAutoPopup caretOffset parentExpr

    and tryCreateFromParentPat isAutoPopup caretOffset (pat: IFSharpPattern) =
        if isNull pat then null else

        let parentPat = pat.IgnoreParentParens().GetContainingNode<IFSharpPattern>()
        tryCreateContextFromPattern isAutoPopup caretOffset parentPat

    member this.CreateContextImpl(solution, caretOffset, isAutoPopup: bool) =
        let token = getTokenAtOffset false true caretOffset solution
        if isNull token then null else

        let pat = token.GetContainingNode<IFSharpPattern>(true)
        let context = tryCreateContextFromPattern isAutoPopup caretOffset pat
        if isNotNull context then context else

        let context = tryCreateFromTypeReference caretOffset token
        if isNotNull context then context else

        let expr = token.GetContainingNode<IFSharpExpression>(true)
        tryCreateContext isAutoPopup caretOffset expr

    interface IParameterInfoContextFactory with
        member this.Language = FSharpLanguage.Instance

        member this.ImportantChars = []
        member this.IsIntellisenseEnabled(_, _) = true // todo: settings

        member this.ShouldPopup(caretOffset, char, solution, _) =
             // todo: settings
            if solution.Locks.IsOnMainThread() then
                // Do a quick check before typed char is inserted,
                // to see if creating a context should be scheduled at all
                Array.contains char popupChars
            else
                // This is called again before requesting a new context on reparsed file
                let offset = caretOffset.Offset
                let shouldPopup =
                    let token = getTokenAtOffset true true caretOffset solution
                    if isNull token then false else

                    let typeReferenceName = getTypeReferenceName token
                    if isInTypeReferenceConstructorNode typeReferenceName then true else

                    let expr = token.GetContainingNode<IFSharpExpression>(true)
                    match expr with
                    | :? IPrefixAppExpr -> true

                    | :? IReferenceExpr as refExpr when offset >= refExpr.GetDocumentEndOffset().Offset ->
                        true

                    | :? IUnitExpr as unitExpr ->
                        isArgExpression unitExpr

                    | :? ITupleExpr as tupleExpr ->
                        not tupleExpr.IsStruct &&

                        let tupleExpr = tupleExpr.IgnoreParentParens(singleLevel = true)
                        isArgExpression tupleExpr

                    | expr ->
                        isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(expr)) &&
                        (offset <= expr.GetTreeStartOffset().Offset || offset >= expr.GetTreeEndOffset().Offset)

                if not shouldPopup then false else
                if char <> ' ' then true else

                let context = this.CreateContextImpl(solution, caretOffset, true)
                isNotNull context && context.ExpectingMoreArgs(caretOffset, false)

        member this.CreateContext(solution, caretOffset, _, char, _, _) =
            let isAutoPopup = char <> '\000'
            let context = this.CreateContextImpl(solution, caretOffset, isAutoPopup)
            // todo: platform: ask if typing should close existing session (space/rparen after last expected arg)
            // todo: platform: allow distinguishing typing inside existing window and requesting info via action
            // todo: allow invoking after expected args via action
            if isNotNull context && context.ExpectingMoreArgs(caretOffset, true) then context else null
