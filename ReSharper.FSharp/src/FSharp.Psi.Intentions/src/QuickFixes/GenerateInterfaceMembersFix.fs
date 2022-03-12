﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
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
        use disableFormatter = new DisableCodeFormatter()

        let typeDeclaration =
            match FSharpTypeDeclarationNavigator.GetByTypeMember(impl) with
            | null ->
                let repr = ObjectModelTypeRepresentationNavigator.GetByTypeMember(impl)
                FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr)
            | decl -> decl

        let typeElement = typeDeclaration.DeclaredElement
        let fcsEntity = typeDeclaration.GetFcsSymbol() :?> FSharpEntity

        let interfaceType = 
            fcsEntity.DeclaredInterfaces |> Seq.find (fun e ->
                e.HasTypeDefinition && e.TypeDefinition.IsEffectivelySameAs(impl.FcsEntity))

        let displayContext = impl.TypeName.Reference.GetSymbolUse().DisplayContext

        let existingMemberDecls = impl.TypeMembers

        let implementedMembers =
            existingMemberDecls
            |> Seq.collect (fun memberDecl ->
                memberDecl.DeclaredElement.As<IOverridableMember>().ExplicitImplementations
                |> Seq.choose (fun i -> i.Resolve() |> Option.ofObj |> Option.map (fun i -> i.Element.XMLDocId)))
            |> HashSet

        TypeElementUtil.GetAllMembers(typeElement)
        |> Seq.collect (fun m ->
            let overridableMember = m.Member.As<IOverridableMember>()
            if isNull overridableMember then Seq.empty else
            OverridableMemberImpl.GetImmediateImplement(OverridableMemberInstance(overridableMember), false))
        |> Seq.map (fun memberInstance -> memberInstance.Element.XMLDocId)
        |> Seq.iter (implementedMembers.Add >> ignore)

        let allInterfaceMembers =
            getInterfaces interfaceType |> List.collect (fun fcsEntityInstance ->
                fcsEntityInstance.Entity.MembersFunctionsAndValues
                |> Seq.map (fun mfv -> FcsMfvInstance.create mfv displayContext fcsEntityInstance.Substitution)
                |> Seq.toList)

        let needsTypesAnnotations =
            GenerateOverrides.getMembersNeedingTypeAnnotations allInterfaceMembers

        let membersToGenerate =
            allInterfaceMembers
            |> List.filter (fun mfvInstance ->
                not (mfvInstance.Mfv.IsAccessor()) &&

                let xmlDocId = mfvInstance.Mfv.GetXmlDocId()
                not (implementedMembers.Contains(xmlDocId)))
            |> List.sortBy (fun mfvInstance -> mfvInstance.Mfv.LogicalName) // todo: better sorting?
            |> List.map (fun mfvInstance -> mfvInstance, needsTypesAnnotations.Contains(mfvInstance.Mfv))
            |> List.map FSharpGeneratorMfvElement

        let indent =
            if existingMemberDecls.IsEmpty then
                impl.Indent + impl.GetIndentSize()
            else
                existingMemberDecls.Last().Indent

        let generatedMembers =
            membersToGenerate
            |> List.map (GenerateOverrides.generateMember impl indent)
            |> List.collect (withNewLineAndIndentBefore indent)

        let existingMembers = impl.TypeMembers
        let anchor, lastNode = 
            if not existingMembers.IsEmpty then
                let lastMember = existingMembers.Last()
                let anchor = GenerateOverrides.addEmptyLineBeforeIfNeeded lastMember
                anchor, addNodesAfter anchor generatedMembers
            else
                if isNull impl.WithKeyword then
                    addNodesAfter impl.TypeName [
                        Whitespace()
                        FSharpTokenType.WITH.CreateLeafElement()
                    ] |> ignore

                impl.WithKeyword, addNodesAfter impl.WithKeyword generatedMembers

        Action<_>(fun textControl ->
            let treeTextRange = GenerateOverrides.getGeneratedSelectionTreeRange lastNode (anchor.RightSiblings())
            if treeTextRange.IsValid() then
                let documentRange = anchor.GetContainingFile().GetDocumentRange(treeTextRange)
                textControl.Caret.MoveTo(documentRange.StartOffset, CaretVisualPlacement.DontScrollIfVisible)
                textControl.Selection.SetRange(documentRange))
