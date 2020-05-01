module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

let tryGetNamedArg (expr: IFSharpExpression) =
    let binaryAppExpr = expr.As<IBinaryAppExpr>()
    if isNull binaryAppExpr then null else

    if binaryAppExpr.Operator.Reference.GetName() <> "=" then null else

    let refExpr = binaryAppExpr.LeftArgument.As<IReferenceExpr>()
    if isNull refExpr then null else

    refExpr.Reference.Resolve().DeclaredElement.As<IParameter>()

let getMatchingParameter (initialExpr: IFSharpExpression) =
    let expr = initialExpr.IgnoreInnerParens()
    let tupleExpr = TupleExprNavigator.GetByExpression(expr)
    let tupleExprContext = tupleExpr.IgnoreParentParens()

    let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(if isNull tupleExpr then expr else tupleExprContext)
    if isNull appExpr then null else

    let refExpr = appExpr.FunctionExpression.As<IReferenceExpr>()
    if isNull refExpr then null else

    use compilationContextCookie = CompilationContextCookie.OverrideOrCreate(expr.GetResolveContext())

    let parameter = tryGetNamedArg initialExpr
    if isNotNull parameter then parameter else

    let method = refExpr.Reference.Resolve().DeclaredElement.As<IMethod>()
    let parameters = method.Parameters
    if parameters.Count = 1 then parameters.[0] else

    let index = tupleExpr.Expressions.IndexOf(expr)
    if index < parameters.Count then parameters.[index] else null

[<Language(typeof<FSharpLanguage>)>]
type FSharpMethodInvocationUtil() =
    interface IFSharpMethodInvocationUtil with
        member x.GetMatchingParameter(expr) = getMatchingParameter expr
        member x.GetNamedArg(expr) = tryGetNamedArg expr
