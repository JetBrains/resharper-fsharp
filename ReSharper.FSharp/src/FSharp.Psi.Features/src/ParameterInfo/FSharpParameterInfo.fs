namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo

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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp
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

type FcsParameterInfoCandidate(range: range, fcsSymbolUse: FSharpSymbolUse, checkResults: FSharpCheckFileResults, expr: IFSharpExpression, resolvedMfv: FSharpMemberOrFunctionOrValue) =
    let mfv = fcsSymbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
    let displayContext = fcsSymbolUse.DisplayContext.WithShortTypeNames(true)

    let psiModule = expr.GetPsiModule()
    let declaredElement = mfv.GetDeclaredElement(psiModule)
    let parametersOwner = declaredElement.As<IParametersOwnerWithAttributes>()
    let isExtensionMember = mfv.IsExtensionMember

    let getParameterIncludingThis index =
        if isNull parametersOwner then null else

        let parameters = parametersOwner.Parameters
        if parameters.Count <= index then null else parameters[index]

    let getParameter index =
        let index = if isExtensionMember then index + 1 else index
        getParameterIncludingThis index

    member this.Mfv = mfv
    member this.ParameterOwner = parametersOwner

    interface ICandidate with
        member this.GetDescription() =
            let substitution = fcsSymbolUse.GenericArguments
            match checkResults.GetDescription(mfv, substitution, false, range) with
            | ToolTipText [ ToolTipElement.Group [ elementData ] ] ->
                let xmlDocService = expr.GetSolution().GetComponent<FSharpXmlDocService>().NotNull()
                xmlDocService.GetXmlDocSummary(elementData.XmlDoc)
            | _ -> null

        member this.GetParametersInfo(paramInfos, paramArrayIndex) =
            paramArrayIndex <- -1

            let paramGroups = mfv.CurriedParameterGroups
            let curriedParamsCount = paramGroups |> Seq.sumBy Seq.length
            let groupParameters = paramGroups.Count
            let paramsCount = curriedParamsCount + groupParameters

            let parameters = parametersOwner.Parameters

            let paramInfos =
                paramInfos <- Array.zeroCreate paramsCount
                paramInfos

            if parameters.Count = 0 then () else

            paramGroups
            |> Seq.concat
            |> Seq.iteri (fun index _ ->
                let parameter = parameters[index]
                let name = parameter.ShortName
            
                let summary =
                    if parameter.PresentationLanguage.Is<FSharpLanguage>() then
                        // todo: implement providing xml in declared element, remove this code
                        match mfv.XmlDoc with
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
            let paramGroups = mfv.CurriedParameterGroups
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
            use _ = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

            let mutable paramIndex = 0
            for i = 0 to paramGroups.Count - 1 do
                text.Append("(", TextStyle.Default) |> ignore

                let paramGroup = paramGroups[i]
                if paramGroup.Count = 0 && not isExtensionMember then
                    text.Append("<no parameters>", TextStyle.Default) |> ignore

                let groupStart = text.Length

                if paramIndex = 0 && isExtensionMember then
                    let parameter = getParameterIncludingThis paramIndex
                    if isNotNull parameter then
                        appendNullabilityAttribute parameter |> ignore

                    text.Append("this", TextStyle FSharpHighlightingAttributeIds.Keyword) |> ignore
                    text.Append(" ", TextStyle.Default) |> ignore

                    // todo: type arg is not provided by FCS, add it to the symbols API
                    text.Append(mfv.ApparentEnclosingEntity.AsType().FormatLayout(displayContext) |> richText) |> ignore
                    if paramGroup.Count > 0 then
                        text.Append(", ", TextStyle.Default) |> ignore

                for i = 0 to paramGroup.Count - 1 do
                    let fcsParameter = paramGroup[i]
                    let parameter = getParameter paramIndex

                    let paramStart = text.Length

                    if isNotNull parameter then
                        appendNullabilityAttribute parameter |> ignore

                        if parameter.IsParameterArray then
                            text.Append("params", TextStyle FSharpHighlightingAttributeIds.Keyword) |> ignore
                            text.Append(" ", TextStyle.Default) |> ignore

                    let hasFSharpDeclarationRange = fcsParameter.DeclarationLocation <> Range.range0
                    if fcsParameter.IsOptionalArg && not parameter.IsOptional && hasFSharpDeclarationRange then
                        text.Append("?", TextStyle.Default) |> ignore

                    match fcsParameter.Name with
                    | Some name ->
                        text.Append(name, TextStyle FSharpHighlightingAttributeIds.Parameter) |> ignore
                        text.Append(": ", TextStyle.Default) |> ignore
                    | _ -> ()

                    let fcsParameterType =
                        let fcsParameterType = fcsParameter.Type
                        if not fcsParameter.IsOptionalArg || not hasFSharpDeclarationRange then fcsParameterType else

                        match tryGetAbbreviatedTypeEntity fcsParameterType with
                        | Some entity when entity.QualifiedBaseName = FSharpPredefinedType.fsOptionTypeName.FullName ->
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

            if not mfv.IsConstructor then
                text.Append(" : ", TextStyle.Default) |> ignore

                if isNotNull parametersOwner then
                    appendNullabilityAttribute parametersOwner |> ignore
                
                text.Append(mfv.ReturnParameter.Type.FormatLayout(displayContext) |> richText) |> ignore

            text

        member this.Matches _ =
            mfv.IsEffectivelySameAs(resolvedMfv)

        member this.IsFilteredOut = false
        member this.IsObsolete = false
        member this.ObsoleteDescription = RichTextBlock()
        member this.PositionalParameterCount = 0
        member this.IsFilteredOut with set _ = ()


[<AllowNullLiteral>]
type FSharpParameterInfoContext2(caretOffset: DocumentOffset, appExpr: IFSharpExpression, methods: FSharpSymbolUse list, checkResults, fcsRange, mfv) =
    let candidates =
        methods
        |> List.choose (fun item ->
            match item.Symbol with
            | :? FSharpMemberOrFunctionOrValue ->
                Some(FcsParameterInfoCandidate(fcsRange, item, checkResults, appExpr, mfv) :> ICandidate)
            | _ -> None)
        |> Array.ofList

    interface IParameterInfoContext with
        member this.Range =
            appExpr.GetDocumentRange().TextRange

        member this.GetArgument(candidate) =
            let candidate = candidate :?> FcsParameterInfoCandidate
            let parameterGroups = candidate.Mfv.CurriedParameterGroups
            let allParametersCount = parameterGroups |> Seq.sumBy Seq.length
            let invalidArg = allParametersCount + parameterGroups.Count

            let rec getArgs (expr: IFSharpExpression) acc =
                match expr with
                | :? IPrefixAppExpr as prefixAppExpr ->
                    match prefixAppExpr.ArgumentExpression with
                    | null -> getArgs prefixAppExpr acc
                    | argExpr -> getArgs prefixAppExpr.FunctionExpression (argExpr :: acc)
                | _ -> acc

            let args = getArgs appExpr []

            let rec loop argIndex (acc: int) (args: IFSharpExpression list) =
                match args with
                | [] ->
                    if argIndex >= parameterGroups.Count then
                        invalidArg
                    elif argIndex < parameterGroups.Count && parameterGroups[argIndex].Count = 1 then
                        acc
                    else
                        allParametersCount + argIndex

                | arg :: args ->
                    let argRange = arg.GetDocumentRange()
                    let argEnd = argRange.EndOffset
                    let argStart = argRange.StartOffset

                    let isAtNextArgStart () =
                        match args with
                        | nextArg :: _ -> nextArg.GetDocumentStartOffset() = caretOffset
                        | _ -> false

                    if caretOffset.Offset > argEnd.Offset || caretOffset = argEnd && isAtNextArgStart () then
                        loop (argIndex + 1) (acc + parameterGroups[argIndex].Count) args

                    elif argRange.Contains(caretOffset) && argStart <> caretOffset && argEnd <> caretOffset then
                        if parameterGroups[argIndex].Count = 1 then acc else
                        if arg :? IUnitExpr && caretOffset.Offset < argEnd.Offset then acc else

                        match arg.IgnoreSingleInnerParens() with
                        | :? ITupleExpr as tupleExpr when not tupleExpr.IsStruct ->
                            let commaIndex =
                                let commas = tupleExpr.Commas
                                if commas.IsEmpty then 0 else

                                commas
                                |> Seq.tryFindIndex (fun comma -> caretOffset.Offset <= comma.GetDocumentStartOffset().Offset)
                                |> Option.defaultValue commas.Count

                            let paramGroup = parameterGroups[argIndex]
                            if commaIndex >= paramGroup.Count then
                                if paramGroup.Count = 0 then invalidArg else

                                let lastParamIndex = acc + paramGroup.Count - 1
                                let parameters = candidate.ParameterOwner.Parameters
                                if lastParamIndex < parameters.Count && parameters[lastParamIndex].IsParameterArray then
                                    lastParamIndex
                                else
                                    invalidArg
                            else
                                let maxArgOffset = max 0 (parameterGroups[argIndex].Count - 1)
                                let currentArgOffset = min maxArgOffset commaIndex
                                acc + currentArgOffset
                        | innerArg ->
                            if innerArg != arg then
                                acc
                            else
                                allParametersCount + argIndex

                    elif argIndex < parameterGroups.Count && parameterGroups[argIndex].Count = 1 then
                        acc

                    else
                        allParametersCount + argIndex

            loop 0 0 args

        member this.Candidates = candidates

        member this.DefaultCandidate =
            candidates
            |> Array.tryFind (function
                | :? FcsParameterInfoCandidate as candidate -> candidate.Mfv.IsEffectivelySameAs(mfv)
                | _ -> false)
            |> Option.defaultValue null

        member this.NamedArguments =
            let appExpr = appExpr.As<IPrefixAppExpr>()
            if isNull appExpr then [||] else

            let funExpr = appExpr.FunctionExpression.IgnoreInnerParens()
            if not (funExpr :? IReferenceExpr) then [||] else

            let args =
                match appExpr.ArgumentExpression.IgnoreSingleInnerParens() with
                | :? ITupleExpr as tupleExpr when not tupleExpr.IsStruct -> tupleExpr.Expressions
                | :? IBinaryAppExpr as binaryAppExpr -> TreeNodeCollection([| binaryAppExpr |])
                | _ -> TreeNodeCollection.Empty

            args
            |> Array.ofSeq
            |> Array.map (function
                | :? IBinaryAppExpr as binaryAppExpr when binaryAppExpr.Operator.ShortName = "=" ->
                    match binaryAppExpr.LeftArgument with
                    | :? IReferenceExpr as referenceExpr -> referenceExpr.ShortName
                    | _ -> null
                | _ -> null)

        member this.ParameterListNodeType = null
        member this.ParameterNodeTypes = null
        member this.NamedArguments with set _ = ()


[<ParameterInfoContextFactory(typeof<FSharpLanguage>)>]
type FSharpParameterInfoContextFactory2() =
    let popupChars = [| ' '; '('; ',' |]

    let getExpressionAtOffset (caretOffset: DocumentOffset) (solution: ISolution) =
        let fsFile = solution.GetPsiServices().GetPsiFile<FSharpLanguage>(caretOffset).As<IFSharpFile>()
        if isNull fsFile then null else

        let token = fsFile.FindTokenAt(caretOffset)
        if isNull token then null else

        let token =
            if getTokenType token != FSharpTokenType.RPAREN then
                token
            else
                let prevToken = token.GetPreviousMeaningfulToken()
                if getTokenType prevToken == FSharpTokenType.RPAREN then
                    token
                else
                    prevToken

        let token = token.GetPreviousMeaningfulToken(true)
        if isNull token then null else

        token.GetContainingNode<IFSharpExpression>(true)

    let shouldShowPopup (caretOffset: DocumentOffset) (exprRange: DocumentRange) =
        exprRange.Contains(caretOffset) ||

        caretOffset.Offset >= exprRange.StartOffset.Offset &&
        caretOffset.ToDocumentCoords().Column > exprRange.StartOffset.ToDocumentCoords().Column

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
            match create isAutoPopup caretOffset appExpr.InvokedReferenceExpression appExpr with
            | null -> tryCreateFromParent isAutoPopup caretOffset expr
            | context -> context

        | :? IReferenceExpr as refExpr ->
            match PrefixAppExprNavigator.GetByArgumentExpression(refExpr.IgnoreParentParens()) with
            | null ->
                let context = create isAutoPopup caretOffset refExpr refExpr
                if isNull context && not isAutoPopup && caretOffset = refExpr.GetDocumentEndOffset() then
                    tryCreateFromParent isAutoPopup caretOffset refExpr
                else
                    context

            | prefixAppExpr ->
                tryCreateContext isAutoPopup caretOffset prefixAppExpr

        | _ ->
            tryCreateFromParent isAutoPopup caretOffset expr

    and create isAutoPopup (caretOffset: DocumentOffset) (invokedExpr: IReferenceExpr) (contextExpr: IFSharpExpression) =
        if isNull invokedExpr || isNull contextExpr then null else

        let range = invokedExpr.GetDocumentRange()
        let appExpr = getOutermostPrefixAppExpr contextExpr
        let appExpr = appExpr.IgnoreParentParens()

        if caretOffset.Offset >= range.StartOffset.Offset && caretOffset.Offset < range.EndOffset.Offset ||
                caretOffset = range.EndOffset && isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(appExpr)) then
            // Inside invoked function name, try to get context from a parent expression instead 
            tryCreateFromParent isAutoPopup caretOffset appExpr else

        let appExpr = getOutermostPrefixAppExpr contextExpr
        if not (shouldShowPopup caretOffset (appExpr.GetDocumentRange())) then null else

        let symbolUse = invokedExpr.Reference.GetSymbolUse()
        if isNull symbolUse then null else

        let mfv = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
        if isNull mfv then null else

        match contextExpr.FSharpFile.GetParseAndCheckResults(true, "FSharpParameterInfoContextFactory") with
        | None -> null
        | Some results ->

        let endOffset = invokedExpr.GetDocumentEndOffset()
        let endCoords = endOffset.ToDocumentCoords()
        let line = int endCoords.Line + 1
        let column = int endCoords.Column + 1
        let names = List.ofSeq invokedExpr.Names

        match results.CheckResults.GetMethodsAsSymbols(line, column, "", names) with
        | Some symbolUses when not symbolUses.IsEmpty ->
            let documentRange = DocumentRange(&endOffset)
            let fcsRange = FSharpRangeUtil.ofDocumentRange documentRange

            FSharpParameterInfoContext2(caretOffset, appExpr, symbolUses, results.CheckResults, fcsRange, mfv)
        | _ -> null

    and tryCreateFromParent isAutoPopup caretOffset (expr: IFSharpExpression) =
        let parentExpr = expr.IgnoreParentParens().GetContainingNode<IFSharpExpression>()
        tryCreateContext isAutoPopup caretOffset parentExpr

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
                match getExpressionAtOffset caretOffset solution with
                | :? IPrefixAppExpr
                | :? IReferenceExpr -> true

                | :? IUnitExpr as unitExpr ->
                    isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(unitExpr))

                | :? ITupleExpr as tupleExpr ->
                    not tupleExpr.IsStruct &&

                    let tupleExpr = tupleExpr.IgnoreSingleParentParens()
                    isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(tupleExpr))

                | expr ->
                    caretOffset.Offset >= expr.GetDocumentEndOffset().Offset &&
                    isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(expr))

        member this.CreateContext(solution, caretOffset, _, char, _) =
            let isAutoPopup = char <> '\000'
            let expr = getExpressionAtOffset caretOffset solution
            tryCreateContext isAutoPopup caretOffset expr
