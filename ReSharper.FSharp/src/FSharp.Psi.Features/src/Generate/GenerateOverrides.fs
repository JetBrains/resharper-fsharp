namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate

open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

type IFSharpGeneratorElement =
    abstract Mfv: FSharpMemberOrFunctionOrValue
    abstract Substitution: (FSharpGenericParameter * FSharpType) list
    abstract AddTypes: bool

module GenerateOverrides =
    let generateMember (context: IFSharpTreeNode) displayContext (element: IFSharpGeneratorElement) =
        let mfv = element.Mfv

        let mutable nextUnnamedVariableNumber = 0
        let getUnnamedVariableName () =
            let name = sprintf "var%d" nextUnnamedVariableNumber
            nextUnnamedVariableNumber <- nextUnnamedVariableNumber + 1
            name

        let argNames =
            mfv.CurriedParameterGroups
            |> Seq.map (Seq.map (fun x ->
                let name = x.Name |> Option.defaultWith (fun _ -> getUnnamedVariableName ())
                name, x.Type.Instantiate(element.Substitution)) >> Seq.toList)
            |> Seq.toList

        let typeParams = mfv.GenericParameters |> Seq.map (fun param -> param.Name) |> Seq.toList
        let memberName = mfv.LogicalName

        let factory = context.CreateElementFactory()
        let settingsStore = context.GetSettingsStoreWithEditorConfig()
        let spaceAfterComma = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAfterComma)
        
        let paramGroups =
            if mfv.IsProperty then [] else
            factory.CreateMemberParamDeclarations(argNames, spaceAfterComma, element.AddTypes, displayContext)

        let memberDeclaration = factory.CreateMemberBindingExpr(memberName, typeParams, paramGroups)

        if element.AddTypes then
            let lastParam = memberDeclaration.ParametersPatterns.LastOrDefault()
            if isNull lastParam then () else

            let typeString = mfv.ReturnParameter.Type.Instantiate(element.Substitution)
            let typeUsage = factory.CreateTypeUsage(typeString.Format(displayContext))
            ModificationUtil.AddChildAfter(lastParam, factory.CreateReturnTypeInfo(typeUsage)) |> ignore

        memberDeclaration
