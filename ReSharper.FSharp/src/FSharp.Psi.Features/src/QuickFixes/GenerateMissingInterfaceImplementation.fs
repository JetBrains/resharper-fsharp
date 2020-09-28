namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type GenerateMissingInterfaceMembersFix(error: NoImplementationGivenInterfaceError) =
    inherit FSharpQuickFixBase()

    let impl = error.Impl

    let mutable nextUnnamedVariableNumber = 0
    let getUnnamedVariableName () =
        let name = sprintf "var%d" nextUnnamedVariableNumber
        nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
        name

    override x.Text = "Generate interface implementation"

    override x.IsAvailable _ =
        let fcsEntity = impl.FcsEntity
        if isNull fcsEntity || not fcsEntity.IsInterface then false else

        impl.TypeMembersEnumerable |> Seq.map (fun x -> x.SourceName) |> Seq.isEmpty

    override x.ExecutePsiTransaction(_, _) =
        let factory = impl.CreateElementFactory()
        use writeCookie = WriteLockCookie.Create(impl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let settingsStore = impl.FSharpFile.GetSettingsStoreWithEditorConfig()
        let spaceAfterComma = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAfterComma)

        let entity = impl.FcsEntity

        let memberDeclarations =
            entity.MembersFunctionsAndValues
            |> Seq.map (fun x ->
                let argNames =
                    x.CurriedParameterGroups
                    |> Seq.map (Seq.map (fun x -> x.Name |> Option.defaultWith (fun _ -> getUnnamedVariableName ())) >> Seq.toList)
                    |> Seq.toList
                let typeParams = x.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList
                let memberName = x.DisplayName

                let paramDeclarationGroups = factory.CreateMemberParamDeclarations(argNames, spaceAfterComma)
                factory.CreateMemberBindingExpr(memberName, typeParams, paramDeclarationGroups)
            ) |> Seq.toList

        let newInterfaceImpl = factory.CreateInterfaceImplementation(impl.TypeName, memberDeclarations, impl.Indent)
        ModificationUtil.ReplaceChild(impl, newInterfaceImpl) |> ignore

        null
