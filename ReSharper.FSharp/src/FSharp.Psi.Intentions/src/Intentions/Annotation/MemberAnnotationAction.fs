namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "AnnotateMember", Group = "F#",
                Description = "Annotate binding with explicit type")>]
type MemberAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.IsAvailable _ =
        let memberDeclaration = dataProvider.GetSelectedElement<IMemberDeclaration>()
        isNotNull memberDeclaration
         && AnnotationUtil.isFullyAnnotatedMemberDeclaration memberDeclaration

    override this.Text = "Add member type annotations"

    override x.ExecutePsiTransaction _ =
        let memberDeclaration = dataProvider.GetSelectedElement<IMemberDeclaration>()

        use writeCookie = WriteLockCookie.Create(memberDeclaration.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match memberDeclaration with
        | Declaration.HasMfvSymbolUse (symbolUse, mfv) ->
            let displayContext = symbolUse.DisplayContext
            match memberDeclaration.DeclaredElement with
            | :? IFSharpProperty ->
                SpecifyUtil.specifyPropertyType displayContext mfv.ReturnParameter.Type memberDeclaration
            | _ ->
                let parameters = memberDeclaration.ParametersDeclarations
                let types = FcsMfvUtil.getFunctionParameterTypes parameters.Count mfv
                (parameters, types)
                ||> Seq.iter2 (fun parameter fsType ->
                    SpecifyUtil.specifyPattern displayContext fsType false parameter.Pattern)
                SpecifyUtil.specifyMethodReturnType displayContext mfv memberDeclaration
        | _ ->
            ()