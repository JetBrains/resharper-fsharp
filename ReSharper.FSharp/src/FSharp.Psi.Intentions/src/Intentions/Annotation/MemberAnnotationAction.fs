namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell


[<ContextAction(Name = "AnnotateMember", Group = "F#",
                Description = "Annotate binding with explicit type")>]
type MemberAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let isAvailableForMemberDeclaration(declaration: IMemberDeclaration) =
        let isAvailable =
            isAtOverridableMemberDeclaration dataProvider declaration
            && not (AnnotationUtil.isFullyAnnotatedMemberDeclaration declaration)

        isAvailable

    override this.IsAvailable _ =
        let memberDeclaration = dataProvider.GetSelectedElement<IOverridableMemberDeclaration>()
        match memberDeclaration with
        | :? IMemberDeclaration as declaration ->
            isAvailableForMemberDeclaration declaration

        | :? IAutoPropertyDeclaration as declaration ->
            // TODO: there is a bug : type is not included in declaration range
            false

        | _ ->
            false

    override this.Text = "Add member type annotations"

    override x.ExecutePsiTransaction _ =
        let memberDeclaration = dataProvider.GetSelectedElement<IMemberDeclaration>()

        use writeCookie = WriteLockCookie.Create(memberDeclaration.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match memberDeclaration with
        | Declaration.IsNotNullAndHasMfvSymbolUse (symbolUse, mfv) ->
            let displayContext = symbolUse.DisplayContext
            match memberDeclaration.DeclaredElement with
            | :? IFSharpProperty ->
                SpecifyUtil.specifyPropertyType displayContext mfv.ReturnParameter.Type memberDeclaration
            | _ ->
                let parameters = memberDeclaration.ParameterPatterns
                let types = FcsTypeUtil.getMethodParameterTypes parameters.Count mfv.FullType
                let forceParens = not (parameters[0].Parent :? IParenPat)

                (parameters, types)
                ||> Seq.iter2 (fun parameter fcsType ->
                    if not (parameter :? IUnitPat) then
                        SpecifyUtil.specifyPattern displayContext fcsType forceParens parameter)
                SpecifyUtil.specifyMethodReturnType displayContext mfv memberDeclaration
        | _ ->
            ()
