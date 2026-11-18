[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate.GenerateOverrides

open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.ObjExprUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

let getMembersNeedingTypeAnnotations (mfvInstances: FcsMfvInstance list) =
    let sameParamNumberMembersGroups =
        mfvInstances
        |> List.map (fun mfvInstance -> mfvInstance.Mfv)
        |> List.groupBy (fun mfv ->
            mfv.LogicalName, Seq.map Seq.length mfv.CurriedParameterGroups |> Seq.toList)

    let sameParamNumberMembers =
        List.map snd sameParamNumberMembersGroups

    sameParamNumberMembers
    |> Seq.filter (Seq.length >> ((<) 1))
    |> Seq.concat
    |> HashSet

let generateMember (context: ITreeNode) (mayHaveBaseCalls: bool) (element: FSharpGeneratorElement) =
    let mfv = element.Mfv
    Assertion.Assert(not (mfv.IsNonCliEventProperty()))

    let mutable nextUnnamedVariableNumber = 0
    let getUnnamedVariableName () =
        let name = sprintf "var%d" nextUnnamedVariableNumber
        nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
        name

    let isPropertyGetterMethod = mfv.IsPropertyGetterMethod
    let isPropertySetterMethod = mfv.IsPropertySetterMethod
    let isPropertyAccessor = isPropertyGetterMethod || isPropertySetterMethod

    let paramGroups = mfv.CurriedParameterGroups

    let paramNameTypeGroups =
        let getParamType (param: FSharpParameter) =
            param.Type.Instantiate(element.Substitution)

        let getParamNameAndType defaultName (param: FSharpParameter) =
            let name = param.Name |> Option.defaultWith defaultName |> FSharpNamingService.mangleNameIfNecessary
            let paramType = getParamType param
            name, paramType

        let getValueParamNameAndType (param: FSharpParameter) =
            getParamNameAndType (fun _ -> "value") param

        if isPropertySetterMethod && paramGroups.Count = 1 && paramGroups[0].Count > 0 then
            let paramGroup = paramGroups[0]
            if paramGroup.Count = 1 then
                [ [ getValueParamNameAndType paramGroup[0] ] ]
            else
                let accessorParams, valueParam =
                    paramGroup
                    |> List.ofSeq
                    |> List.splitAt (paramGroup.Count - 1)

                [ accessorParams |> List.map (getParamNameAndType getUnnamedVariableName)
                  valueParam |> List.map getValueParamNameAndType ]
        else
            paramGroups
            |> Seq.map (Seq.map (getParamNameAndType getUnnamedVariableName) >> Seq.toList)
            |> Seq.toList

    let typeParams =
        if not element.AddTypes then [] else
        mfv.GenericParameters |> Seq.map _.Name |> Seq.toList

    let memberName =
        if isPropertyAccessor then
            mfv.LogicalName.Substring(4)
        else
            mfv.LogicalName

    let factory = context.CreateElementFactory()
    let settingsStore = context.GetSettingsStoreWithEditorConfig()
    let spaceAfterComma = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAfterComma)

    let generateParameters =
        if isPropertySetterMethod then true else
        if isPropertyGetterMethod && paramGroups.Count = 1 && paramGroups[0].Count = 0 then false else

        not (mfv.IsCliEvent())

    let paramGroups =
        if not generateParameters then [] else
        factory.CreateMemberParamDeclarations(paramNameTypeGroups, spaceAfterComma)

    let isStatic = not mfv.IsInstanceMember

    let memberDeclaration =
        if isPropertyAccessor && generateParameters then
            let accessorName = if isPropertyGetterMethod then "get" else "set"
            factory.CreatePropertyWithAccessor(isStatic, memberName, accessorName, paramGroups)
        else
            factory.CreateMemberDecl(isStatic, memberName, typeParams, paramGroups)

    let shouldCallBase (element: FSharpGeneratorElement) =
        let fsGeneratorElement = element.As<FSharpGeneratorElement>()
        isNotNull fsGeneratorElement &&

        let overridableMember = fsGeneratorElement.DeclaredElement.As<IOverridableMember>()
        not overridableMember.IsAbstract &&

        (not (overridableMember :? IAccessor) || not generateParameters)

    if mayHaveBaseCalls && shouldCallBase element then
        let paramNameTypeGroups =
            paramNameTypeGroups
            |> List.map (List.map (fun (name, fcsType) -> if isRef fcsType then $"&{name}" else name))

        let args =
            if paramNameTypeGroups.IsEmpty || not generateParameters then "" else

            let groupCount = paramNameTypeGroups.Length

            paramNameTypeGroups
            |> List.mapi (fun i paramNames ->
                match paramNames, i with
                | [head], 0 when groupCount > 1 -> $" {head}"
                | [head], _ when groupCount > 1 -> head
                | _ ->
                    let names = paramNames |> String.concat ", "
                    $"({names})"
            )
            |> String.concat " "

        let expr = factory.CreateExpr($"base.{memberName}{args}")
        ModificationUtil.ReplaceChild(memberDeclaration.Expression, expr) |> ignore

    if element.IsOverride then
        memberDeclaration.SetOverride(true)

    if element.Mfv.IsCliEvent() then
        let attribute = context.CreateElementFactory().CreateAttribute("CLIEvent")
        FSharpAttributesUtil.addOuterAttributeList memberDeclaration
        FSharpAttributesUtil.addAttribute memberDeclaration.AttributeLists[0] attribute |> ignore

    let types =
        if not element.AddTypes then None else

        let returnType = mfv.ReturnParameter.Type.Instantiate(element.Substitution)
        Some(paramNameTypeGroups, returnType)

    memberDeclaration, types

let noEmptyLineAnchors =
    NodeTypeSet(
        FSharpTokenType.STRUCT,
        FSharpTokenType.CLASS,
        FSharpTokenType.WITH,
        FSharpTokenType.EQUALS,
        FSharpTokenType.LINE_COMMENT)

let getThisOrPreviousMeaningfulSibling (node: ITreeNode) =
    if isNotNull node && node.IsFiltered() then node.GetPreviousMeaningfulSibling() else node

let getGeneratedBodyToSelect (addedMembers: IMemberDeclaration seq) =
    match Seq.tryHead addedMembers with
    | None -> null
    | Some memberDecl ->

    match Seq.tryHead memberDecl.AccessorDeclarationsEnumerable with
    | Some accessorDecl -> accessorDecl.Expression
    | _ -> memberDecl.Expression

let private getObjectTypeReprAnchor (objectTypeRepr: IObjectModelTypeRepresentation) (psiView: IPsiView) =
    let node = psiView.GetSelectedTreeNode()
    if node.GetTreeStartOffset().Offset < objectTypeRepr.GetTreeStartOffset().Offset then null else

    let prevSibling = getThisOrPreviousMeaningfulSibling node
    if isNotNull prevSibling && prevSibling != objectTypeRepr.EndKeyword then prevSibling else null

let canInsertBefore (node: ITreeNode) =
    match node with
    | null -> true
    | :? ILetBindingsDeclaration
    | :? IDoStatement
    | :? IValFieldDeclaration
    | :? ITypeInherit -> false
    | _ -> true

let canInsertAtNode (node: ITreeNode) =
    isNotNull node && isAtEmptyLine node &&
    canInsertBefore (node.GetNextMeaningfulSibling())

let inline private flip(f: 'a -> 'b -> 'c) (x: 'b) (y: 'a) = f y x

let getAnchorNodeInInterfaceImpl (psiView: IPsiView) (next: IPsiView -> ITreeNode)=
    let selectedTreeNode = psiView.GetSelectedTreeNode()

    let interfaceImpl =
          selectedTreeNode.GetPreviousMeaningfulSibling().As<IInterfaceImplementation>()
    if isNotNull interfaceImpl && not (interfaceImpl.Contains(selectedTreeNode)) then
        let psiView = PsiFileView(interfaceImpl.GetContainingFile(), interfaceImpl.GetLastTokenIn().GetTreeTextRange())
        next psiView else null

let rec getAnchorNodeInObjExpr (psiView: IPsiView) (objExpr: IObjExpr): ITreeNode =
    let selectedTreeNode = psiView.GetSelectedTreeNode()
    let withKeyword = objExpr.WithKeyword

    if isNull withKeyword then null else

    if selectedTreeNode.GetTreeEndOffset().Offset <= withKeyword.GetTreeEndOffset().Offset then
        withKeyword else

    let memberDecl = psiView.GetSelectedTreeNode<ITypeBodyMemberDeclaration>()
    if isNotNull memberDecl && objExpr.Contains(memberDecl) then
        memberDecl else

    let interfaceImplAnchor = getAnchorNodeInInterfaceImpl psiView (flip(getAnchorNodeInObjExpr) objExpr)
    if isNotNull interfaceImplAnchor then interfaceImplAnchor else

    let prevSibling = selectedTreeNode.GetPreviousMeaningfulSibling()
    if isNotNull prevSibling then
        let psiView = PsiFileView(prevSibling.GetContainingFile(), prevSibling.GetTreeTextRange())
        getAnchorNodeInObjExpr psiView objExpr
    else null

let rec getAnchorNode (psiView: IPsiView) (typeDecl: IFSharpTypeElementDeclaration): ITreeNode =
    let selectedTreeNode = psiView.GetSelectedTreeNode()

    let objExpr = typeDecl.As<IObjExpr>()
    if isNotNull objExpr && objExpr.Contains(selectedTreeNode) then
        getAnchorNodeInObjExpr psiView objExpr else

    if isNotNull typeDecl && not (typeDecl.Contains(selectedTreeNode)) &&
            selectedTreeNode.GetTreeStartOffset().Offset >= typeDecl.GetTreeEndOffset().Offset then
        let psiView = PsiFileView(typeDecl.GetContainingFile(), typeDecl.GetLastTokenIn().GetTreeTextRange())
        getAnchorNode psiView typeDecl else

    let memberDecl = psiView.GetSelectedTreeNode<ITypeBodyMemberDeclaration>()
    if isNotNull memberDecl && canInsertBefore (memberDecl.GetNextMeaningfulSibling()) then memberDecl else

    let interfaceImplAnchor = getAnchorNodeInInterfaceImpl psiView (flip(getAnchorNode) typeDecl)
    if isNotNull interfaceImplAnchor then interfaceImplAnchor else

    if canInsertAtNode selectedTreeNode then
        if isNull typeDecl || typeDecl.Contains(selectedTreeNode) then selectedTreeNode else
        typeDecl.FindLastTokenIn().GetPreviousMeaningfulToken(true)
    else
        let objectTypeRepr = psiView.GetSelectedTreeNode<IObjectModelTypeRepresentation>()
        if isNotNull objectTypeRepr then
            getObjectTypeReprAnchor objectTypeRepr psiView else

        let typeRepresentation = psiView.GetSelectedTreeNode<ITypeRepresentation>()
        if isNotNull typeRepresentation then typeRepresentation else

        selectedTreeNode.LeftSiblings()
        |> Seq.tryPick (fun node ->
            match node with
            | :? ITypeDeclarationGroup as node -> Some (node.TypeDeclarations.Last() :> ITreeNode)
            | :? ITypeBodyMemberDeclaration as node -> Some node
            | :? ITypeRepresentation as node -> Some node
            | _ -> None)
        |> Option.defaultValue null

let canHaveOverrides (typeElement: ITypeElement) =
    // todo: filter out union cases
    match typeElement with
    | :? FSharpClass as fsClass -> not (fsClass.IsAbstract && fsClass.IsSealed)
    | :? IStruct -> true
    | _ -> false // todo: interfaces with default impl

let getTestDescriptor (overridableMember: ITypeMember) =
    GeneratorElementBase.GetTestDescriptor(overridableMember, overridableMember.IdSubstitution)

let getMemberXmlDocId (overridableMember: ITypeMember) =
    XMLDocUtil.GetTypeMemberXmlDocId(overridableMember, overridableMember.ShortName)

let getAccessorOrPropertyXmlDocId (mfv: FSharpMemberOrFunctionOrValue) (prop: IProperty) =
        if mfv.IsPropertyGetterMethod then
            getMemberXmlDocId prop.Getter else

        if mfv.IsPropertySetterMethod then
            getMemberXmlDocId prop.Setter else

        getMemberXmlDocId prop

let getPropertyAccessorXmlDocIds (implementedProp: IProperty) (prop: IProperty) =
    prop.GetAllAccessors()
    |> Seq.choose (fun accessor ->
        match accessor.Kind with
        | AccessorKind.GETTER -> Some(getMemberXmlDocId implementedProp.Getter)
        | AccessorKind.SETTER -> Some(getMemberXmlDocId implementedProp.Setter)
        | _ -> None)

let getMfvXmlDocId (mfvInstance: FcsMfvInstance) (psiModule: IPsiModule) =
    let mfv = mfvInstance.Mfv
    let declaredElement = mfv.GetDeclaredElement(psiModule)
    match declaredElement with
    | :? IProperty as prop -> getAccessorOrPropertyXmlDocId mfv prop
    | :? ITypeMember as typeMember -> getMemberXmlDocId typeMember
    | _ -> mfv.GetXmlDocId()

let getMemberXmlDocIds (typeElement: ITypeElement) =
    if isNull typeElement then Seq.empty else

    typeElement.GetMembers()
    |> Seq.collect (fun typeMember ->
        if typeMember :? IFSharpGeneratedElement then Seq.empty else

        match typeMember with
        | :? IProperty as prop ->
            [ if isNotNull prop.Getter then
                  getMemberXmlDocId prop.Getter
              if isNotNull prop.Setter then
                  getMemberXmlDocId prop.Setter ]
        | _ -> [getMemberXmlDocId typeMember]
    )

let private getOverridableMemberIds typeElement (fcsEntity: FSharpEntity) psiModule baseTypesFirst =
    let typeElement = if isNull typeElement then fcsEntity.GetTypeElement(psiModule) else typeElement

    GenerateUtil.GetOverridableMembersOrder(typeElement, baseTypesFirst)
    |> Seq.mapi (fun declarationIndex memberInstance ->
        memberInstance.Member.XMLDocId, (declarationIndex, memberInstance))

type private MemberMfvInstance =
    { Member: IOverridableMember
      MfvInstance: FcsMfvInstance }

let inline private hasImplGetter (state: PropertyOverrideState) =
    state &&& PropertyOverrideState.Getter <> enum 0

let inline private hasImplSetter (state: PropertyOverrideState) =
    state &&& PropertyOverrideState.Setter <> enum 0

let private getMissingAccessorsOrMember (memberMfvInstance: MemberMfvInstance)
        (implementedAccessors: PropertyOverrideState) =
    let mfvInstance = memberMfvInstance.MfvInstance
    let mfv = mfvInstance.Mfv
    let prop = memberMfvInstance.Member.As<IProperty>()
    if isNull prop || not (mfv.IsNonCliEventProperty()) then [memberMfvInstance] else

    [ if isNotNull prop.Getter && mfv.HasGetterMethod && not (hasImplGetter implementedAccessors) then
          { Member = prop.Getter; MfvInstance = { mfvInstance with Mfv = mfv.GetterMethod } }
      if isNotNull prop.Setter && mfv.HasSetterMethod && not (hasImplSetter implementedAccessors) then
          { Member = prop.Setter; MfvInstance = { mfvInstance with Mfv = mfv.SetterMethod } } ]

let private getFcsEntity (fcsSymbolUse: FSharpSymbolUse) =
    match fcsSymbolUse.Symbol with
    | :? FSharpEntity as fcsEntity -> fcsEntity
    | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsConstructor -> mfv.ApparentEnclosingEntity.Value
    | _ -> Unchecked.defaultof<_>

let private getFcsTypeArgs (fcsEntity: FSharpEntity) (fcsSymbolUse: FSharpSymbolUse) =
    let fcsEntityTypeArgs = fcsEntity.GenericArguments
    if fcsEntityTypeArgs.Count <> 0 then
        (fcsEntity.GenericParameters, fcsEntityTypeArgs) ||> Seq.zip
    else
        fcsSymbolUse.GenericArguments
    |> List.ofSeq

let rec private getBaseTypes includeThis (fcsEntity: FSharpEntity) (fcsSymbolUse: FSharpSymbolUse) =
    let rec loop acc (fcsType: FSharpType) =
        let fcsEntityInstance = FcsEntityInstance.create fcsType
        let acc = if isNotNull fcsEntityInstance then fcsEntityInstance :: acc else acc

        match fcsType.BaseType with
        | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
        | _ -> List.rev acc

    let acc =
        if not includeThis then [] else

        let fcsTypeArgs = getFcsTypeArgs fcsEntity fcsSymbolUse
        let t = fcsEntity.AsType().Instantiate(fcsTypeArgs)
        [FcsEntityInstance.create t]

    match fcsEntity.BaseType with
    | Some baseType when baseType.HasTypeDefinition -> loop acc baseType
    | _ -> acc

let getOverridableMembersForType (typeElement: ITypeElement) (fcsSymbolUse: FSharpSymbolUse) missingMembersOnly isObjExpr (psiModule: IPsiModule) =
    let displayContext = fcsSymbolUse.DisplayContext
    let fcsEntity = getFcsEntity fcsSymbolUse

    let ownMembersXmlDocIds = getMemberXmlDocIds typeElement |> HashSet
    let memberInstances = getOverridableMemberIds typeElement fcsEntity psiModule missingMembersOnly |> dict

    let baseFcsTypes = getBaseTypes isObjExpr fcsEntity fcsSymbolUse

    let baseFcsMembers =
        baseFcsTypes |> List.map (fun fcsEntityInstance ->
            let mfvInstances =
                fcsEntityInstance.Entity.MembersFunctionsAndValues
                |> Seq.map (fun mfv -> FcsMfvInstance.create mfv displayContext fcsEntityInstance.Substitution)
                |> Seq.toList
            fcsEntityInstance, mfvInstances
        )

    let alreadyOverriden = Dictionary<OverridableMemberInstance, PropertyOverrideState>()

    let addOverrides (memberInstance: OverridableMemberInstance) =
        for overridableMemberInstance in OverridableMemberImpl.GetImmediateOverride(memberInstance) do
            let state =
                match memberInstance.Member with
                | :? IProperty as prop ->
                    (if prop.IsReadable then PropertyOverrideState.Getter else PropertyOverrideState.None) |||
                    (if prop.IsWritable then PropertyOverrideState.Setter else PropertyOverrideState.None)
                | _ -> PropertyOverrideState.None

            let state =
                match alreadyOverriden.TryGetValue(overridableMemberInstance) with
                | true, existingState -> existingState ||| state
                | _ -> state

            alreadyOverriden[overridableMemberInstance] <- state

    let isOverridden (memberInstance: OverridableMemberInstance) =
        match alreadyOverriden.TryGetValue(memberInstance) with
        | false, _ -> false
        | true, state ->

        let prop = memberInstance.Member.As<IProperty>()
        isNull prop ||

        (not prop.IsReadable || hasImplGetter state) &&
        (not prop.IsWritable || hasImplSetter state)

    for KeyValue(_, (_, memberInstance)) in memberInstances do
        let fsTypeMember = memberInstance.Member.As<IFSharpTypeMember>()
        if isNull fsTypeMember || fsTypeMember.IsVisibleFromFSharp then
            addOverrides memberInstance

    let allOverridableMemberInstances =
        baseFcsMembers |> List.collect (fun (_, mfvInstances) ->
            mfvInstances |> List.choose (fun mfvInstance ->
                if mfvInstance.Mfv.IsAccessor() then None else

                let xmlDocId = getMfvXmlDocId mfvInstance psiModule
                match memberInstances.TryGetValue(xmlDocId) with
                | false, _ -> None
                | true, (declarationIndex, memberInstance) ->

                let isAvailable =
                    not (ownMembersXmlDocIds.Contains(xmlDocId)) &&
                    not (isOverridden memberInstance)

                addOverrides memberInstance
                Some(declarationIndex, { Member = memberInstance.Member; MfvInstance = mfvInstance }, isAvailable)
            )
        )

    let overridableMemberInstances =
        allOverridableMemberInstances
        |> List.choose (fun (declarationIndex, memberMfvInstance, isAvailable) ->
            if isAvailable then Some(declarationIndex, memberMfvInstance) else None)

    let needsTypesAnnotations =
        allOverridableMemberInstances
        |> List.distinctBy (fun (_, memberMfvInstance, _) -> getTestDescriptor memberMfvInstance.Member)
        |> List.map (fun (_, memberMfvInstance, _) -> memberMfvInstance.MfvInstance)
        |> getMembersNeedingTypeAnnotations

    overridableMemberInstances
    |> Seq.filter (fun (_, { Member = m }) -> (m :? IMethod || m :? IProperty || m :? IEvent) && m.CanBeOverridden())
    |> Seq.collect (fun (declarationIndex, memberMfvInstance) ->
        let implementedAccessors =
            match alreadyOverriden.TryGetValue(OverridableMemberInstance(memberMfvInstance.Member)) with
            | true, state -> state
            | _ -> PropertyOverrideState.None

        getMissingAccessorsOrMember memberMfvInstance implementedAccessors
        |> List.map (fun { Member = m; MfvInstance = mfvInstance } ->
            let addTypes = needsTypesAnnotations.Contains(mfvInstance.Mfv)
            declarationIndex, FSharpGeneratorElement(m, mfvInstance, addTypes, interfaceImpl = null)))
    |> Seq.filter (fun (_, i) -> not (ownMembersXmlDocIds.Contains(i.TestDescriptor)))
    |> Seq.distinctBy (fun (_, i) -> i.TestDescriptor) // todo: better way to check shadowing/overriding members
    |> Seq.filter (fun (_, i) -> not missingMembersOnly || i.Member.IsAbstract)
    |> Seq.sortBy fst
    |> Seq.map snd

let getInterfaceMembers (implContext: InterfaceImplementationContext) (typeElement: ITypeElement) =
    let psiModule = typeElement.Module
    let fcsEntity = implContext.FcsEntity
    let interfaceImpl = InterfaceImplementationNavigator.GetByTypeName(implContext.TypeName)
    let inst = Seq.zip fcsEntity.GenericParameters fcsEntity.GenericArguments |> List.ofSeq
    let interfaceType = fcsEntity.AsType().Instantiate(inst)

    let getInterfaces (fcsType: FSharpType) =
        fcsType.AllInterfaces
        |> Seq.filter _.HasTypeDefinition
        |> Seq.map FcsEntityInstance.create
        |> Seq.filter isNotNull
        |> Seq.toList

    let memberInstances =
        getOverridableMemberIds typeElement fcsEntity psiModule true
        |> Seq.collect (fun (_, (declarationIndex, memberInstance) as indexedMemberId) ->
            let prop = memberInstance.Member.As<IProperty>()
            if isNull prop then [ indexedMemberId ] else

            [ for accessor in [ prop.Getter; prop.Setter ] do
                  if isNotNull accessor then
                      let accessorInstance = OverridableMemberInstance(accessor, memberInstance.Substitution)
                      getMemberXmlDocId accessor, (declarationIndex, accessorInstance) ])
        |> Seq.groupBy fst
        |> Seq.map (fun (xmlDocId, memberIds) -> xmlDocId, memberIds |> Seq.map snd |> Seq.toList)
        |> dict

    let isSameType (declaringType: IType) (otherDeclaringType: IType) =
        TypeEqualityComparer.Default.Equals(declaringType, otherDeclaringType)

    let tryFindMemberInstance xmlDocId declaringType =
        match memberInstances.TryGetValue(xmlDocId) with
        | false, _ -> None
        | true, instances ->

        instances
        |> List.tryFind (fun (_, memberInstance) -> isSameType memberInstance.DeclaringType declaringType)
        |> Option.orElse (List.tryHead instances)

    let existingMemberDecls =
        implContext.TypeMembers
        |> Seq.filter (fun memberDecl ->
            let declaredElement = memberDecl.DeclaredElement :?> IOverridableMember
            isNotNull declaredElement)

    let baseClassType =
        match typeElement with
        | :? IClass as classTypeElement -> classTypeElement.GetBaseClassType()
        | _ -> null

    let baseTypeMembers =
        let baseTypeElement = if isNull baseClassType then null else baseClassType.GetTypeElement()
        if isNull baseTypeElement then Seq.empty else

        let baseTypeSubstitution = baseClassType.GetSubstitution()
        TypeElementUtil.GetAllMembers(baseTypeElement)
        |> Seq.choose (fun memberInstance ->
            let overridableMember = memberInstance.Member.As<IOverridableMember>()
            if isNull overridableMember then None else

            let substitution = memberInstance.Substitution.Apply(baseTypeSubstitution)
            Some(OverridableMemberInstance(overridableMember, substitution)))

    let allInterfaceMembers =
        let displayContext = implContext.TypeName.Reference.GetSymbolUse().DisplayContext
        getInterfaces interfaceType |> List.collect (fun fcsEntityInstance ->
            let declaringType = fcsEntityInstance.FcsType.MapType(typeElement.TypeParameters, psiModule)
            fcsEntityInstance.Entity.MembersFunctionsAndValues
            |> Seq.map (fun mfv ->
                declaringType, FcsMfvInstance.create mfv displayContext fcsEntityInstance.Substitution)
            |> Seq.toList)

    let needsTypesAnnotations =
        getMembersNeedingTypeAnnotations (List.map snd allInterfaceMembers)

    let needsTypesAnnotations mfvInstance =
        needsTypesAnnotations.Contains(mfvInstance.Mfv)

    let implementedMembers =
        let explicitlyImplemented =
            existingMemberDecls
            |> Seq.collect (fun memberDecl ->
                let declaredElement = memberDecl.DeclaredElement :?> IOverridableMember
                declaredElement.ExplicitImplementations
                |> Seq.choose (fun explicitImpl ->
                    match explicitImpl.Resolve() with
                    | null -> None
                    | memberInstance ->

                    let xmlDocIds =
                        match memberInstance.Member, declaredElement with
                        | null, _ -> Seq.empty
                        | :? IProperty as implementedProp,
                          (:? IProperty as prop) when memberDecl.GetFcsSymbol().IsNonCliEventPropertyOrAccessor() ->
                            getPropertyAccessorXmlDocIds implementedProp prop
                        | implementedMember, _ -> [ implementedMember.XMLDocId ]

                    Some(xmlDocIds, memberInstance.DeclaringType)))

        let implementedInBaseType =
            baseTypeMembers
            |> Seq.collect (fun memberInstance -> OverridableMemberImpl.GetImmediateImplement(memberInstance, false))
            |> Seq.map (fun memberInstance ->
                let xmlDocIds =
                    match memberInstance.Element with
                    | :? IProperty as prop -> getPropertyAccessorXmlDocIds prop prop
                    | element -> [ element.XMLDocId ]

                xmlDocIds, memberInstance.DeclaringType)

        Seq.append explicitlyImplemented implementedInBaseType
        |> Seq.collect (fun (xmlDocIds, declaringType) -> xmlDocIds |> Seq.map (fun xmlDocId -> xmlDocId, declaringType))
        |> List.ofSeq

    let isImplemented xmlDocId declaringType =
        implementedMembers |> List.exists (fun (implementedXmlDocId, implementedDeclaringType) ->
            implementedXmlDocId = xmlDocId && isSameType implementedDeclaringType declaringType)

    let availableInterfaceMembers =
        allInterfaceMembers
        |> List.filter (fun (declaringType, mfvInstance) ->
            let mfv = mfvInstance.Mfv
            (not mfv.IsProperty || mfv.IsCliEvent()) && not (mfv.IsCliEventAccessor()) &&

            let xmlDocId = getMfvXmlDocId mfvInstance psiModule
            not (isImplemented xmlDocId declaringType))

    availableInterfaceMembers
    |> List.choose (fun (declaringType, mfvInstance) ->
        let xmlDocId = getMfvXmlDocId mfvInstance psiModule

        tryFindMemberInstance xmlDocId declaringType
        |> Option.map (fun (declarationIndex, memberInstance) ->
            let addTypes = needsTypesAnnotations mfvInstance
            declarationIndex, FSharpGeneratorElement(memberInstance.Member, mfvInstance, addTypes, interfaceImpl)))
    |> List.sortBy fst
    |> List.map snd

let getMissingInterfaceMembers (implContext: InterfaceImplementationContext) (typeElement: ITypeElement) =
    getInterfaceMembers implContext typeElement
    |> List.filter _.Member.IsAbstract

let getOverridableMembers missingMembersOnly (typeDeclaration: IFSharpTypeElementDeclaration) : FSharpGeneratorElement seq =
    if isNull typeDeclaration then [] else

    let typeElement = typeDeclaration.DeclaredElement
    if not (canHaveOverrides typeElement) then [] else

    let psiModule = typeElement.Module

    let fcsSymbol, fcsSymbolUse =
        match typeDeclaration with
        | :? IObjExpr as objExpr ->
            let reference = objExpr.TypeName.Reference
            reference.GetFcsSymbol(), reference.GetSymbolUse()
        | _ -> typeDeclaration.GetFcsSymbol(), typeDeclaration.GetFcsSymbolUse()

    if isNull fcsSymbol then [] else

    match typeDeclaration, fcsSymbol with
    | (:? IObjExpr as objExpr), (:? FSharpEntity as fcsEntity) when fcsEntity.IsInterface ->
        let implContext = InterfaceImplementationContext.Create(objExpr)
        if missingMembersOnly then getMissingInterfaceMembers implContext typeElement else

        getInterfaceMembers implContext typeElement

    | _ ->

    let isObjExpr = typeDeclaration :? IObjExpr
    getOverridableMembersForType typeElement fcsSymbolUse missingMembersOnly isObjExpr psiModule

let mayHaveBaseCalls (typeDecl: IFSharpTypeElementDeclaration) =
    match typeDecl with
    | :? IFSharpTypeDeclaration as typeDecl -> isNotNull typeDecl.TypeInheritMember
    | :? IObjExpr -> true
    | _ -> false

let generateInterfaceMembers (typeDecl: IFSharpTypeElementDeclaration) (interfaceImpl: IInterfaceImplementation option) =
    let implContext =
        match interfaceImpl, typeDecl with
        | None, (:? IObjExpr as objExpr) ->
            ValueSome (InterfaceImplementationContext.Create(objExpr))
        | Some impl, _ ->
            ValueSome (InterfaceImplementationContext.Create(impl))
        | _ -> ValueNone

    match implContext with
    | ValueNone -> []
    | ValueSome implContext ->
        let typeElement = typeDecl.DeclaredElement
        getInterfaceMembers implContext typeElement

let private annotateParamDecl (paramDecl: IFSharpParameterDeclaration) (_, fcsType) =
    match paramDecl with
    | :? IFSharpPattern as fsPat -> TypeAnnotationUtil.specifyPatternType fcsType fsPat
    | _ -> ()

let bindTypes types (memberDecl: IMemberDeclaration) =
    match types with
    | None -> ()
    | Some(paramTypes, returnType) ->

    match Seq.tryHead memberDecl.AccessorDeclarationsEnumerable with
    | Some accessorDecl ->
        (accessorDecl.ParameterPatterns.GetParameterDeclarations(), paramTypes)
        ||> Seq.iter2 (Seq.iter2 annotateParamDecl)

    | _ ->

    (memberDecl.GetParameterDeclarations(), paramTypes)
    ||> Seq.iter2 (Seq.iter2 annotateParamDecl)

    FSharpTypeUsageUtil.setParametersOwnerReturnTypeNoBind memberDecl returnType
    |> Seq.singleton
    |> TypeAnnotationUtil.bindAnnotations

let addMembers inputElements (typeDecl: IFSharpTypeElementDeclaration) (anchor: ITreeNode) =
    let mayHaveBaseCalls = mayHaveBaseCalls typeDecl

    let addedMembers =
        ((anchor, []), inputElements) ||> Seq.fold (fun (anchor, addedNodes) generatorElement ->
            let memberDecl, types = generateMember typeDecl mayHaveBaseCalls generatorElement
            let addedDecl = ModificationUtil.AddChildAfter(anchor, memberDecl)
            addedDecl.FormatNode(CodeFormatProfile.INDENT)
            bindTypes types addedDecl

            addedDecl, addedDecl :: addedNodes
        )

    addedMembers |> snd |> List.rev

let addInterfaceMembers inputElements (typeDecl: IFSharpTypeElementDeclaration) (impl: IInterfaceImplementation) =
    let anchor: ITreeNode =
        let existingMembers = impl.TypeMembers
        if not existingMembers.IsEmpty then existingMembers.Last() else

        if isNull impl.WithKeyword then
            ModificationUtil.AddChildAfter(impl.TypeName, FSharpTokenType.WITH.CreateLeafElement()) |> ignore

        impl.WithKeyword

    addMembers inputElements typeDecl anchor

let isInterfaceImplementation (impl: IInterfaceImplementation) =
    let fcsEntity = impl.FcsEntity
    isNotNull fcsEntity && fcsEntity.IsInterface

let private getInterfaceImplementations (typeDecl: IFSharpTypeElementDeclaration) : IInterfaceImplementation seq =
    match typeDecl with
    | :? IObjExpr as objExpr -> objExpr.InterfaceImplementationsEnumerable
    | :? IFSharpTypeDeclaration as typeDecl ->
        let reprMembers: ITypeBodyMemberDeclaration seq =
            match typeDecl.TypeRepresentation with
            | :? IObjectModelTypeRepresentation as repr -> repr.TypeMembersEnumerable
            | _ -> Seq.empty

        Seq.append reprMembers typeDecl.TypeMembersEnumerable
        |> Seq.choose (fun typeMember -> typeMember.As<IInterfaceImplementation>() |> Option.ofObj)
    | _ -> Seq.empty

let getOverridableAndMissingInterfaceMembers missingOverridableMembersOnly (typeDecl: IFSharpTypeElementDeclaration)
        : FSharpGeneratorElement seq =
    seq {
        yield! getOverridableMembers missingOverridableMembersOnly typeDecl

        for impl in getInterfaceImplementations typeDecl do
            if isInterfaceImplementation impl then
                yield! getMissingInterfaceMembers (InterfaceImplementationContext.Create(impl)) typeDecl.DeclaredElement
    }

let convertToObjectExpression (factory: IFSharpElementFactory) (psiModule: IPsiModule) (expr: IFSharpExpression) =
    let reference = NewObjPostfixTemplate.getReference expr
    let fcsSymbolUse = reference.GetSymbolUse()

    let objExpr = NewObjPostfixTemplate.createObjExpr factory expr
    let inputElements = getOverridableMembersForType null fcsSymbolUse true true psiModule

    let indent = expr.Indent + expr.GetIndentSize()
    let lineEnding = expr.GetLineEnding()

    let objExpr =
        use disableFormatter = new DisableCodeFormatter()
        ModificationUtil.ReplaceChild(expr, objExpr)

    addMembers inputElements objExpr objExpr.WithKeyword |> ignore

    if Seq.isEmpty objExpr.MemberDeclarationsEnumerable then
        use disableFormatter = new DisableCodeFormatter()

        deleteChildRange objExpr.WithKeyword.NextSibling objExpr.RightBrace.PrevSibling
        addNodesAfter objExpr.WithKeyword [
            NewLine(lineEnding)
            Whitespace(indent + 1)
        ] |> ignore

    objExpr

let selectObjExprMemberOrCallCompletion (objExpr: IObjExpr) : IBulbActionCommand =
    match objExpr.MemberDeclarationsEnumerable |> Seq.tryHead with
    | Some(:? IMemberDeclaration as decl) ->
        BulbActionCommands.SetSelection(decl.Expression)

    | _ ->
        let offset = objExpr.RightBrace.GetDocumentStartOffset() - 1
        BulbActionCommands.ShowCodeCompletionPopup(offset, FSharpLanguage.Instance, AutopopupType.SoftAutopopup)
