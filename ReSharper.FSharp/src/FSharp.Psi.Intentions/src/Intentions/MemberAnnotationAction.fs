namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

[<ContextAction(Name = "AnnotateBinding", Group = "F#",
                Description = "Annotate binding with explicit type")>]
type MemberAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let isAnnotated (memberDeclaration: IMemberDeclaration) =
        isNotNull memberDeclaration.ReturnTypeInfo &&
        memberDeclaration.ParametersDeclarations |> Seq.forall (fun parameter ->
            let pattern = parameter.Pattern.IgnoreInnerParens()
            match pattern with
            | :? ITypedPat as PatUtil.IsPartiallyAnnotatedTypedPat -> false
            | :? ITypedPat | :? IUnitPat -> true
            | _ -> false)

    override this.IsAvailable _ =
        let memberDeclaration = dataProvider.GetSelectedElement<IMemberDeclaration>()
        isNotNull memberDeclaration
         && not (isAnnotated memberDeclaration)

    override this.Text = "Add member type annotations"

    override x.ExecutePsiTransaction _ =
        let memberDeclaration = dataProvider.GetSelectedElement<IMemberDeclaration>()

        use writeCookie = WriteLockCookie.Create(memberDeclaration.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let symbolUse =
            let checker = memberDeclaration.FSharpFile.FcsCheckerService
            checker.ResolveNameAtLocation(memberDeclaration, [| memberDeclaration.DeclaredName |], true, "Get declaration")

        match symbolUse with
        | Some symbolUse when (symbolUse.Symbol :? FSharpMemberOrFunctionOrValue) ->
            let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let displayContext = symbolUse.DisplayContext
            match memberDeclaration.DeclaredElement with
            | :? IFSharpProperty ->
                SpecifyTypes.specifyPropertyType displayContext mfv.ReturnParameter.Type memberDeclaration
            | _ ->
                memberDeclaration.ParametersDeclarations |> Seq.iter SpecifyTypes.specifyParameterDeclaration
                SpecifyTypes.specifyMethodReturnType displayContext mfv memberDeclaration
        | _ ->
            ()