module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.FSharpAutoOpenUtil

open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl.Reflection2
open JetBrains.Util

let getAutoOpenModules (psiAssemblyFileLoader: IPsiAssemblyFileLoader) (autoOpenCache: FSharpAutoOpenCache)
        (assembly: IPsiAssembly) =
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
                result.AddRange(autoOpenCache.GetAutoImportedElements(declaredElement, symbolScope))) |> ignore

    result
