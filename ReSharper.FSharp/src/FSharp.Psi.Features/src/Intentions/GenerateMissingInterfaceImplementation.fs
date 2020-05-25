namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open  FSharp.Compiler.SourceCodeServices

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
        // TODO: Maybe just do it on the interface name?
        let declaration = dataProvider.GetSelectedElement<IInterfaceImplementation>()
        if isNull declaration then false else
        let typeName = declaration.TypeName
        if isNull typeName then false else
        let reference = typeName.Reference
        if isNull reference then false else
        let symbol = reference.GetSymbolUse().Symbol
        let entity =
            match symbol with
            | :? FSharpEntity as entity -> Some entity
            | _ -> None
            |> Option.defaultWith(fun _ -> failwith "ExpectedFSharpEntity")
        
        let isNotImplemented = declaration.TypeMembersEnumerable |> Seq.map(fun x -> x.SourceName) |> Seq.isEmpty
        entity.IsInterface && isNotImplemented

    override x.ExecutePsiTransaction(_, _) =
//        let fieldDecl = dataProvider.GetSelectedElement<IDeclaration>()
//        let declaredElement = fieldDecl.DeclaredElement :?> IRecordField
//        declaredElement.SetIsMutable(true)
        // let expectedMembers = e.MembersFunctionsAndValues |> Seq.map (fun x -> x.CurriedParameterGroups) // TODO MC: Don't try to add types, just group as curried parameters with names

        null
