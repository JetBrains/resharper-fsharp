namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open FSharp.Compiler.Diagnostics.ExtendedData
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<AbstractClass>]
type ChangeTypeFixBase(expr: IFSharpExpression, fcsDiagnosticInfo: FcsCachedDiagnosticInfo) =
    inherit FSharpQuickFixBase()

    let canUpdateType (decl: ITreeNode) =
        decl :? IFSharpTypeUsageOwnerNode ||
        decl :? IFSharpParameterDeclaration

    member this.TargetFcsType =
        use pinCheckResultsCookie = expr.FSharpFile.PinTypeCheckResults(true, "ChangeTypeFixBase")
        this.GetTargetFcsType(fcsDiagnosticInfo.TypeMismatchData)

    abstract DeclaredElement: IDeclaredElement
    abstract GetTargetFcsType: TypeMismatchDiagnosticExtendedData -> FSharpType 

    abstract GetDeclarations: unit -> (ITreeNode * FSharpSymbol)[]
    default this.GetDeclarations() =
        match this.DeclaredElement with
        | null -> EmptyArray.Instance
        | declaredElement ->
            declaredElement.GetDeclarations()
            |> Seq.map (fun decl ->
                let fsDecl = decl :?> IFSharpDeclaration
                let fcsSymbol = fsDecl.GetFcsSymbol()
                fsDecl :> ITreeNode, fcsSymbol
            )
            |> Array.ofSeq

    // todo: use IDeclaration
    abstract SetType: declTreeNode: ITreeNode * FSharpSymbol * FSharpType -> unit
    default this.SetType(declTreeNode: ITreeNode, _, fcsType) =
        match declTreeNode with
        | :? IReferencePat as refPat ->
            let paramDecl = refPat.TryGetContainingParameterDeclarationPattern()
            TypeAnnotationUtil.specifyPatternType fcsType paramDecl

        | :? IFSharpTypeUsageOwnerNode as typeOwnerDecl ->
            FSharpTypeUsageUtil.setTypeOwnerType fcsType typeOwnerDecl

        | _ -> ()

    override this.Text =
        $"Change type of '{this.DeclaredElement.GetSourceName()}' to '{this.TargetFcsType.Format()}'"

    override this.IsAvailable _ =
        let decls = this.GetDeclarations()
        decls |> Array.isEmpty |> not &&

        let fcsType = this.TargetFcsType
        decls |> Array.forall (fun (decl, _) -> canUpdateType decl && isFcsTypeAccessible decl fcsType)

    override this.ExecutePsiTransaction _ =
        let decls = this.GetDeclarations()
        let firstDecl = Array.head decls |> fst :?> IFSharpTreeNode
        use writeCookie = WriteLockCookie.Create(firstDecl.IsPhysical())

        let targetFcsType =
            use pinCheckResultsCookie = firstDecl.FSharpFile.PinTypeCheckResults(true, "ChangeTypeFixBase")
            this.GetTargetFcsType(fcsDiagnosticInfo.TypeMismatchData)

        for decl, fcsSymbol in decls do
            this.SetType(decl, fcsSymbol, targetFcsType)


// todo: test signatures, virtual/abstract members
type ChangeParameterTypeFromArgumentFix(expr, fcsDiagnosticInfo: FcsCachedDiagnosticInfo) =
    inherit ChangeTypeFixBase(expr, fcsDiagnosticInfo)

    new (error: TypeEquationError) =
        ChangeParameterTypeFromArgumentFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeParameterTypeFromArgumentFix(error.Expr, error.DiagnosticInfo)

    override this.Text =
        $"Change type of parameter to '{this.TargetFcsType.Format()}'"

    override this.GetTargetFcsType(data) =
        data.ActualType

    override this.DeclaredElement =
        let param = getMatchingParameter expr
        param.As<IFSharpParameter>()

    // todo: test signatures, virtual/abstract members
    override this.GetDeclarations() =
        match this.DeclaredElement with
        | :? IFSharpParameter as fsParam ->
            fsParam.GetParameterOriginDeclarations()
            |> Seq.map (fun decl -> decl :> ITreeNode, Unchecked.defaultof<FSharpSymbol>)
            |> Seq.toArray
        | _ -> EmptyArray.Instance


type ChangeLocalType(expr, fcsDiagnosticInfo: FcsCachedDiagnosticInfo) =
    inherit FSharpQuickFixBase()

    let expectedFcsType = fcsDiagnosticInfo.TypeMismatchData.ExpectedType

    let getLocalValueDecl (expr: IFSharpExpression) : IReferencePat =
        match expr with
        | :? IReferenceExpr as refExpr -> refExpr.Reference.Resolve().DeclaredElement.As()
        | _ -> null

    let refPat = getLocalValueDecl expr

    new (error: TypeEquationError) =
        ChangeLocalType(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeLocalType(error.Expr, error.DiagnosticInfo)


    override this.Text =
        $"Change type of '{refPat.SourceName}' to '{expectedFcsType.Format()}'"

    override this.IsAvailable _ = isNotNull refPat

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        TypeAnnotationUtil.specifyPatternType expectedFcsType refPat


type ChangeTypeFromElementReferenceFix(expr, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase(expr, fcsDiagnosticInfo)

    new (error: TypeEquationError) =
        ChangeTypeFromElementReferenceFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeTypeFromElementReferenceFix(error.Expr, error.DiagnosticInfo)

    override this.GetTargetFcsType(data) =
        data.ExpectedType

    override this.DeclaredElement =
        let refExpr = expr.As<IReferenceExpr>()
        if isNull refExpr then null else

        refExpr.Reference.Resolve().DeclaredElement


type ChangeReturnTypeFromInvocationFix(expr: IFSharpExpression, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase(expr, fcsDiagnosticInfo)

    new (error: TypeEquationError) =
        ChangeReturnTypeFromInvocationFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeReturnTypeFromInvocationFix(error.Expr, error.DiagnosticInfo)

    override this.GetTargetFcsType(data) =
        data.ExpectedType

    override this.DeclaredElement =
        let appExpr = expr.As<IPrefixAppExpr>()
        if isNull appExpr then null else

        let reference = appExpr.InvokedFunctionReference
        if isNull reference then null else

        let mfv = reference.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
        if isNull mfv then null else

        if mfv.CurriedParameterGroups.Count <> appExpr.AppliedExpressions.Count then null else

        reference.Resolve().DeclaredElement

    override this.SetType(decl, _, fcsType) =
        let decl = FSharpParameterOwnerDeclarationNavigator.Unwrap(decl)

        let lambdaParamsCount =
            let bindingParamDeclCount =
                match decl with
                | :? IParameterOwnerMemberDeclaration as paramOwnerDecl -> paramOwnerDecl.ParametersDeclarations.Count
                | _ -> 0

            let mfv = decl.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
            mfv.CurriedParameterGroups.Count - bindingParamDeclCount

        let decl = decl.As<IFSharpTypeOwnerDeclaration>()
        if isNull decl.TypeUsage then
            FSharpTypeUsageUtil.setFcsParametersOwnerReturnType decl

        decl.TypeUsage
        |> FSharpTypeUsageUtil.skipParameters lambdaParamsCount
        |> FSharpTypeUsageUtil.updateTypeUsage fcsType


type ChangeTypeFromRecordFieldBindingFix(expr, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase(expr, fcsDiagnosticInfo)

    new (error: TypeEquationError) =
        ChangeTypeFromRecordFieldBindingFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeTypeFromRecordFieldBindingFix(error.Expr, error.DiagnosticInfo)

    override this.GetTargetFcsType(data) =
        data.ActualType

    override this.DeclaredElement =
        let binding = RecordFieldBindingNavigator.GetByExpression(expr.IgnoreParentParens())
        if isNull binding then null else

        binding.ReferenceName.Reference.Resolve().DeclaredElement


type ChangeTypeFromSetExprFix(expr, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase(expr, fcsDiagnosticInfo)

    new (error: TypeEquationError) =
        ChangeTypeFromSetExprFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeTypeFromSetExprFix(error.Expr, error.DiagnosticInfo)

    override this.GetTargetFcsType(data) =
        data.ActualType

    override this.DeclaredElement =
        let setExpr = SetExprNavigator.GetByRightExpression(expr.IgnoreParentParens())
        if isNull setExpr then null else

        let refExpr = setExpr.LeftExpression.As<IReferenceExpr>()
        if isNull refExpr then null else

        refExpr.Reference.Resolve().DeclaredElement
