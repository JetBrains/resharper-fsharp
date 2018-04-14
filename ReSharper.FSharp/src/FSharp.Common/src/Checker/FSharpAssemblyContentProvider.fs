namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open JetBrains.Application
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<ShellComponent>]
type FSharpAssemblyContentProvider() =
    let entityCache = EntityCache()

    member x.GetLibrariesEntities(checkResults: FSharpCheckFileResults) =
        let getEntitiesAsync =
            async {
            return
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
        
                  for fileName, signatures in assembliesByFileName do
                      yield! AssemblyContentProvider.getAssemblyContent entityCache.Locking Public fileName signatures ]
            }
        getEntitiesAsync.RunAsTask()