namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open FSharp.Compiler.ExtensionTyping
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

[<SolutionComponent>]
type FsiSessionsHostStub() =
    interface IHideImplementation<FsiHost>

[<ShellComponent>]
type FSharpFileServiceStub() =
    interface IHideImplementation<FSharpFileService>

    interface IFSharpFileService with
        member x.IsScratchFile _ = false
        member x.IsScriptLike _ = false

[<SolutionComponent>]
type TestExtensionTypingProviderStub() =
    interface IProxyExtensionTypingProvider with
        member this.DisplayNameOfTypeProvider(typeProvider, fullName) =
            ExtensionTypingProvider.DisplayNameOfTypeProvider(typeProvider, fullName)

        member this.DumpTypeProvidersProcess() = ""
        member this.GetInvokerExpression(provider, methodBase, paramExprs) =
            ExtensionTypingProvider.GetInvokerExpression(provider, methodBase, paramExprs)

        member this.GetProvidedTypes(pn) = ExtensionTypingProvider.GetProvidedTypes(pn)

        member this.HasGenerativeTypeProviders _ = false

        member this.InstantiateTypeProvidersOfAssembly(runtimeAssemblyFilename, designerAssemblyName, var0, var1, isInteractive, systemRuntimeContainsType, systemRuntimeAssemblyVersion, compilerToolsPath, logError, m) =
            ExtensionTypingProvider.InstantiateTypeProvidersOfAssembly(runtimeAssemblyFilename, designerAssemblyName, var0, var1, isInteractive, systemRuntimeContainsType, systemRuntimeAssemblyVersion, compilerToolsPath, logError, m)

        member this.ResolveTypeName(pn, typeName) = ExtensionTypingProvider.ResolveTypeName(pn, typeName)

        member this.RuntimeVersion() = failwith "todo"
