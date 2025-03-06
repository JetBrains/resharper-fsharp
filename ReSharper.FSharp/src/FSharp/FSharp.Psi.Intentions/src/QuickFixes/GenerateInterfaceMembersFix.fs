namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

type FSharpGeneratorMfvElement(mfv, displayContext, substitution, addTypes) =
    new (mfvInstance: FcsMfvInstance, addTypes) =
        FSharpGeneratorMfvElement(mfvInstance.Mfv, mfvInstance.DisplayContext, mfvInstance.Substitution, addTypes)

    interface IFSharpGeneratorElement with
        member x.Mfv = mfv
        member x.DisplayContext = displayContext
        member x.Substitution = substitution
        member x.AddTypes = addTypes
        member x.IsOverride = false

type GenerateInterfaceMembersFix(impl: IInterfaceImplementation) =
    inherit FSharpQuickFixBase()

    let getInterfaces (fcsType: FSharpType) =
        fcsType.AllInterfaces
        |> Seq.filter (fun t -> t.HasTypeDefinition)
        |> Seq.map FcsEntityInstance.create
        |> Seq.filter isNotNull
        |> Seq.toList

    new (error: NoImplementationGivenInInterfaceError) =
        GenerateInterfaceMembersFix(error.Impl)

    new (error: NoImplementationGivenInInterfaceWithSuggestionError) =
        GenerateInterfaceMembersFix(error.Impl)

    override x.Text = "Generate missing members"

    override x.IsAvailable _ =
        let fcsEntity = impl.FcsEntity
        isNotNull fcsEntity && fcsEntity.IsInterface

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(impl.IsPhysical())

        let typeDeclaration: IFSharpTypeElementDeclaration =
            match FSharpTypeDeclarationNavigator.GetByTypeMember(impl) with
            | null ->
                match ObjExprNavigator.GetByInterfaceImplementation(impl) with
                | null ->
                    let repr = ObjectModelTypeRepresentationNavigator.GetByTypeMember(impl)
                    FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr)
                | objExpr -> objExpr
            | decl -> decl

        let psiModule = typeDeclaration.GetPsiModule()
        let typeElement = typeDeclaration.DeclaredElement
        let fcsEntity = impl.FcsEntity
        let inst = Seq.zip fcsEntity.GenericParameters fcsEntity.GenericArguments |> List.ofSeq
        let interfaceType = fcsEntity.AsType().Instantiate(inst)
        let existingMemberDecls = impl.TypeMembers

        let getXmlDocId (typeMember: ITypeMember) =
            XMLDocUtil.GetTypeMemberXmlDocId(typeMember, typeMember.ShortName)

        let getAccessorOrPropertyXmlDocId (mfv: FSharpMemberOrFunctionOrValue) (prop: IProperty) =
            if mfv.IsPropertyGetterMethod then
                getXmlDocId prop.Getter else

            if mfv.IsPropertySetterMethod then
                getXmlDocId prop.Setter else

            getXmlDocId prop

        let getPropertyAccessorXmlDocIds (implementedProp: IProperty) (prop: IProperty) =
            prop.GetAllAccessors()
            |> Seq.choose (fun accessor ->
                match accessor.Kind with
                | AccessorKind.GETTER -> Some(getXmlDocId implementedProp.Getter)
                | AccessorKind.SETTER -> Some(getXmlDocId implementedProp.Setter)
                | _ -> None)

        let implementedMembers =
            existingMemberDecls
            |> Seq.collect (fun memberDecl ->
                let declaredElement = memberDecl.DeclaredElement :?> IOverridableMember
                declaredElement.ExplicitImplementations
                |> Seq.collect (fun explicitImpl ->
                    match explicitImpl.Resolve() with
                    | null -> Seq.empty
                    | memberInstance ->

                    let fcsSymbol = memberDecl.GetFcsSymbol()
                    match memberInstance.Member, declaredElement with
                    | :? IProperty as implementedProp, (:? IProperty as prop) when fcsSymbol.IsNonCliEventPropertyOrAccessor() ->
                        getPropertyAccessorXmlDocIds implementedProp prop
                    | implementedMember, _ -> [implementedMember.XMLDocId]))
            |> HashSet

        let baseTypeElement =
            match typeElement with
            | :? IClass as classTypeElement ->
                let baseClassType = classTypeElement.GetBaseClassType()
                baseClassType.Resolve().DeclaredElement.As<ITypeElement>()
            | _ -> null

        let baseTypeMembers =
            if isNull baseTypeElement then Seq.empty else
            TypeElementUtil.GetAllMembers(baseTypeElement)

        baseTypeMembers
        |> Seq.collect (fun memberInstance ->
            let overridableMember = memberInstance.Member.As<IOverridableMember>()
            if isNull overridableMember then Seq.empty else
            OverridableMemberImpl.GetImmediateImplement(OverridableMemberInstance(overridableMember), false))
        |> Seq.collect (fun memberInstance ->
            match memberInstance.Element with
            | :? IProperty as prop -> getPropertyAccessorXmlDocIds prop prop
            | element -> [element.XMLDocId])
        |> Seq.iter (implementedMembers.Add >> ignore)

        let allInterfaceMembers =
            let displayContext = impl.TypeName.Reference.GetSymbolUse().DisplayContext
            getInterfaces interfaceType |> List.collect (fun fcsEntityInstance ->
                fcsEntityInstance.Entity.MembersFunctionsAndValues
                |> Seq.map (fun mfv -> FcsMfvInstance.create mfv displayContext fcsEntityInstance.Substitution)
                |> Seq.toList)

        let needsTypesAnnotations =
            GenerateOverrides.getMembersNeedingTypeAnnotations allInterfaceMembers

        let needsTypesAnnotations mfvInstance =
            needsTypesAnnotations.Contains(mfvInstance.Mfv)

        let membersToGenerate =
            allInterfaceMembers
            |> List.filter (fun mfvInstance ->
                let mfv = mfvInstance.Mfv
                (not mfv.IsProperty || mfv.IsCliEvent()) && not (mfv.IsCliEventAccessor()) &&

                let declaredElement = mfv.GetDeclaredElement(psiModule)
                let xmlDocId =
                    match declaredElement with
                    | :? IProperty as prop -> getAccessorOrPropertyXmlDocId mfv prop
                    | :? ITypeMember as typeMember -> getXmlDocId typeMember
                    | _ -> mfv.GetXmlDocId()

                not (implementedMembers.Contains(xmlDocId)))
            |> List.sortBy (fun mfvInstance -> mfvInstance.Mfv.DisplayNameCore) // todo: try to preserve declaration sorting?
            |> List.map (fun mfvInstance -> FSharpGeneratorMfvElement(mfvInstance, needsTypesAnnotations mfvInstance))

        let (anchor: ITreeNode) =
            let existingMembers = impl.TypeMembers
            if not existingMembers.IsEmpty then
                existingMembers.Last()
            else
                if isNull impl.WithKeyword then
                    ModificationUtil.AddChildAfter(impl.TypeName, FSharpTokenType.WITH.CreateLeafElement()) |> ignore

                impl.WithKeyword

        let addedMembers = GenerateOverrides.addMembers membersToGenerate typeDeclaration anchor

        Action<_>(fun textControl ->
            let treeTextRange = GenerateOverrides.getGeneratedSelectionTreeRange addedMembers
            if treeTextRange.IsValid() then
                let documentRange = anchor.GetContainingFile().GetDocumentRange(treeTextRange)
                textControl.Caret.MoveTo(documentRange.StartOffset, CaretVisualPlacement.DontScrollIfVisible)
                textControl.Selection.SetRange(documentRange))
