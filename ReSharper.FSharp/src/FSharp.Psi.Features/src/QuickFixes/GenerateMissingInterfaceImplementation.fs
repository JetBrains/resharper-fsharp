namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type GenerateInterfaceMembersFix(error: NoImplementationGivenInterfaceError) =
    inherit FSharpQuickFixBase()

    let impl = error.Impl

    let rec getInterfaces (fcsEntity: FSharpEntity) =
        fcsEntity.AllInterfaces |> Seq.map (fun interfaceType -> 
            let fcsType = getAbbreviatedType interfaceType
            fcsType.TypeDefinition)

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

        let entity = impl.FcsEntity

        let existingMemberDecls = impl.TypeMembers

        let implementedMembers =
            existingMemberDecls
            |> Seq.map (fun m ->
                m.DeclaredElement.As<IOverridableMember>().ExplicitImplementations
                |> Seq.choose (fun i -> i.Resolve() |> Option.ofObj |> Option.map (fun i -> i.Element.XMLDocId)))
            |> Seq.concat
            |> HashSet

        let membersToGenerate = 
            getInterfaces entity
            |> Seq.collect (fun fcsEntity -> fcsEntity.MembersFunctionsAndValues)
            |> Seq.filter (fun mfv ->
                // todo: other accessors
                not (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod) &&

                let xmlDocId = FSharpElementsUtil.GetXmlDocId(mfv)
                isNotNull xmlDocId && not (implementedMembers.Contains(xmlDocId)))
            |> Seq.sortBy (fun mfv -> mfv.DisplayName) // todo: better sorting?
            |> Seq.toList

        let indent =
            if existingMemberDecls.IsEmpty then
                impl.Indent + impl.GetIndentSize()
            else
                existingMemberDecls.Last().Indent

        let lineEnding = impl.GetLineEnding()

        let generatedMembers =
            membersToGenerate
            |> List.collect (fun mfv ->
                let argNames =
                    mfv.CurriedParameterGroups
                    |> Seq.map (Seq.map (fun x -> x.Name |> Option.defaultWith (fun _ -> getUnnamedVariableName ())) >> Seq.toList)
                    |> Seq.toList

                let typeParams = mfv.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList
                let memberName = mfv.DisplayName

                let paramGroups =
                    if mfv.IsProperty then [] else
                    factory.CreateMemberParamDeclarations(argNames, spaceAfterComma)

                [ NewLine(lineEnding) :> ITreeNode
                  Whitespace(indent) :> _
                  factory.CreateMemberBindingExpr(memberName, typeParams, paramGroups) :> _ ])

        if isNull impl.WithKeyword then
            addNodesAfter impl.LastChild [
                Whitespace()
                FSharpTokenType.WITH.CreateLeafElement()
            ] |> ignore

        addNodesAfter impl.LastChild generatedMembers |> ignore
