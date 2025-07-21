[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpTypeUsageUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

let updateTypeUsage (fcsType: FSharpType) (typeUsage: ITypeUsage) =
    let factory = typeUsage.CreateElementFactory()
    let newTypeUsage = factory.CreateTypeUsage(fcsType.Format(), TypeUsageContext.TopLevel)
    let newTypeUsage = ModificationUtil.ReplaceChild(typeUsage, newTypeUsage)
    let typeUsage =
        if RedundantParenTypeUsageAnalyzer.needsParens newTypeUsage newTypeUsage then
            let parenTypeUsage = factory.CreateTypeUsage("(int)", TypeUsageContext.TopLevel) :?> IParenTypeUsage
            parenTypeUsage.SetInnerTypeUsage(newTypeUsage) |> ignore
            ModificationUtil.ReplaceChild(newTypeUsage, parenTypeUsage): ITypeUsage
        else
            newTypeUsage

    bindAnnotations [ fcsType, typeUsage ]


let getTupleParentNavigationPath (expr: IFSharpExpression) =
    let rec loop (expr: IFSharpExpression) acc =
        let expr = expr.IgnoreParentParens()
        let tupleExpr = TupleExprNavigator.GetByExpression(expr)
        if isNotNull tupleExpr then
            let itemIndex = tupleExpr.ExpressionsEnumerable.IndexOf(expr)
            loop tupleExpr (itemIndex :: acc)
        else
            expr, List.rev acc

    loop expr []

let rec navigateTuplePath (typeUsage: ITypeUsage) path : ITypeUsage =
    match typeUsage.IgnoreInnerParens(), path with
    | :? ITupleTypeUsage as tupleTypeUsage, step :: rest when tupleTypeUsage.ItemsEnumerable.Count() > step ->
        navigateTuplePath tupleTypeUsage.Items[step] rest

    | _, _ :: _ -> null

    | typeUsage, _ -> typeUsage.IgnoreParentParens()
