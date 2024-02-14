module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.ObjExprUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree

module NewObjPostfixTemplate =
    let private isApplicableArgExpr (expr: IFSharpExpression) =
        expr :? IParenExpr || expr :? IUnitExpr

    let getReference (expr: IFSharpExpression) =
        match expr with
        | :? IReferenceExpr as refExpr -> refExpr.Reference
        | :? IPrefixAppExpr as appExpr ->
            match appExpr.FunctionExpression with
            | :? IReferenceExpr as refExpr when isApplicableArgExpr appExpr.ArgumentExpression -> refExpr.Reference
            | _ -> null
        | _ -> null

    let getTypeElement (reference: IReference) =
        if isNull reference then null else

        match reference.Resolve().DeclaredElement with
        | :? ITypeElement as typeElement -> typeElement
        | :? IConstructor as ctor -> ctor.ContainingType
        | _ -> null

    let createObjExpr (factory: IFSharpElementFactory) (expr: IFSharpExpression) =
        let text, argExpr =
            match expr with
            | :? IReferenceExpr as refExpr -> refExpr.GetText(), null
            | :? IPrefixAppExpr as appExpr ->
                match appExpr.FunctionExpression with
                | :? IReferenceExpr as refExpr ->
                    refExpr.GetText(), appExpr.ArgumentExpression
                | _ -> failwith "Unreachable"
            | _ -> failwith "Unreachable"

        let objExpr = factory.CreateExpr($"{{ new {text} with }}") :?> IObjExpr
        if isNotNull argExpr then
            ModificationUtil.AddChildAfter(objExpr.TypeName, argExpr.Copy()) |> ignore
        objExpr

    let isApplicableType (typeElement: ITypeElement) =
        typeElement :? IClass || typeElement :? IInterface &&
        not (typeElement.IsRecord() || typeElement.IsUnion())

    let isApplicableExpr (expr: IFSharpExpression) =
        let reference = getReference expr
        let typeElement = getTypeElement reference
        isApplicableType typeElement

