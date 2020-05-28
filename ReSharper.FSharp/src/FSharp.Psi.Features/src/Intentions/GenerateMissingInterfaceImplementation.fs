namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open  FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree

[<ContextAction(Name = "GenerateInterfaceImplementation", Group = "F#", Description = "Generates skeleton interface implementation")>]
type GenerateMissingInterfaceImplementation(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    // TODO: This variable number should be per-interface-member
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
        
        let isNotImplemented = declaration.TypeMembersEnumerable |> Seq.map(fun x -> x.SourceName) |> Seq.isEmpty
        entity.IsInterface && isNotImplemented

    override x.ExecutePsiTransaction(_, _) =
        let interfaceImplementation = dataProvider.GetSelectedElement<IInterfaceImplementation>()
        let factory = interfaceImplementation.CreateElementFactory()
        use _writeCookie = WriteLockCookie.Create(interfaceImplementation.IsPhysical())
        use _disableFormatter = new DisableCodeFormatter()
        
        let symbol = interfaceImplementation.TypeName.Reference.GetFSharpSymbol()
        
        let entity =
            match symbol with
            | :? FSharpEntity as entity -> Some entity
            | _ -> None
            |> Option.defaultWith(fun _ -> failwith "ExpectedFSharpEntity")
            
        // TODO: Handle generation of arguments for tupled arguments to member functions
        let memberDeclarations =
            entity.MembersFunctionsAndValues
            |> Seq.map(fun x ->
                let argNames =
                    x.CurriedParameterGroups
                    |> Seq.map(Seq.toList >> function | [] -> None | [singleParam] -> Some singleParam | _ -> failwith "Unexpected number of params")
                    |> Seq.map (Option.map (fun param -> param.Name |> Option.defaultWith(fun _ -> getUnnamedVariableName())))
                    |> Seq.toList
                let typeParams = x.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList
                let memberName = x.DisplayName
                factory.CreateMemberBindingExpr(memberName, typeParams, argNames)
                )
        let file = interfaceImplementation.FSharpFile
        let memberIndent = interfaceImplementation.Indent + file.GetIndentSize()
        let lineEnding = file.GetLineEnding()
        let mutable lastChild = interfaceImplementation.LastChild
        for declaration in memberDeclarations do
            let newLine = ModificationUtil.AddChildAfter(lastChild, NewLine(lineEnding))
            let space = ModificationUtil.AddChildAfter(newLine, Whitespace(memberIndent))
            lastChild <- ModificationUtil.AddChildAfter(space, declaration)
            
        null
