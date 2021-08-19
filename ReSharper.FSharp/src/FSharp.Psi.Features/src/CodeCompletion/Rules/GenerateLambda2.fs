namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

//open FSharp.Compiler.Symbols
//open JetBrains.Diagnostics
//open JetBrains.DocumentModel
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
//open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
//open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
//open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
//open JetBrains.ReSharper.Feature.Services.Util
//open JetBrains.ReSharper.Plugins.FSharp.Psi
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
//open JetBrains.ReSharper.Psi
//open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
//open JetBrains.ReSharper.Psi.Transactions
//open JetBrains.ReSharper.Psi.Tree
//open JetBrains.ReSharper.Resources.Shell
//open JetBrains.UI.RichText
//open JetBrains.Util
//
//module GenerateLambdaInfo =
//    let [<Literal>] CreateLambda = "Create lambda"
//
//
//type GenerateLambdaInfo(text, paramNames: string list list) =
//    inherit TextualInfo(text, GenerateLambdaInfo.CreateLambda)
//
//    member val Names = paramNames
//
//    override this.IsRiderAsync = false
//
//
//type GenerateLambdaBehavior(info: GenerateLambdaInfo) =
//    inherit TextualBehavior<GenerateLambdaInfo>(info)
//
//    override this.Accept(textControl, nameRange, _, _, solution, _) =
//        let psiServices = solution.GetPsiServices()
//
//        textControl.Document.ReplaceText(nameRange, "__")
//        let nameRange = nameRange.StartOffset.ExtendRight("__".Length)
//
//        psiServices.Files.CommitAllDocuments()
//        let refExpr = TextControlToPsi.GetElement<IReferenceExpr>(solution, nameRange.EndOffset)
//
//        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
//        use transactionCookie =
//            PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, GenerateLambdaInfo.CreateLambda)
//
//        let parenExpr = refExpr.CreateElementFactory().CreateExpr(info.Text).As<IParenExpr>()
//        let insertedParenExpr = ModificationUtil.ReplaceChild(refExpr, parenExpr)
//        let insertedLambdaExpr = insertedParenExpr.InnerExpression.As<ILambdaExpr>()
//
//        let hotspotsRegistry = HotspotsRegistry(insertedLambdaExpr.GetPsiServices())
//
//        (info.Names, insertedLambdaExpr.Parameters.Patterns) ||> Seq.iter2 (fun names itemPattern ->
//            let nameSuggestionsExpression = NameSuggestionsExpression(names)
//            let rangeMarker = itemPattern.GetDocumentRange().CreateRangeMarker()
//            hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression))
//
//        let hotspotSession =
//            LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
//                solution, insertedLambdaExpr.Expression.GetDocumentEndOffset(), textControl,
//                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotsRegistry.CreateHotspots())
//
//        hotspotSession.ExecuteAndForget()
//
//
//[<Language(typeof<FSharpLanguage>)>]
//type GenerateLambdaRule() =
//    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()
//
//    let getFunExprAndPosition (expr: IFSharpExpression) =
//        let expr = expr.NotNull().IgnoreParentParens()
//        let tupleExpr = TupleExprNavigator.GetByExpression(expr)
//        let arg, positionInGroup =
//            if isNull tupleExpr then expr, 0 else tupleExpr.IgnoreParentParens(), tupleExpr.Expressions.IndexOf(expr)
//
//        let prefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(arg)
//        if isNull prefixAppExpr then None else
//
//        let rec loop curriedGroupPosition (appExpr: IPrefixAppExpr) =
//            match appExpr.FunctionExpression.IgnoreInnerParens() with
//            | :? IPrefixAppExpr as nestedPrefixAppExpr -> loop (curriedGroupPosition + 1) nestedPrefixAppExpr
//            | funExpr -> Some(funExpr.IgnoreParentParens(), (curriedGroupPosition, positionInGroup))
//
//        loop 0 prefixAppExpr
//
//    let rec extractPartialSubstitution (declType: FSharpType) (argType: FSharpType) =
//        if declType.IsGenericParameter then [declType.GenericParameter, argType] else
//
//        // todo: functions, tuples
//        if not declType.HasTypeDefinition || not argType.HasTypeDefinition then [] else
//
//        let declTypeDefinition = declType.TypeDefinition
//        let argTypeDefinition = argType.TypeDefinition
//
//        // todo: inheritors: e.g. list vs seq
//        // todo: different abbreviations
//        if not (declTypeDefinition.Equals(argTypeDefinition))  then [] else
//
//        // todo: tuple
//        let declTypeArgs = declType.GenericArguments
//        let argTypeArgs = argType.GenericArguments
//
//        let concat = 
//            (List.ofSeq declTypeArgs, List.ofSeq argTypeArgs)
//            ||> List.map2 extractPartialSubstitution
//            |> List.concat
//
//        concat
//
//    override this.IsAvailable(context) =
//        context.IsBasicOrSmartCompletion && not context.IsQualified
//
//    override this.AddLookupItems(context, collector) =
//        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
//        if isNull reference then false else
//
//        let referenceOwner = reference.GetElement().As<IReferenceExpr>()
//        if isNull referenceOwner then false else
//
//        // todo: expected expression types
//        //   * if ... then f else {caret}
//        //   * x.Field <- {caret}
//        //   * { Field = {caret} }
//        //   * x.M(namedArg = {caret})
//        //   * let _: ... = {caret}
//
//        match getFunExprAndPosition referenceOwner with
//        | None -> false
//        | Some(funExpr, (groupIndex, paramIndex)) ->
//
//        // todo: it can become a function type after the substitution, move the check below
//        let funFcsType = funExpr.TryGetFcsType()
//        if isNull funFcsType || not funFcsType.IsFunctionType then false else
//
//        let getFunTypeArgs (fcsType: FSharpType) =
//            let rec loop acc (fcsType: FSharpType) =
//                if not fcsType.IsFunctionType then List.rev acc else
//
//                let acc = fcsType.GenericArguments.[0] :: acc
//                loop acc fcsType.GenericArguments.[1]
//            loop [] fcsType
//
//        let fcsFunParamTypes = getFunTypeArgs funFcsType
//
//        match List.tryItem groupIndex fcsFunParamTypes with
//        | None -> false
//        | Some(paramGroupType) ->
//
//        let paramType =
//            if paramGroupType.IsTupleType && not paramGroupType.IsStructTupleType then
//                let genericArguments = paramGroupType.GenericArguments
//                Seq.tryItem paramIndex genericArguments
//            elif paramIndex = 0 then
//                Some(paramGroupType)
//            else
//                None
//
//        match paramType with
//        | None -> false
//        | Some(fcsParamType) ->
//
//        let partialSubstitution =
//            let prefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(referenceOwner.IgnoreParentParens())
//            let binaryAppExpr = BinaryAppExprNavigator.GetByRightArgument(prefixAppExpr)
//            if isNull binaryAppExpr then [] else
//
//            let opReferenceExpr = binaryAppExpr.Operator
//            if isNull opReferenceExpr then [] else
//
//            let getLeftArgType (binaryAppExpr: IBinaryAppExpr) =
//                let arg = binaryAppExpr.LeftArgument
//                if isNotNull arg then arg.TryGetFcsType() else Unchecked.defaultof<_>
//
//            // todo: generalize for other operators
//            // todo: check isPredefined
//            match opReferenceExpr.ShortName with
//            | "|>" ->
//                let leftArgType = getLeftArgType binaryAppExpr
//                if isNull leftArgType then [] else
//
//                if fcsFunParamTypes.Length < 2 then [] else
//
//                let fcsParamType = List.last fcsFunParamTypes
//                extractPartialSubstitution fcsParamType leftArgType
//
//            | "||>" ->
//                let leftArgType = getLeftArgType binaryAppExpr
//                if isNull leftArgType || not leftArgType.IsTupleType then [] else
//
//                let tupleTypeArgs = leftArgType.GenericArguments
//                if tupleTypeArgs.Count <> 2 then [] else
//
//                let parameterGroupCount = fcsFunParamTypes.Length
//                if parameterGroupCount < 3 then [] else
//
//                let fcsParamGroups = 
//                    [ fcsFunParamTypes.[parameterGroupCount - 2]
//                      fcsFunParamTypes.[parameterGroupCount - 1] ]
//
//                (fcsParamGroups, List.ofSeq tupleTypeArgs)
//                ||> List.map2 extractPartialSubstitution
//                |> List.concat
//
//            | _ -> []
//
//        if not fcsParamType.IsFunctionType then false else
//
//        let fcsArgTypes =
//            let rec getFunTypeArgs acc (fcsType: FSharpType) =
//                if not fcsType.IsFunctionType || fcsType.GenericArguments.Count <> 2 then List.rev acc else
//
//                let acc = fcsType.GenericArguments.[0] :: acc
//                getFunTypeArgs acc fcsType.GenericArguments.[1]
//
//            getFunTypeArgs [] fcsParamType
//
//        let fcsArgTypesWithSubstitution =
//            fcsArgTypes |> List.map (fun fcsType -> fcsType.Instantiate(partialSubstitution))
//
//        let lambdaParamTypes = fcsArgTypesWithSubstitution |> List.map (fun fcsType -> fcsType.MapType(referenceOwner))
//
//        let paramNames = 
//            lambdaParamTypes
//            |> List.map (fun t ->
//                FSharpNamingService.createEmptyNamesCollection referenceOwner
//                |> FSharpNamingService.addNamesForType t
//                |> FSharpNamingService.prepareNamesCollection EmptySet.Instance referenceOwner
//                |> (fun names -> List.ofSeq names @ ["_"]))
//
//        let paramNamesText = 
//            paramNames
//            |> List.map (fun names ->
//                names
//                |> Seq.tryHead
//                |> Option.defaultValue "_")
//            |> String.concat " "
//
//        let fcsDisplayContext =
//            let displayContext = funExpr.TryGetFcsDisplayContext()
//            if isNull displayContext then FSharpDisplayContext.Empty else displayContext
//
//        let text =
//            fcsArgTypesWithSubstitution
//            |> List.map (fun arg -> arg.Format(fcsDisplayContext))
//            |> String.concat " -> "
//
//        let presentationText = $"fun {text} ->"
//        let info = GenerateLambdaInfo($"(fun {paramNamesText} -> ())", paramNames, Ranges = context.Ranges)
//
//        let item =
//            LookupItemFactory.CreateLookupItem(info)
//                .WithPresentation(fun _ -> TextualPresentation(RichText(presentationText), info) :> _)
//                .WithBehavior(fun _ -> GenerateLambdaBehavior(info) :> _)
//                .WithMatcher(fun _ -> TextualMatcher(presentationText, info) :> _)
//                .WithRelevance(CLRLookupItemRelevance.ExpectedTypeMatchLambda)
//
//        item.Placement.Location <- PlacementLocation.Top
//
//        collector.Add(item)
//
//        true
