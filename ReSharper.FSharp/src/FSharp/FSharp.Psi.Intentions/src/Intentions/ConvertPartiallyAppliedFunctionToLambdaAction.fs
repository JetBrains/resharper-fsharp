namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

module ToInfixAppExpr =
    let toInfixAppExpr (appExpr: IPrefixAppExpr) =
        let rightArgExpr = appExpr.ArgumentExpression.NotNull()
        let innerAppExpr = appExpr.FunctionExpression :?> IPrefixAppExpr
        let leftArgExpr = innerAppExpr.ArgumentExpression.NotNull()
        let opExpr = innerAppExpr.FunctionExpression :?> IReferenceExpr

        let opName = opExpr.Reference.GetName()

        let factory = appExpr.CreateElementFactory()
        let binaryAppExpr = factory.CreateBinaryAppExpr(opName, leftArgExpr, rightArgExpr)
        ModificationUtil.ReplaceChild(appExpr, binaryAppExpr)


[<ContextAction(Name = "ConvertPartiallyAppliedFunctionToLambdaAction", GroupType = typeof<FSharpContextActions>,
                Description = "Converts partially applied function to a lambda expression")>]
type ConvertPartiallyAppliedFunctionToLambdaAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let getAppliedArgCount (expr: IFSharpExpression) =
        let rec loop i (expr: IFSharpExpression) =
            let appExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr.IgnoreParentParens())
            if isNotNull appExpr then
                loop (i + 1) appExpr
            else
                i

        loop 0 expr

    override this.Text = "To lambda expression"

    override this.IsAvailable _ =
        let refExpr = dataProvider.GetSelectedElement<IReferenceExpr>()
        isNotNull refExpr &&

        isNull (BinaryAppExprNavigator.GetByOperator(refExpr)) &&

        let fcsSymbol = refExpr.Reference.GetFcsSymbol()
        isNotNull fcsSymbol &&

        match tryGetFunctionType fcsSymbol with
        | None -> false
        | Some fcsType ->

        let argTypes = FcsTypeUtil.getFunctionTypeArgs false fcsType
        not argTypes.IsEmpty &&

        let appliedArgCount = getAppliedArgCount refExpr
        appliedArgCount < argTypes.Length

    override this.ExecutePsiTransaction(_, _) =
        let refExpr = dataProvider.GetSelectedElement<IReferenceExpr>()
        let isOperator = isOperatorReferenceExpr refExpr

        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let hotspotsRegistry = HotspotsRegistry(refExpr.GetPsiServices())
        let factory = refExpr.CreateElementFactory()

        let reference = refExpr.Reference
        let substitution = reference.GetSymbolUse().GenericArguments
        let fcsSymbol = reference.GetFcsSymbol()
        let fcsType = tryGetFunctionType fcsSymbol |> Option.get

        let argTypes =
            let argTypes = FcsTypeUtil.getFunctionTypeArgs false fcsType

            let symbolUse = reference.GetSymbolUse()
            if isNull symbolUse then argTypes else argTypes |> List.map _.Instantiate(substitution)

        let argTypesCount = argTypes.Length
        let appliedArgCount = getAppliedArgCount refExpr
        let generateArgsCount = argTypesCount - appliedArgCount
        let convertToBinaryAppExpr = isOperator && argTypesCount = 2

        let generateArgTypes =
            argTypes
            |> List.skip appliedArgCount
            |> List.map _.MapType(refExpr)

        let outermostPrefixAppExpr = getOutermostPrefixAppExpr refExpr
        let usedNames = FSharpNamingService.getUsedNames [outermostPrefixAppExpr] EmptyList.InstanceList null true

        let paramNames =
            generateArgTypes
            |> List.map (fun t ->
                FSharpNamingService.createEmptyNamesCollection refExpr
                |> FSharpNamingService.addNamesForType t
                |> FSharpNamingService.prepareNamesCollection usedNames refExpr
                |> fun names ->
                    let names = List.ofSeq names @ ["_"]
                    usedNames.Add(names.Head.RemoveBackticks()) |> ignore
                    List.distinct names
            )
            |> List.toArray

        let paramPatterns = List.replicate generateArgsCount "_" |> String.concat " "
        let newArgsExprs = List.replicate generateArgsCount "()" |> String.concat " "

        let lambdaExpr = factory.CreateExpr($"fun {paramPatterns} -> () {newArgsExprs}") :?> ILambdaExpr

        let rec replaceLambdaExpr (bodyExpr: IFSharpExpression) n =
            if n = 0 then
                ModificationUtil.ReplaceChild(bodyExpr, outermostPrefixAppExpr) |> ignore
                ModificationUtil.ReplaceChild(outermostPrefixAppExpr, lambdaExpr) |> addParensIfNeeded :?> ILambdaExpr
            else
                let appExpr = bodyExpr :?> IPrefixAppExpr
                replaceLambdaExpr appExpr.FunctionExpression (n - 1)

        let lambdaExpr = replaceLambdaExpr lambdaExpr.Expression generateArgsCount
        let patterns = lambdaExpr.Patterns

        let bodyExpr =
            if convertToBinaryAppExpr then
                lambdaExpr.Expression.As() |> ToInfixAppExpr.toInfixAppExpr
            else
                lambdaExpr.Expression

        let rec getParamArgPairs (n: int) (bodyExpr: IFSharpExpression) : (IFSharpPattern * IReferenceExpr * string list) list =
            let createNodes i originalArgExpr =
                let names = paramNames[i]

                let pat = ModificationUtil.ReplaceChild(patterns[i], factory.CreatePattern(names[0], false))
                let newArgExpr = factory.CreateReferenceExpr(names[0])
                let argExpr = ModificationUtil.ReplaceChild(originalArgExpr, newArgExpr)
                pat, argExpr, names
    
            let rec loop n acc (bodyExpr: IFSharpExpression) =
                if n - appliedArgCount = 0 then acc else

                let i = n - appliedArgCount - 1
                let appExpr = bodyExpr :?> IPrefixAppExpr

                let pat, argExpr, names = createNodes i appExpr.ArgumentExpression
                let acc = (pat, argExpr, names) :: acc
                loop (n - 1) acc appExpr.FunctionExpression

            if convertToBinaryAppExpr then
                let binaryAppExpr = bodyExpr :?> IBinaryAppExpr

                let acc =
                    if appliedArgCount = 1 then [] else

                    let pat, argExpr, names = createNodes 0 binaryAppExpr.LeftArgument
                    [pat, argExpr, names]

                let pat, argExpr, names = createNodes acc.Length binaryAppExpr.RightArgument
                (pat, argExpr, names) :: acc
            else
                loop n [] bodyExpr

        getParamArgPairs argTypesCount bodyExpr
        |> List.iter (fun (pat, argExpr, names) ->
            let nameExpression = NameSuggestionsExpression(names)
            let nodes: ITreeNode[] = [| pat; argExpr |]
            hotspotsRegistry.Register(nodes, nameExpression)
        )

        Action<_>(fun textControl ->
            let endOffset = bodyExpr.GetDocumentStartOffset()
            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, endOffset).Invoke(textControl)
        )
