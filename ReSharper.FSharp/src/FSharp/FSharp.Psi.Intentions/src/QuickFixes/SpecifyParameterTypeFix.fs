namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl.Special
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type SpecifyTypeFixBase(refExpr: IQualifiedExpr) =
    inherit FSharpQuickFixBase()

    member val QualifierRefExpr =
        if isNotNull refExpr then refExpr.Qualifier.As<IReferenceExpr>() else null

    override this.Text =
        let symbolUse = this.QualifierRefExpr.Reference.GetSymbolUse()
        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let mfvType = mfv.FullType.Format(symbolUse.DisplayContext.WithShortTypeNames(true))
        $"Add '{mfvType}' type annotation to '{this.QualifierRefExpr.ShortName}'"

    abstract IsApplicable: mfv: FSharpMemberOrFunctionOrValue -> bool
    abstract IsApplicable: declaredElement: IDeclaredElement -> bool
    abstract IsApplicable: decl: IDeclaration -> bool

    abstract SpecifyType: decl: IDeclaration * mfv: FSharpMemberOrFunctionOrValue * displayContext: FSharpDisplayContext -> unit

    override this.IsAvailable _ =
        isValid this.QualifierRefExpr &&

        let reference = this.QualifierRefExpr.Reference
        let mfv = reference.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
        isNotNull mfv && not mfv.FullType.IsGenericParameter && this.IsApplicable(mfv) &&

        let declaredElement = reference.Resolve().DeclaredElement
        isNotNull declaredElement && this.IsApplicable(declaredElement) &&

        let declarations = declaredElement.GetDeclarations()
        declarations.Count = 1 && this.IsApplicable(declarations[0])

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(this.QualifierRefExpr.IsPhysical())

        let reference = this.QualifierRefExpr.Reference
        let declaration = reference.Resolve().DeclaredElement.GetDeclarations().[0]

        let symbolUse = reference.GetSymbolUse()
        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue

        this.SpecifyType(declaration, mfv, symbolUse.DisplayContext)


type SpecifyParameterTypeFix(qualifiedExpr: IQualifiedExpr) =
    inherit SpecifyTypeFixBase(qualifiedExpr)

    new (error: IndeterminateTypeError) =
        SpecifyParameterTypeFix(error.RefExpr)

    new (error: IndexerIndeterminateTypeError) =
        SpecifyParameterTypeFix(error.IndexerExpr)

    override this.IsApplicable(mfv: FSharpMemberOrFunctionOrValue) =
        not mfv.IsModuleValueOrMember

    override this.IsApplicable(de: IDeclaredElement) =
        let pat =
            let refPat = de.As<ILocalReferencePat>().IgnoreParentParens()
            match TuplePatNavigator.GetByPattern(refPat).IgnoreParentParens() with
            | null -> refPat
            | tuplePat -> tuplePat

        isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(pat))

    override this.IsApplicable(decl: IDeclaration) =
        decl :? ILocalReferencePat

    override this.SpecifyType(decl, mfv, d) =
        let decl = decl :?> ILocalReferencePat
        SpecifyTypes.specifyPatternType d mfv.FullType decl


type SpecifyPropertyTypeFix(qualifiedExpr: IQualifiedExpr) =
    inherit SpecifyTypeFixBase(qualifiedExpr)

    new (error: IndeterminateTypeError) =
        SpecifyPropertyTypeFix(error.RefExpr)

    new (error: IndexerIndeterminateTypeError) =
        SpecifyPropertyTypeFix(error.IndexerExpr)

    override this.IsApplicable(_: FSharpMemberOrFunctionOrValue) = true

    override this.IsApplicable(declaredElement: IDeclaredElement) =
        let fsProperty = declaredElement.As<IFSharpProperty>()
        isNotNull fsProperty && fsProperty.Getter :? ImplicitAccessor && not fsProperty.HasExplicitAccessors

    override this.IsApplicable(decl: IDeclaration) =
        match decl with
        | :? IMemberDeclaration as decl ->
            isNull decl.ReturnTypeInfo &&
            Seq.isEmpty decl.AccessorDeclarationsEnumerable
        | _ -> false

    override this.SpecifyType(decl, mfv, displayContext) =
        let memberDecl = decl :?> IMemberDeclaration
        SpecifyTypes.specifyPropertyType displayContext mfv.FullType memberDecl
