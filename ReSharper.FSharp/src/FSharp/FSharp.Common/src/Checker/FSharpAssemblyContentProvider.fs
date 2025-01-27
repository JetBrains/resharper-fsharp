namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Application.Threading
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpAssemblyContentProvider(lifetime, onSolutionCloseNotifier: OnSolutionCloseNotifier,
        locks: IShellLocks) =
    let entityCache = EntityCache()
    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ -> entityCache.Clear())

    member x.GetLibrariesEntities(checkResults: FSharpCheckFileResults, solution: ISolution) =
        locks.AssertReadAccessAllowed()

        [
          // FCS sometimes returns several FSharpAssembly for single referenced assembly.
          // For example, it returns two different ones for Swensen.Unquote; the first one
          // contains no useful entities, the second one does. Our cache prevents to process
          // the second FSharpAssembly which results with the entities containing in it to be
          // not discovered.
          let assembliesByFileName =
              checkResults.ProjectContext.GetReferencedAssemblies()
              |> Seq.groupBy (fun asm -> asm.FileName)
              |> Seq.map (fun (fileName, asms) -> fileName, List.ofSeq asms)
              |> Seq.toList
              |> List.rev // if mscorlib.dll is the first then FSC raises exception when we try to
                          // get Content.Entities from it.

          Interruption.Current.CheckAndThrow()

          let assembliesByFileName =
              let assemblyReaderShim = solution.GetComponent<IFcsAssemblyReaderShim>()
              if not assemblyReaderShim.IsEnabled then assembliesByFileName else

              assembliesByFileName
              |> List.filter (fun (path, _) ->
                  Interruption.Current.CheckAndThrow()

                  let path =
                      path
                      |> Option.map (fun path -> VirtualFileSystemPath.TryParse(path, InteractionContext.SolutionContext))
                      |> Option.defaultWith (fun _ -> VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext))

                  // Remove types from module readers from FCS import completion
                  path.IsEmpty || not (assemblyReaderShim.IsKnownModule(path)))

          for fileName, signatures in assembliesByFileName do
              Interruption.Current.CheckAndThrow()
              yield! AssemblyContent.GetAssemblyContent entityCache.Locking AssemblyContentType.Public fileName signatures ]
