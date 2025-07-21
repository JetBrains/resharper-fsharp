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

let setParametersOwnerReturnTypeNoBind (decl: IParameterOwnerMemberDeclaration) (mfv: FSharpMemberOrFunctionOrValue) =
    let fcsReturnType =
        let fullType = mfv.FullType
        if decl :? IBinding && fullType.IsFunctionType then
            skipFunctionParameters fullType decl.ParametersDeclarations.Count
        else
            mfv.ReturnParameter.Type

    let factory = decl.CreateElementFactory()
    let typeUsage = factory.CreateTypeUsage(fcsReturnType.Format(), TypeUsageContext.TopLevel)
    let anchor = decl.EqualsToken

    let returnTypeInfo = ModificationUtil.AddChildBefore(anchor, factory.CreateReturnTypeInfo(typeUsage))
    fcsReturnType, returnTypeInfo.ReturnType

let setParametersOwnerReturnType (decl: IParameterOwnerMemberDeclaration) =
    let mfv = decl.GetFcsSymbolUse().Symbol.As<FSharpMemberOrFunctionOrValue>()
    let fcsReturnType, returnTypeUsage = setParametersOwnerReturnTypeNoBind decl mfv
    bindAnnotations [ fcsReturnType, returnTypeUsage ]


let rec skipParameters paramsToSkipCount (typeUsage: ITypeUsage) =
    if paramsToSkipCount = 0 then typeUsage else

    let funTypeUsage = typeUsage.As<IFunctionTypeUsage>()
    if isNull funTypeUsage then null else

    skipParameters (paramsToSkipCount - 1) funTypeUsage.ReturnTypeUsage


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

let rec navigateTuplePath path (typeUsage: ITypeUsage) : ITypeUsage =
    match typeUsage.IgnoreInnerParens(), path with
    | :? ITupleTypeUsage as tupleTypeUsage, step :: rest when tupleTypeUsage.ItemsEnumerable.Count() > step ->
        navigateTuplePath rest tupleTypeUsage.Items[step]

    | _, _ :: _ -> null

    | typeUsage, _ -> typeUsage.IgnoreParentParens()
