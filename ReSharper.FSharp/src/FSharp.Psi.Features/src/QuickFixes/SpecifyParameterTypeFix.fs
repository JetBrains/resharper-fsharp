namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Resources.Shell

type SpecifyParameterTypeFix(error: IndeterminateTypeError) =
    inherit FSharpQuickFixBase()

    let refExpr = error.RefExpr
    let qualifierRefExpr = if isNotNull refExpr then refExpr.Qualifier.As<IReferenceExpr>() else null

    override this.Text = $"Annotate '{qualifierRefExpr.ShortName}' type"

    override this.IsAvailable _ =
        isValid qualifierRefExpr && isNull qualifierRefExpr.Qualifier &&

        let reference = qualifierRefExpr.Reference
        let mfv = reference.GetFSharpSymbol().As<FSharpMemberOrFunctionOrValue>()
        isNotNull mfv && not mfv.IsModuleValueOrMember && not mfv.FullType.IsGenericParameter &&

        let pat =
            let refPat = reference.Resolve().DeclaredElement.As<ILocalReferencePat>().IgnoreParentParens()
            match TuplePatNavigator.GetByPattern(refPat).IgnoreParentParens() with
            | null -> refPat
            | tuplePat -> tuplePat

        isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(pat))

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())

        let reference = refExpr.QualifierReference
        let refPat = reference.Resolve().DeclaredElement.As<ILocalReferencePat>()

        let symbolUse = reference.GetSymbolUse()
        let fcsSymbol = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue

        SpecifyTypes.specifyParameterType symbolUse.DisplayContext fcsSymbol.FullType refPat
