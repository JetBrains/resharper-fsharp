namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open type JetBrains.ReSharper.Plugins.FSharp.Psi.FSharpTreeNodeExtensions
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil.PsiModificationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree

type UpdateCompiledNameInSignatureFix(error: ValueNotContainedMutabilityCompiledNamesDifferError) =
    inherit FSharpQuickFixBase()

    let mutable bindingImplementation = null
    let mutable bindingSignature = null
    let mutable signatureCompiledNameAttribute = None
    let mutable implementationCompiledNamedAttribute = None
    
    let tryFindCompiledNameAttribute (binding: IBindingLikeDeclaration) =
        binding.Attributes
        |> Seq.tryFind (FSharpAttributesUtil.resolvesToType FSharpPredefinedType.compiledNameAttrTypeName)

    override this.Text = $"Update CompiledName for {error.Pat.Identifier.Name} in signature"
    
    override x.IsAvailable _ =
        match tryFindBindingPairFromTopReferencePat error.Pat with
        | None -> false
        | Some (BindingPair(implementation, implMember, signature, sigMember)) ->
            bindingImplementation <- implementation
            bindingSignature <- signature
            signatureCompiledNameAttribute <-tryFindCompiledNameAttribute signature
            implementationCompiledNamedAttribute <- tryFindCompiledNameAttribute implementation
            implMember.Mfv.CompiledName <> sigMember.Mfv.CompiledName
            && (implementationCompiledNamedAttribute.IsSome || signatureCompiledNameAttribute.IsSome)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.Pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match implementationCompiledNamedAttribute, signatureCompiledNameAttribute with
        | None, None -> () // This error will not be raised unless one side has a CompiledNameAttribute.
        | Some value, None ->
            if bindingSignature.Attributes.IsEmpty then
                // We create an elementFactory with the implementation file because the CreateEmptyAttributeList is tied to implementation files only.
                let elementFactory = bindingImplementation.CreateElementFactory()
                let attributeList = elementFactory.CreateEmptyAttributeList()
                FSharpAttributesUtil.addAttribute attributeList value |> ignore
                addNodesBefore bindingSignature [
                    attributeList
                    NewLine(bindingSignature.GetLineEnding())
                ] |> ignore
            else
                FSharpAttributesUtil.addAttributeAfter (Seq.last bindingSignature.Attributes) value
        | None, Some sigAttr ->
            let parentAttributeList = AttributeListNavigator.GetByAttribute(sigAttr)
            if parentAttributeList.Attributes.Count = 1 then
                deleteChild parentAttributeList
            else
                deleteChild sigAttr
        | Some implAttr, Some sigAttr ->
            sigAttr.SetArgExpression(implAttr.ArgExpression)
            |> ignore
