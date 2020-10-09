namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type GenerateInterfaceMembersFix(error: NoImplementationGivenInterfaceError) =
    inherit FSharpQuickFixBase()

    let impl = error.Impl

    let getInterfaces (fcsType: FSharpType) =
        fcsType.AllInterfaces
        |> Seq.filter (fun t -> t.HasTypeDefinition)
        |> Seq.map (fun t ->
            let fcsEntity = t.TypeDefinition
            fcsEntity, Seq.zip fcsEntity.GenericParameters t.GenericArguments |> Seq.toList)

    let mutable nextUnnamedVariableNumber = 0
    let getUnnamedVariableName () =
        let name = sprintf "var%d" nextUnnamedVariableNumber
        nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
        name

    override x.Text = "Generate missing members"

    override x.IsAvailable _ =
        let fcsEntity = impl.FcsEntity
        isNotNull fcsEntity && fcsEntity.IsInterface 

    override x.ExecutePsiTransaction _ =
        let factory = impl.CreateElementFactory()
        use writeCookie = WriteLockCookie.Create(impl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let settingsStore = impl.GetSettingsStoreWithEditorConfig()
        let spaceAfterComma = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAfterComma)

        let interfaceType =
            let typeDeclaration =
                match FSharpTypeDeclarationNavigator.GetByTypeMember(impl) with
                | null ->
                    let repr = ObjectModelTypeRepresentationNavigator.GetByTypeMember(impl)
                    FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr)
                | decl -> decl

            let fcsEntity = typeDeclaration.GetFSharpSymbol() :?> FSharpEntity
            fcsEntity.DeclaredInterfaces |> Seq.find (fun e ->
                e.HasTypeDefinition && e.TypeDefinition.IsEffectivelySameAs(impl.FcsEntity))

        let displayContext = impl.TypeName.Reference.GetSymbolUse().DisplayContext

        let existingMemberDecls = impl.TypeMembers

        let implementedMembers =
            existingMemberDecls
            |> Seq.map (fun m ->
                m.DeclaredElement.As<IOverridableMember>().ExplicitImplementations
                |> Seq.choose (fun i -> i.Resolve() |> Option.ofObj |> Option.map (fun i -> i.Element.XMLDocId)))
            |> Seq.concat
            |> HashSet

        let allInterfaceMembers = 
            getInterfaces interfaceType
            |> Seq.collect (fun (fcsEntity, substitution) ->
                fcsEntity.MembersFunctionsAndValues |> Seq.map (fun mfv -> mfv, substitution))
            |> Seq.toList

        let needsTypesAnnotations = 
            let sameParamNumberMembersGroups = 
                allInterfaceMembers |> Seq.groupBy (fun (mfv, _) ->
                    let parameterGroups = mfv.CurriedParameterGroups
                    mfv.LogicalName, (Seq.length parameterGroups), (Seq.map Seq.length parameterGroups |> Seq.toList))
                |> Seq.toList

            let sameParamNumberMembers =
                List.map (snd >> (Seq.map fst) >> Seq.toList) sameParamNumberMembersGroups

            sameParamNumberMembers
            |> Seq.filter (Seq.length >> ((<) 1))
            |> Seq.concat
            |> HashSet

        let membersToGenerate = 
            allInterfaceMembers
            |> Seq.filter (fun (mfv, _) ->
                // todo: other accessors
                not (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod) &&

                let xmlDocId = FSharpElementsUtil.GetXmlDocId(mfv)
                isNotNull xmlDocId && not (implementedMembers.Contains(xmlDocId)))
            |> Seq.sortBy (fun (mfv, _) -> mfv.LogicalName) // todo: better sorting?
            |> Seq.toList

        let indent =
            if existingMemberDecls.IsEmpty then
                impl.Indent + impl.GetIndentSize()
            else
                existingMemberDecls.Last().Indent

        let lineEnding = impl.GetLineEnding()

        let generatedMembers =
            membersToGenerate
            |> List.collect (fun (mfv, substitution) ->
                let argNames =
                    mfv.CurriedParameterGroups
                    |> Seq.map (Seq.map (fun x ->
                        let name = x.Name |> Option.defaultWith (fun _ -> getUnnamedVariableName ())
                        name, x.Type.Instantiate(substitution)) >> Seq.toList)
                    |> Seq.toList

                let typeParams = mfv.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList
                let memberName = mfv.LogicalName

                let addTypes = needsTypesAnnotations.Contains(mfv)

                let paramGroups =
                    if mfv.IsProperty then [] else
                    factory.CreateMemberParamDeclarations(argNames, spaceAfterComma, addTypes, displayContext)

                let memberDeclaration = factory.CreateMemberBindingExpr(memberName, typeParams, paramGroups)

                if addTypes then
                    let lastParam = memberDeclaration.ParametersPatterns.LastOrDefault()
                    if isNull lastParam then () else

                    let typeString = mfv.ReturnParameter.Type.Instantiate(substitution)
                    let typeUsage = factory.CreateTypeUsage(typeString.Format(displayContext))
                    ModificationUtil.AddChildAfter(lastParam, factory.CreateReturnTypeInfo(typeUsage)) |> ignore

                [ NewLine(lineEnding) :> ITreeNode
                  Whitespace(indent) :> _
                  memberDeclaration :> _ ])

        if isNull impl.WithKeyword then
            addNodesAfter impl.LastChild [
                Whitespace()
                FSharpTokenType.WITH.CreateLeafElement()
            ] |> ignore

        addNodesAfter impl.LastChild generatedMembers |> ignore
