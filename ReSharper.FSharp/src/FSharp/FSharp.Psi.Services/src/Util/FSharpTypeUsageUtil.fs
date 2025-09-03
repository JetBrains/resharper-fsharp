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

let setParametersOwnerReturnTypeNoBind (decl: IFSharpTypeOwnerDeclaration) (fcsType: FSharpType) =
    let factory = decl.CreateElementFactory()
    let typeUsage = factory.CreateTypeUsage(fcsType.Format(), TypeUsageContext.TopLevel)
    fcsType, decl.SetTypeUsage(typeUsage)

let setFcsParametersOwnerReturnTypeNoBind (decl: IFSharpTypeOwnerDeclaration) (mfv: FSharpMemberOrFunctionOrValue) =
    let fcsReturnType =
        let fullType = mfv.FullType
        match decl with
        | :? IBinding as binding when fullType.IsFunctionType ->
            skipFunctionParameters fullType binding.ParametersDeclarations.Count
        | _ ->
            mfv.ReturnParameter.Type

    setParametersOwnerReturnTypeNoBind decl fcsReturnType

let setFcsParametersOwnerReturnType (decl: IFSharpTypeOwnerDeclaration) =
    let mfv = decl.GetFcsSymbolUse().Symbol.As<FSharpMemberOrFunctionOrValue>()
    let fcsReturnType, returnTypeUsage = setFcsParametersOwnerReturnTypeNoBind decl mfv
    bindAnnotations [ fcsReturnType, returnTypeUsage ]

let setTypeOwnerType (fcsType: FSharpType) (decl: IFSharpTypeUsageOwnerNode) =
    let factory = decl.CreateElementFactory()
    let typeUsage = decl.SetTypeUsage(factory.CreateTypeUsage(fcsType.Format(), TypeUsageContext.TopLevel))
    bindAnnotations [ fcsType, typeUsage ]


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
