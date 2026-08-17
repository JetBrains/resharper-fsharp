namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type GenerateInterfaceMembersFix(impl: IInterfaceImplementation) =
    inherit FSharpQuickFixBase()

    let getTypeDeclByImpl (impl: IInterfaceImplementation) : IFSharpTypeElementDeclaration =
        match FSharpTypeDeclarationNavigator.GetByTypeMember(impl) with
        | null ->
            match ObjExprNavigator.GetByInterfaceImplementation(impl) with
            | null ->
                let repr = ObjectModelTypeRepresentationNavigator.GetByTypeMember(impl)
                FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr)
            | objExpr -> objExpr
        | decl -> decl

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
            getTypeDeclByImpl impl

        let typeElement = typeDeclaration.DeclaredElement

        let membersToGenerate =
            GenerateOverrides.getInterfaceMembers true impl typeElement
            |> GenerateOverrides.sanitizeMembers
            
        let (anchor: ITreeNode) =
            let existingMembers = impl.TypeMembers
            if not existingMembers.IsEmpty then
                existingMembers.Last()
            else
                if isNull impl.WithKeyword then
                    ModificationUtil.AddChildAfter(impl.TypeName, FSharpTokenType.WITH.CreateLeafElement()) |> ignore

                impl.WithKeyword

        let addedMembers = GenerateOverrides.addMembers membersToGenerate typeDeclaration anchor
        let expr = GenerateOverrides.getGeneratedBodyToSelect addedMembers
        BulbActionCommands.SetSelection(expr)
