module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.ExpandToLambdaAction

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.Util
open JetBrains.ReSharper.Psi.Tree

[<ContextAction(Name = "Expand to lambda", Group = "F#",
                Description = "Expand partial application to lambda")>]
type ExpandToLambdaAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    override this.IsAvailable _ =
        let referenceExpr = dataProvider.GetSelectedElement<IReferenceExpr>()

        isNotNull referenceExpr &&
        isNull (PrefixAppExprNavigator.GetByFunctionExpression(referenceExpr.IgnoreParentParens())) &&
        isValid referenceExpr &&

        let declaredElement = referenceExpr.Reference.Resolve().DeclaredElement
        isNotNull (declaredElement.As<IFunction>())

    override this.Text = "Expand to lambda"

    override x.ExecutePsiTransaction(_, _) =
        let referenceExpr = dataProvider.GetSelectedElement<IReferenceExpr>()
        let needParens = ParenExprNavigator.GetByInnerExpression(referenceExpr) |> isNull
        let factory = referenceExpr.CreateElementFactory()

        let referenceSymbol = referenceExpr.Reference.GetFcsSymbol()
        let getNewName =
            let mutable argI = 0
            fun () -> argI <- argI + 1; $"arg{argI}"

        let isMethod, parameters =
            match referenceSymbol with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.IsMethod, mfv.CurriedParameterGroups
            | _ -> false, [||]

        let paramNamesText =
            if isMethod && parameters.Count = 1 then
                "(" + (parameters[0] |> Seq.map (fun x -> x.Name |> Option.defaultWith getNewName) |> String.concat ", ") + ")"
            else
            parameters
            |> Seq.map (fun x -> if x.Count = 1 then x[0].Name |> Option.defaultWith getNewName else getNewName())
            |> String.concat " "

        let newExpr =
            factory.CreateExpr(
                [| if needParens then "("
                   "fun "
                   paramNamesText
                   " -> "
                   referenceExpr.QualifiedName
                   if not isMethod then " "
                   paramNamesText
                   if needParens then ")"
                |]
                |> String.concat "")

        replaceWithCopy referenceExpr newExpr
        // let refExprIdent = referenceExpr.Indent
        //
        // let newExpr = ModificationUtil.ReplaceChild(referenceExpr, newExpr.Copy())
        // let newExprIndent = newExpr.IgnoreInnerParens().As<ILambdaExpr>().Expression.Indent
        // let indentDiff = newExprIndent - refExprIdent
        // shiftNode indentDiff (newExpr.Parent.Parent :?> _)

        null
