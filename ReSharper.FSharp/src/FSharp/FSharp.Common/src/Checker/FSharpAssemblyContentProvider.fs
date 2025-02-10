namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Application.Threading
open JetBrains.ReSharper.Feature.Services

[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpAssemblyContentProvider(lifetime, onSolutionCloseNotifier: OnSolutionCloseNotifier,
        locks: IShellLocks) =
    let entityCache = EntityCache()
    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ -> entityCache.Clear())

    member x.GetLibrariesEntities(checkResults: FSharpCheckFileResults) =
        locks.AssertReadAccessAllowed()

        // FCS sometimes returns several FSharpAssembly for single referenced assembly.
        // For example, it returns two different ones for Swensen.Unquote; the first one
        // contains no useful entities, the second one does. Our cache prevents to process
        // the second FSharpAssembly which results with the entities containing in it to be
        // not discovered.
        let assembliesByFileName =
            checkResults.ProjectContext.GetReferencedAssemblies()
            |> Seq.filter _.IsFSharp
            |> Seq.groupBy _.FileName

        assembliesByFileName
        |> List.ofSeq
        |> List.collect (fun (fileName, signatures) ->
            Interruption.Current.CheckAndThrow()

            let signatures = List.ofSeq signatures
            AssemblyContent.GetAssemblyContent entityCache.Locking AssemblyContentType.Public fileName signatures
        )
