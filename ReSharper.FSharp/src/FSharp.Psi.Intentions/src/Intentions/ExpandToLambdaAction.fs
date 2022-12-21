namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType

[<ContextAction(Name = "Expand to lambda", Group = "F#",
                Description = "Expand function reference to lambda")>]
type ExpandToLambdaAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.Text = "Expand to lambda"

    override this.IsAvailable _ =
        let referenceExpr = dataProvider.GetSelectedElement<IReferenceExpr>()

        isValid referenceExpr &&

        let actualParametersCount =
            let outermostPrefixApp = getOutermostPrefixAppExpr(referenceExpr.IgnoreParentParens())
            match outermostPrefixApp with
            | :? IPrefixAppExpr as prefixApp -> ValueOption.Some prefixApp.Arguments.Count
            | _ -> ValueNone

        let expectedParametersCount =
            let declaredElement = referenceExpr.Reference.Resolve().DeclaredElement
            match declaredElement with
            | :? IFunction as f -> ValueSome f.Parameters.Count
            | :? IUnionCase as uc -> if uc.HasFields then ValueSome 1 else ValueSome 0
            | :? IFSharpMember as m -> ValueSome m.Mfv.CurriedParameterGroups.Count
            | :? IReferencePat as refPat ->
                match refPat.GetFcsSymbol() with
                | :? FSharpMemberOrFunctionOrValue as mfv -> ValueSome mfv.CurriedParameterGroups.Count
                | _ -> ValueNone
            | _ -> ValueNone

        match actualParametersCount, expectedParametersCount with
        | ValueSome x, ValueSome y -> x < y
        | _ -> false

    override x.ExecutePsiTransaction(_, _) =
        let referenceExpr = dataProvider.GetSelectedElement<IReferenceExpr>()
        let referenceFcsSymbol = referenceExpr.Reference.GetFcsSymbol()
        let factory = referenceExpr.CreateElementFactory()

        let getArgName =
            let mutable argI = 0
            fun () -> argI <- argI + 1; $"arg{argI}"

        let isSimpleMethodLike, isSingleParameter, paramNamesText =
            match referenceFcsSymbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                let parameterGroups = mfv.CurriedParameterGroups
                let isSimpleMethodLike = (mfv.IsMethod || mfv.IsConstructor) && parameterGroups.Count = 1

                isSimpleMethodLike, parameterGroups.Count = 1 && parameterGroups[0].Count <= 1,
                if isSimpleMethodLike && parameterGroups.Count = 1 then
                    if parameterGroups[0].Count = 0 then "()" else
                    parameterGroups[0]
                    |> Seq.map (fun x -> x.Name |> Option.defaultWith getArgName)
                    |> String.concat ", "
                else
                    parameterGroups
                    |> Seq.map (fun x -> if x.Count = 1 then (if isUnit x[0].Type then "()" else x[0].Name |> Option.defaultWith getArgName) else getArgName())
                    |> String.concat " "
            | :? FSharpUnionCase as unionCase ->
                let fields = unionCase.Fields
                true, fields.Count = 1,
                unionCase.Fields
                |> Seq.map (fun x -> StringUtil.Decapitalize(x.Name))
                |> String.concat ", "
            | _ -> false, true, "_"

        let newExpr =
            factory.CreateExpr(
                [|
                   "fun "
                   if isSimpleMethodLike && not isSingleParameter then "("
                   paramNamesText
                   if isSimpleMethodLike && not isSingleParameter then ")"
                   " -> "
                   referenceExpr.GetText()
                   if not isSimpleMethodLike then " "
                   if isSimpleMethodLike then "("
                   if paramNamesText <> "()" then paramNamesText
                   if isSimpleMethodLike then ")"
                |]
                |> String.concat "") //TODO: FIX func ()

        ModificationUtil.ReplaceChild(referenceExpr, newExpr.Copy())
        |> FSharpParensUtil.addParensIfNeeded
        |> ignore

        // TODO: shifting
        // TODO: deconstruction
        null
