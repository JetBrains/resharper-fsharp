namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type NamedArgumentsRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let tailNodeTypes =
        [| FSharpTokenType.WHITESPACE
           FSharpTokenType.EQUALS
           FSharpTokenType.WHITESPACE
           TailType.CaretTokenNodeType.Instance |]

    let getNamedFields (fields: FSharpField seq) =
        fields
        |> Seq.filter (fun field -> not field.IsNameGenerated)
        |> Seq.cast<FSharpSymbol>
        |> List.ofSeq

    let getNamedParameters (parameters: FSharpParameter seq) =
        parameters
        |> Seq.filter _.Name.IsSome
        |> Seq.cast<FSharpSymbol>
        |> List.ofSeq

    let getPatNamedArgTargets (parametersOwnerPat: IParametersOwnerPat) =
        let referenceName = parametersOwnerPat.ReferenceName
        if isNull referenceName then [] else

        match referenceName.Reference.GetFcsSymbol() with
        | FieldsOwner fields ->
            let fieldsPat = parametersOwnerPat.Parameters.SingleItem.As<INamedUnionCaseFieldsPat>()
            let usedNames =
                if isNull fieldsPat then HashSet() else
                fieldsPat.FieldPatterns |> Seq.map _.ShortName |> HashSet

            getNamedFields fields |> List.filter (_.DisplayNameCore >> usedNames.Contains >> not)

        | _ -> []

    let getArgExpr (context: FSharpCodeCompletionContext) =
        let refExpr = getRefExpr context
        let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(refExpr)
        if FSharpArgumentsUtil.HasNamedArgStructure(binaryAppExpr) then
            binaryAppExpr :> IFSharpExpression
        else
            refExpr

    let isSettableMemberAccessible (context: ITreeNode) (fcsSymbol: FSharpSymbol) =
        let accessibility =
            match fcsSymbol with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.SetterMethod.Accessibility
            | _ -> fcsSymbol.Accessibility

        if accessibility.IsPublic then true else

        let typeMember =
            match fcsSymbol.GetDeclaredElement(context.GetPsiModule()) with
            | :? IProperty as property -> property.Setter :> ITypeMember
            | :? IField as field -> field
            | _ -> null

        if isNull typeMember then not (accessibility.IsPrivate || accessibility.IsProtected) else

        AccessUtil.IsSymbolAccessible(typeMember, FSharpAccessContext(context))

    let splitPrecedingArgs (fcsSymbol: FSharpSymbol) (precedingArgs: IFSharpExpression list) =
        let argNames = precedingArgs |> List.map FSharpArgumentsUtil.TryGetNamedArgName |> Array.ofList
        let namedArgs =
            match fcsSymbol with
            | FieldsOwner fields -> getFieldNamedArgs fields argNames
            | _ -> argNames

        let positionalCount = namedArgs |> Array.takeWhile isNull |> Array.length
        positionalCount, positionalCount < namedArgs.Length

    let getCandidateNamedArgTargets (context: ITreeNode) (precedingArgs: IFSharpExpression list)
            (usedNames: HashSet<string>) (fcsSymbol: FSharpSymbol) =
        let unnamedArgCount, _ = splitPrecedingArgs fcsSymbol precedingArgs
        match fcsSymbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when canHaveNamedArguments mfv ->
            let parameters = mfv.CurriedParameterGroups[0]
            if not (acceptsUnnamedArgs parameters unnamedArgCount) then [] else

            let returnType = if mfv.IsConstructor then mfv.ApparentEnclosingType else mfv.ReturnParameter.Type
            let settableMembers = getSettableMembers returnType |> List.filter (isSettableMemberAccessible context)
            let targetNames = getNamedParameters parameters @ settableMembers |> Seq.map _.DisplayNameCore
            if not (usedNames.IsSubsetOf(targetNames)) then [] else

            let remainingParameters = parameters |> Seq.skip (min unnamedArgCount parameters.Count)
            getNamedParameters remainingParameters @ settableMembers

        | FieldsOwner fields ->
            if unnamedArgCount > fields.Count then [] else

            fields |> Seq.skip unnamedArgCount |> getNamedFields

        | _ -> []

    let isNamedArgsOwner (reference: FSharpSymbolReference) =
        match reference.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as mfv -> canHaveNamedArguments mfv
        | FieldsOwner fields -> fields |> Seq.exists (fun field -> not field.IsNameGenerated)
        | :? FSharpEntity -> true
        | _ -> false

    let getNamedArgsOwnerReference (argExpr: IFSharpExpression) =
        if not (FSharpArgumentsUtil.IsTopLevelArg(argExpr)) then null else

        let argsOwner = FSharpMethodInvocationUtil.getArgsOwner argExpr
        let reference = FSharpMethodInvocationUtil.getReference argsOwner
        if isNull reference || not (isNamedArgsOwner reference) then null else

        reference

    let tryGetExprArgs (context: FSharpCodeCompletionContext) =
        let argExpr = getArgExpr context
        let reference = getNamedArgsOwnerReference argExpr
        if isNull reference then None else

        let args =
            match TupleExprNavigator.GetByExpression(argExpr) with
            | null -> []
            | tupleExpr -> List.ofSeq tupleExpr.Expressions

        let usedNames =
            args
            |> List.filter ((!=) argExpr)
            |> List.choose (FSharpArgumentsUtil.TryGetNamedArgName >> Option.ofObj)
            |> HashSet

        Some(reference, List.takeWhile ((!=) argExpr) args, usedNames)

    let getExprNamedArgTargets (context: FSharpCodeCompletionContext) =
        match tryGetExprArgs context with
        | None -> []
        | Some(reference, precedingArgs, usedNames) ->

        match getMethodCandidates reference "NamedArgumentsRule" with
        | None -> []
        | Some(_, symbolUses) ->

        symbolUses
        |> List.collect (fun symbolUse ->
            getCandidateNamedArgTargets context.NodeInFile precedingArgs usedNames symbolUse.Symbol)
        |> List.distinctBy _.DisplayNameCore
        |> List.filter (_.DisplayNameCore >> usedNames.Contains >> not)

    let getNamedArgTargets (context: FSharpCodeCompletionContext) =
        match getParametersOwnerPatFromReference context.ReparsedContext.Reference with
        | null -> getExprNamedArgTargets context
        | parametersOwnerPat -> getPatNamedArgTargets parametersOwnerPat

    let isInExprNamedArgsSection (context: FSharpCodeCompletionContext) =
        match tryGetExprArgs context with
        | None -> false
        | Some(reference, precedingArgs, _) ->

        let _, hasNamedArgs = splitPrecedingArgs (reference.GetFcsSymbol()) precedingArgs
        hasNamedArgs

    let isInNamedArgsSection (context: FSharpCodeCompletionContext) =
        match getParametersOwnerPatFromReference context.ReparsedContext.Reference with
        | null -> isInExprNamedArgsSection context
        | parametersOwnerPat -> parametersOwnerPat.Parameters.SingleItem :? INamedUnionCaseFieldsPat

    let isFcsNamedArgItem (fcsSymbols: FSharpSymbol list) (item: ILookupItem) =
        match item with
        | :? FcsLookupItem as fcsItem ->
            match fcsItem.FcsSymbol with
            | :? FSharpParameter -> true
            | fcsSymbol -> fcsSymbols |> List.exists _.IsEffectivelySameAs(fcsSymbol)
        | _ -> false

    let isParameterTarget (fcsSymbol: FSharpSymbol) =
        match fcsSymbol with
        | :? FSharpParameter -> true
        | :? FSharpField as field ->
            field.IsUnionCaseField || field.DeclaringEntity |> Option.exists _.IsFSharpExceptionDeclaration
        | _ -> false

    let createItem (context: FSharpCodeCompletionContext) (emphasize: bool) (fcsSymbol: FSharpSymbol) =
        let info = FcsSymbolInfo(fcsSymbol.DisplayNameCore, fcsSymbol, context, Ranges = context.Ranges)

        let text = RichText(info.Text)
        LookupUtil.AddInformationText(text, "=")
        if emphasize then
            LookupUtil.AddEmphasize(text, TextRange(0, info.Text.Length))

        let item =
            LookupItemFactory
                .CreateLookupItem(info)
                .WithPresentation(fun _ -> TextPresentation(text, getIconId fcsSymbol, info, DisplayTypeName = info.TypeText))
                .WithBehavior(fun _ -> TextualBehavior(info))
                .WithMatcher(LookupItemMatcher.Literal)

        item.PutKey(CompletionKeys.IsNamedArgumentKey)
        item.SetTailType(SimpleTailType(" = ", tailNodeTypes, SkipTypings = [| " = "; "= " |]))
        item.WithRelevance(CLRLookupItemRelevance.NamedArguments)

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified

    override this.AddLookupItems(context, collector) =
        let targets = getNamedArgTargets context
        if targets.IsEmpty then false else

        let inNamedArgsSection = isInNamedArgsSection context
        for fcsSymbol in targets do
            let emphasize = inNamedArgsSection && isParameterTarget fcsSymbol
            collector.Add(createItem context emphasize fcsSymbol)

        false

    override this.TransformItems(context, collector) =
        if isInNamedArgsSection context then
            collector.RemoveWhere(fun item -> not (item.IsNamedArgument()))
            FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
        else
            collector.RemoveWhere(isFcsNamedArgItem (getNamedArgTargets context))
