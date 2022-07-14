[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.FSharpAutoOpenUtil

open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.Impl.Reflection2
open JetBrains.Util

let rec getNestedAutoImportedModules (declaredElement: IClrDeclaredElement) (symbolScope: ISymbolScope) = seq {
    for typeElement in getNestedTypes declaredElement symbolScope do
        if typeElement.HasAutoOpenAttribute() then
            yield typeElement
            yield! getNestedAutoImportedModules typeElement symbolScope }

let getAutoOpenModules (psiAssemblyFileLoader: IPsiAssemblyFileLoader) (assembly: IPsiAssembly) =
    let result = List()

    psiAssemblyFileLoader.GetOrLoadAssembly(assembly, true, fun psiAssembly assemblyFile metadataAssembly ->
        let attributesSet = assemblyFile.CreateAssemblyAttributes()
        let attributes = getAutoOpenAttributes attributesSet

        if attributes.IsEmpty() then () else

        let psiServices = psiAssembly.PsiModule.GetPsiServices()
        let symbolScope = psiServices.Symbols.GetSymbolScope(psiAssembly.PsiModule, false, true)

        for attribute in attributes do
            let moduleString = attribute.PositionParameter(0).ConstantValue.AsString()
            if moduleString.IsNullOrEmpty() then () else

            for declaredElement in symbolScope.GetElementsByQualifiedName(moduleString) do
                result.Add(declaredElement)
                result.AddRange(getNestedAutoImportedModules declaredElement symbolScope)) |> ignore

    result
