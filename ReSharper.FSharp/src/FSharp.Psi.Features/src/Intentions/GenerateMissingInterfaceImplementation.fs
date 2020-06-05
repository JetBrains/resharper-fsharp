namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Application.Settings

[<ContextAction(Name = "GenerateInterfaceImplementation", Group = "F#", Description = "Generates skeleton interface implementation")>]
type GenerateMissingInterfaceImplementation(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let mutable nextUnnamedVariableNumber = 0
    let getUnnamedVariableName() =
        let name = sprintf "var%d" nextUnnamedVariableNumber
        nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
        name
    
    override x.Text = "Generate interface implementation"

    override x.IsAvailable _ =
        let declaration = dataProvider.GetSelectedElement<IInterfaceImplementation>()
        if isNull declaration then false else
        let typeName = declaration.TypeName
        if isNull typeName then false else
        let reference = typeName.Reference
        if isNull reference then false else
        let symbol = reference.GetFSharpSymbol()
        let entity =
            match symbol with
            | :? FSharpEntity as entity -> Some entity
            | _ -> None
            |> Option.defaultWith(fun _ -> failwith "ExpectedFSharpEntity")
        
        let isNotImplemented = declaration.TypeMembersEnumerable |> Seq.map (fun x -> x.SourceName) |> Seq.isEmpty
        entity.IsInterface && isNotImplemented

    override x.ExecutePsiTransaction(_, _) =
        let initialInterfaceImpl = dataProvider.GetSelectedElement<IInterfaceImplementation>()
        let factory = initialInterfaceImpl.CreateElementFactory()
        use _writeCookie = WriteLockCookie.Create(initialInterfaceImpl.IsPhysical())
        use _disableFormatter = new DisableCodeFormatter()
        let settingsStore = initialInterfaceImpl.FSharpFile.GetSettingsStore()
        let spaceAfterComma = settingsStore.GetValue(fun (key : FSharpFormatSettingsKey) -> key.SpaceAfterComma)
        
        let symbol = initialInterfaceImpl.TypeName.Reference.GetFSharpSymbol()
        
        let entity =
            match symbol with
            | :? FSharpEntity as entity -> Some entity
            | _ -> None
            |> Option.defaultWith(fun _ -> failwith "ExpectedFSharpEntity")
            
        let memberDeclarations =
            entity.MembersFunctionsAndValues
            |> Seq.map(fun x ->
                let argNames =
                    x.CurriedParameterGroups
                    |> Seq.map (Seq.map (fun x -> x.Name |> Option.defaultWith(fun _ -> getUnnamedVariableName())) >> Seq.toList)
                    |> Seq.toList
                let typeParams = x.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList
                let memberName = x.DisplayName
                
                let paramDeclarationGroups = factory.CreateMemberParamDeclarations(argNames, spaceAfterComma)
                factory.CreateMemberBindingExpr(memberName, typeParams, paramDeclarationGroups)
                )
            |> Seq.toList
            
        let newInterfaceImpl = factory.CreateInterfaceImplementation(initialInterfaceImpl.TypeName, memberDeclarations, initialInterfaceImpl.Indent)
        ModificationUtil.ReplaceChild(initialInterfaceImpl, newInterfaceImpl) |> ignore
            
        null
