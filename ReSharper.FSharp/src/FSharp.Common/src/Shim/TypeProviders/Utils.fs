module JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders.Utils

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel

type IProject with
    member x.IsFSharpWithoutGenerativeTypeProviders with get() =
        x.IsFSharp &&

        let solution = x.GetSolution()
        let paths = solution.GetComponent<IFcsProjectProvider>().GetProjectOutputPaths(x)
        let typeProvidersShim = solution.GetComponent<IProxyExtensionTypingProvider>()
        let typeProvidersManager = typeProvidersShim.TypeProvidersManager

        // We can determine which projects contain generative provided types
        // only from type providers hosted out-of-process
        not typeProvidersShim.IsConnectionAlive ||
        paths |> Array.filter typeProvidersManager.HasGenerativeTypeProviders |> Array.isEmpty
