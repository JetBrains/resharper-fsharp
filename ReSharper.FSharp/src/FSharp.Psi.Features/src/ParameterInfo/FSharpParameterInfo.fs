namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo

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
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
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


type IFcsParameterInfoCandidate =
    abstract Symbol: FSharpSymbol
    abstract ParameterGroupCounts: IList<int>
    abstract ParameterOwner: IParametersOwner


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

    member this.TryGetTupledArgs() =
        match this with
        | Expression expr ->
            match expr.IgnoreInnerParens(true) with
            | :? ITupleExpr as tupleExpr when not tupleExpr.IsStruct ->
                Some(ParameterInfoTupledArguments.Expression tupleExpr)
            | _ -> None

        | Pattern pat ->
            match pat.IgnoreInnerParens(true) with
            | :? ITuplePat as tuplePat when not tuplePat.IsStruct ->
                Some(ParameterInfoTupledArguments.Pattern tuplePat)
            | _ -> None

and ParameterInfoTupledArguments =
    | Expression of ITupleExpr
    | Pattern of ITuplePat

    member this.Node: IFSharpTreeNode =
        match this with
        | Expression expr -> expr
        | Pattern pat -> pat

    member this.Commas =
        match this with
        | Expression tupleExpr -> tupleExpr.Commas
        | Pattern pat -> pat.Commas

[<AllowNullLiteral>]
type IFSharpParameterInfoContext =
    inherit IParameterInfoContext

    abstract CheckResults: FSharpCheckFileResults
    abstract FcsRange: range
    abstract MainSymbol: FSharpSymbol
    abstract Reference: FSharpSymbolReference

    abstract ArgGroups: ParameterInfoArgument list
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

    abstract ParameterGroups: IList<IList<'TParameter>>
    abstract XmlDoc: FSharpXmlDoc

    abstract GetParamName: 'TParameter -> string option
    abstract GetParamType: 'TParameter -> FSharpType

    abstract IsOptionalParam: 'TParameter -> bool
    default this.IsOptionalParam _ = false

    abstract ExtendedType: FSharpEntity option
    default this.ExtendedType = None

    abstract ReturnType: FSharpType option
    default this.ReturnType = None

    abstract SkipLastGroupDescription: bool
    default this.SkipLastGroupDescription = false

    member this.GetParameter(index) =
        let index = if this.IsExtensionMember then index + 1 else index
        getParameterIncludingThis index

    interface IFcsParameterInfoCandidate with
        member this.ParameterGroupCounts =
            this.ParameterGroups
            |> Array.ofSeq
            |> Array.map Seq.length :> _

        member this.ParameterOwner = getParametersOwner ()
        member this.Symbol = this.Symbol

    interface ICandidate with
        member this.GetDescription _ =
            match fsContext.CheckResults.GetDescription(symbol, [], false, fsContext.FcsRange) with
            | ToolTipText [ ToolTipElement.Group [ elementData ] ] ->
                let referenceOwner = fsContext.Reference.GetElement()
                let xmlDocService = referenceOwner.GetSolution().GetComponent<FSharpXmlDocService>().NotNull()
                xmlDocService.GetXmlDocSummary(elementData.XmlDoc)
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
                if index >= curriedParamsCount then () else

                let parameter = parameters[index]
                let name = parameter.ShortName
            
                let summary =
                    if parameter.PresentationLanguage.Is<FSharpLanguage>() then
                        // todo: implement providing xml in declared element, remove this code
                        match this.XmlDoc with
                        | FSharpXmlDoc.FromXmlText xmlDoc ->
                            match DocCommentBlockUtil.TryGetXml(xmlDoc.UnprocessedLines, null) with
                            | true, node -> XMLDocUtil.ExtractParameterSummary(node, name)
                            | _ -> null
                        | _ -> null
                    else
                        parameter.GetXMLDescriptionSummary(true)
            
                let description = XmlDocRichTextPresenter.Run(summary, false, CSharpLanguage.Instance)
                paramInfos[index] <- ParamPresentationInfo(Name = name, Description = description)
            )

        member this.GetSignature(_, _, parameterRanges, mapToOriginalOrder, extensionMethodInfo) =
            let referenceOwner = fsContext.Reference.GetElement()

            let paramGroups = this.ParameterGroups
            if paramGroups.Count = 0 then RichText() else

            let curriedParamsCount = paramGroups |> Seq.sumBy Seq.length 
            let groupParameters = paramGroups.Count

            // Add additional group parameters to highlight group ranges
            let paramsCount = curriedParamsCount + groupParameters
            parameterRanges <- Array.zeroCreate paramsCount

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
                    | Some entity ->
                        // todo: type arg is not provided by FCS, add it to the symbols API
                        text.Append(entity.AsType().FormatLayout(displayContext) |> richText) |> ignore
                        if paramGroup.Count > 0 then
                            text.Append(", ", TextStyle.Default) |> ignore
                    | _ -> ()

                for i = 0 to paramGroup.Count - 1 do
                    let fcsParameter = paramGroup[i]
                    let parameter = this.GetParameter(paramIndex)

                    let paramStart = text.Length

                    if isNotNull parameter then
                        appendNullabilityAttribute parameter |> ignore

                        if parameter.IsParameterArray then
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

                        match tryGetAbbreviatedTypeEntity fcsParameterType with
                        | Some entity when entity.BasicQualifiedName = FSharpPredefinedType.fsOptionTypeName.FullName ->
                            fcsParameterType.GenericArguments
                            |> Seq.tryExactlyOne
                            |> Option.defaultValue fcsParameterType
                        | _ -> fcsParameterType

                    text.Append(fcsParameterType.FormatLayout(displayContext) |> richText) |> ignore

                    if isNotNull parameter && parameter.IsOptional then
                        let constantValue = parameter.GetDefaultValue().ConstantValue
                        let presentation = constantValue.GetPresentation(FSharpLanguage.Instance, TypePresentationStyle.Default)
                        text.Append(" = ", TextStyle.Default) |> ignore
                        text.Append(presentation) |> ignore

                    let paramEnd = text.Length
                    parameterRanges[paramIndex] <- TextRange(paramStart, paramEnd)

                    if i < paramGroup.Count - 1 then
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

                text.Append(returnType.FormatLayout(displayContext) |> richText) |> ignore
            | _ -> ()

            text

        member this.Matches _ =
            let element = getElement ()
            let mainElement = getMainElement ()

            isNotNull mainElement && mainElement.Equals(element) ||
            symbol.IsEffectivelySameAs(fsContext.MainSymbol)

        member this.IsFilteredOut = false
        member this.IsObsolete = false
        member this.ObsoleteDescription = RichTextBlock()
        member this.PositionalParameterCount = 0
        member this.IsFilteredOut with set _ = ()


type FcsMfvParameterInfoCandidate(mfv, symbolUse, fsContext) =
    inherit FcsParameterInfoCandidateBase<FSharpMemberOrFunctionOrValue, FSharpParameter>(mfv, symbolUse, fsContext)

    override val ExtendedType = if mfv.IsExtensionMember then Some mfv.ApparentEnclosingEntity else None
    override val ParameterGroups = mfv.CurriedParameterGroups
    override val ReturnType = if mfv.IsConstructor then None else Some mfv.ReturnParameter.Type
    override val XmlDoc = mfv.XmlDoc

    override this.GetParamName(parameter) = parameter.Name
    override this.GetParamType(parameter) = parameter.Type
    override this.IsOptionalParam(parameter) = parameter.IsOptionalArg


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


type FcsUnionCaseParameterInfoCandidate(unionCase, symbolUse, fsContext) =
    inherit FcsUnionCaseParameterInfoCandidateBase<FSharpUnionCase>(unionCase, symbolUse, fsContext)

    override this.Parameters = unionCase.Fields
    override this.XmlDoc = unionCase.XmlDoc


type FcsExceptionParameterInfoCandidate(entity, symbolUse, fsContext) =
    inherit FcsUnionCaseParameterInfoCandidateBase<FSharpEntity>(entity, symbolUse, fsContext)

    override this.Parameters = entity.FSharpFields
    override this.XmlDoc = entity.XmlDoc


type FcsDelegateParameterInfoCandidate(entity, symbolUse, fsContext) =
    inherit FcsParameterInfoCandidateBase<FSharpEntity, string option * FSharpType>(entity, symbolUse, fsContext)

    override val ParameterGroups = [| entity.FSharpDelegateSignature.DelegateArguments |]
    override val XmlDoc = entity.XmlDoc

    override this.GetParamName((name, _)) = name
    override this.GetParamType((_, paramType)) = paramType


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
                    apc.Index < mfvReturnType.GenericArguments.Count && FSharpPredefinedType.isChoice mfvReturnType ->
                Some mfvReturnType.GenericArguments[apc.Index]

            | false when
                    FSharpPredefinedType.isOption mfvReturnType ||

                    FSharpPredefinedType.isValueOption mfvReturnType &&
                    mfv.ReturnParameter.Attributes.HasAttributeInstance(FSharpPredefinedType.structAttrTypeName) ->
                let optionArgType = mfvReturnType.GenericArguments[0]
                if FSharpPredefinedType.isUnit optionArgType.StrippedType then None else Some optionArgType

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
    let documentRange = DocumentRange(&referenceEndOffset)
    let fcsRange = FSharpRangeUtil.ofDocumentRange documentRange

    let candidates =
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
                Some(FcsDelegateParameterInfoCandidate(e, item, fsContext))

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

    abstract ArgGroups: ParameterInfoArgument list
    abstract NamedArgs: string[]

    interface IFSharpParameterInfoContext with
        member this.Range =
            context.GetDocumentRange().TextRange

        member this.GetArgument(candidate) =
            let candidate = candidate :?> IFcsParameterInfoCandidate
            let parameterGroups = candidate.ParameterGroupCounts
            let allParametersCount = parameterGroups |> Seq.sum
            let invalidArg = allParametersCount + parameterGroups.Count

            if invalidArg = 0 then invalidArg else

            let args = this.ArgGroups

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
                        if parameterGroups[argIndex] = 1 then acc else
                        if arg.Node :? IUnitExpr && caretOffset.Offset < argEnd.Offset then acc else

                        match arg.TryGetTupledArgs() with
                        | Some tupledArgs ->
                            let commaIndex =
                                let commas = tupledArgs.Commas
                                if commas.IsEmpty then 0 else

                                commas
                                |> Seq.tryFindIndex (fun comma -> caretOffset.Offset <= comma.GetDocumentStartOffset().Offset)
                                |> Option.defaultValue commas.Count

                            let paramGroup = parameterGroups[argIndex]
                            if commaIndex >= paramGroup then
                                if paramGroup = 0 then invalidArg else

                                let lastParamIndex = acc + paramGroup - 1
                                let parameters = candidate.ParameterOwner.Parameters
                                if lastParamIndex < parameters.Count && parameters[lastParamIndex].IsParameterArray then
                                    lastParamIndex
                                else
                                    invalidArg
                            else
                                let maxArgOffset = max 0 (parameterGroups[argIndex] - 1)
                                let currentArgOffset = min maxArgOffset commaIndex
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
        member this.Candidates = candidates

        member this.DefaultCandidate =
            let tryToFindCandidate matches =
                candidates
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

                candidates
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

                candidates
                |> Array.exists (function
                    | :? IFcsParameterInfoCandidate as c -> c.ParameterGroupCounts.Count > argGroupsLength
                    | _ -> false)

        member this.CheckResults = checkResults
        member this.FcsRange = fcsRange
        member this.MainSymbol = mainSymbol
        member this.Reference = reference


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

    override this.NamedArgs =
        let appExpr = context.As<IPrefixAppExpr>()
        if isNull appExpr then [||] else

        let funExpr = appExpr.FunctionExpression.IgnoreInnerParens()
        if not (funExpr :? IReferenceExpr) then [||] else

        let argExpr = appExpr.ArgumentExpression

        let args =
            match argExpr.IgnoreInnerParens(singleLevel = true) with
            | :? ITupleExpr as tupleExpr when not tupleExpr.IsStruct -> tupleExpr.Expressions
            | :? IBinaryAppExpr as binaryAppExpr -> TreeNodeCollection([| binaryAppExpr |])
            | _ -> TreeNodeCollection.Empty

        args
        |> Array.ofSeq
        |> Array.map (function
            | :? IBinaryAppExpr as binaryAppExpr when binaryAppExpr.ShortName = "=" ->
                match binaryAppExpr.LeftArgument with
                | :? IReferenceExpr as referenceExpr -> referenceExpr.ShortName
                | _ -> null
            | _ -> null)

[<AllowNullLiteral>]
type FSharpTypeReferenceCtorParameterInfoContext(caretOffset, context, argExpr: IFSharpExpression, reference,
        symbolUses, checkResults, referenceEndOffset, mainSymbol) =
    inherit FSharpParameterInfoContextBase<IFSharpReferenceOwner>(caretOffset, context, reference, symbolUses,
        checkResults, referenceEndOffset, mainSymbol)

    override this.NamedArgs = [||] // todo

    override this.ArgGroups =
        [ if isNotNull argExpr then ParameterInfoArgument.Expression argExpr ]


[<AllowNullLiteral>]
type FSharpPatternParameterInfoContext(caretOffset, pat: IFSharpPattern, reference,
        symbolUses, checkResults, referenceEndOffset, mainSymbol) =
    inherit FSharpParameterInfoContextBase<IFSharpPattern>(caretOffset, pat, reference, symbolUses,
        checkResults, referenceEndOffset, mainSymbol)

    override this.NamedArgs = [||] // todo

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
            if isNotNull binaryAppExpr then null else

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

        match getAllMethods reference true "FSharpParameterInfoContextFactory.getMethods" with
        | None -> None
        | Some (checkResults, Some symbolUses) when not symbolUses.IsEmpty -> Some(checkResults, symbol, symbolUses)
        | Some (checkResults, _) -> Some(checkResults, symbol, [symbolUse])

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

        match getSymbols reference with
        | Some(checkResults, symbol, symbolUses) ->
            FSharpPatternParameterInfoContext(caretOffset, pat, reference, symbolUses,
                checkResults, endOffset, symbol) :> IFSharpParameterInfoContext
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

        member this.CreateContext(solution, caretOffset, _, char, _) =
            let isAutoPopup = char <> '\000'
            let context = this.CreateContextImpl(solution, caretOffset, isAutoPopup)
            // todo: platform: ask if typing should close existing session (space/rparen after last expected arg)
            // todo: platform: allow distinguishing typing inside existing window and requesting info via action
            // todo: allow invoking after expected args via action
            if isNotNull context && context.ExpectingMoreArgs(caretOffset, true) then context else null
