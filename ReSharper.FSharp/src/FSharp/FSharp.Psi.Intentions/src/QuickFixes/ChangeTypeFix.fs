namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<AbstractClass>]
type ChangeTypeFixBase() =
    inherit FSharpQuickFixBase()

    let canUpdateType (decl: ITreeNode) =
        decl :? IFSharpTypeUsageOwnerNode ||
        decl :? IFSharpParameterDeclaration

    abstract DeclaredElement: IDeclaredElement
    abstract TargetFcsType: FSharpType 

    abstract GetDeclarations: unit -> ITreeNode[]
    default this.GetDeclarations() =
        match this.DeclaredElement with
        | null -> EmptyArray.Instance
        | declaredElement -> declaredElement.GetDeclarations() |> Seq.cast |> Array.ofSeq

    override this.Text =
        $"Change type of '{this.DeclaredElement.GetSourceName()}' to '{this.TargetFcsType.Format()}'"

    override this.IsAvailable _ =
        let decls = this.GetDeclarations()
        decls |> Array.isEmpty |> not &&

        let fcsType = this.TargetFcsType
        decls |> Array.forall (fun decl -> canUpdateType decl && isFcsTypeAccessible decl fcsType)

    override this.ExecutePsiTransaction _ =
        let decls = this.GetDeclarations()
        let firstDecl = Array.head decls
        use writeCookie = WriteLockCookie.Create(firstDecl.IsPhysical())

        for decl in decls do
            match decl with
            | :? IReferencePat as refPat ->
                let paramDecl = refPat.TryGetContainingParameterDeclarationPattern()
                specifyPatternType this.TargetFcsType paramDecl

            | :? IFSharpTypeUsageOwnerNode as typeOwnerDecl ->
                FSharpTypeUsageUtil.setTypeOwnerType this.TargetFcsType typeOwnerDecl

            | _ -> ()


type ChangeParameterTypeFromArgumentFix(expr, fcsDiagnosticInfo: FcsCachedDiagnosticInfo) =
    inherit ChangeTypeFixBase()

    new (error: TypeEquationError) =
        ChangeParameterTypeFromArgumentFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeParameterTypeFromArgumentFix(error.Expr, error.DiagnosticInfo)

    override this.Text =
        $"Change type of parameter to '{this.TargetFcsType}'"

    override this.TargetFcsType =
        fcsDiagnosticInfo.TypeMismatchData.ActualType

    override this.DeclaredElement =
        let param = getMatchingParameter expr
        param.As<IFSharpParameter>()

    override this.GetDeclarations() =
        match this.DeclaredElement with
        | :? IFSharpParameter as fsParam -> fsParam.GetParameterOriginDeclarations() |> Seq.cast |> Array.ofSeq
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

        specifyPatternType expectedFcsType refPat


type ChangeTypeFromElementReferenceFix(expr, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase()

    new (error: TypeEquationError) =
        ChangeTypeFromElementReferenceFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeTypeFromElementReferenceFix(error.Expr, error.DiagnosticInfo)

    override this.TargetFcsType =
        fcsDiagnosticInfo.TypeMismatchData.ExpectedType

    override this.DeclaredElement =
        let refExpr = expr.As<IReferenceExpr>()
        if isNull refExpr then null else

        refExpr.Reference.Resolve().DeclaredElement


type ChangeTypeFromRecordFieldBindingFix(expr, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase()

    new (error: TypeEquationError) =
        ChangeTypeFromRecordFieldBindingFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeTypeFromRecordFieldBindingFix(error.Expr, error.DiagnosticInfo)

    override this.TargetFcsType =
        fcsDiagnosticInfo.TypeMismatchData.ActualType

    override this.DeclaredElement =
        let binding = RecordFieldBindingNavigator.GetByExpression(expr.IgnoreParentParens())
        if isNull binding then null else

        binding.ReferenceName.Reference.Resolve().DeclaredElement


type ChangeTypeFromSetExprFix(expr, fcsDiagnosticInfo) =
    inherit ChangeTypeFixBase()

    new (error: TypeEquationError) =
        ChangeTypeFromSetExprFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeConstraintMismatchError) =
        ChangeTypeFromSetExprFix(error.Expr, error.DiagnosticInfo)

    override this.TargetFcsType =
        fcsDiagnosticInfo.TypeMismatchData.ActualType

    override this.DeclaredElement =
        let setExpr = SetExprNavigator.GetByRightExpression(expr.IgnoreParentParens())
        if isNull setExpr then null else

        let refExpr = setExpr.LeftExpression.As<IReferenceExpr>()
        if isNull refExpr then null else

        refExpr.Reference.Resolve().DeclaredElement
