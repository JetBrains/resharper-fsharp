namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
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

        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let hotspotsRegistry = HotspotsRegistry(refExpr.GetPsiServices())
        let factory = refExpr.CreateElementFactory()

        let fcsSymbol = refExpr.Reference.GetFcsSymbol()
        let fcsType = tryGetFunctionType fcsSymbol |> Option.get
        let argTypes = FcsTypeUtil.getFunctionTypeArgs false fcsType

        let appliedArgCount = getAppliedArgCount refExpr
        let generateArgsCount = argTypes.Length - appliedArgCount

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

        let rec getParamArgPairs n (bodyExpr: IFSharpExpression) =
            let rec loop n acc (bodyExpr: IFSharpExpression) =
                if n - appliedArgCount = 0 then acc else

                let i = n - appliedArgCount - 1
                let names = paramNames[i]

                let pat = ModificationUtil.ReplaceChild(patterns[i], factory.CreatePattern(names[0], false))
                let appExpr = bodyExpr :?> IPrefixAppExpr
                let newArgExpr = factory.CreateReferenceExpr(names[0])
                let argExpr = ModificationUtil.ReplaceChild(appExpr.ArgumentExpression, newArgExpr)

                let acc = (pat, argExpr, names) :: acc
                loop (n - 1) acc appExpr.FunctionExpression

            loop n [] bodyExpr

        getParamArgPairs argTypes.Length lambdaExpr.Expression
        |> List.iter (fun (pat, argExpr, names) ->
            let nameExpression = NameSuggestionsExpression(names)
            let nodes: ITreeNode[] = [| pat; argExpr |]
            hotspotsRegistry.Register(nodes, nameExpression)
        )

        Action<_>(fun textControl ->
            let endOffset = lambdaExpr.Expression.GetDocumentStartOffset()
            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, endOffset).Invoke(textControl)
        )
