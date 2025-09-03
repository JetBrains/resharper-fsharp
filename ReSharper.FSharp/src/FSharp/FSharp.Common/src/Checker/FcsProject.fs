namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Generic
open System.IO
open FSharp.Compiler.CodeAnalysis
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util.Dotnet.TargetFrameworkIds

type FcsProjectKey =
    { Project: IProject
      TargetFrameworkId: TargetFrameworkId }

    static member Create(psiModule: IPsiModule) =
        { Project = psiModule.ContainingProjectModule :?> _
          TargetFrameworkId = psiModule.TargetFrameworkId }

    static member Create(project, targetFrameworkId) =
        { Project = project
          TargetFrameworkId = targetFrameworkId }


type FcsProject =
    { OutputPath: VirtualFileSystemPath
      ProjectOptions: FSharpProjectOptions
      ParsingOptions: FSharpParsingOptions
      FileIndices: IDictionary<VirtualFileSystemPath, int>
      ImplementationFilesWithSignatures: ISet<VirtualFileSystemPath>
      ReferencedModules: ISet<FcsProjectKey> }

    member x.IsKnownFile(sourceFile: IPsiSourceFile) =
        let path = sourceFile.GetLocation()
        x.FileIndices.ContainsKey(path)

    member x.GetIndex(sourceFile: IPsiSourceFile) =
        let path = sourceFile.GetLocation()
        tryGetValue path x.FileIndices |> Option.defaultValue -1

    member x.TestDump(writer: TextWriter) =
        let projectOptions = x.ProjectOptions

        writer.WriteLine($"Project file: {projectOptions.ProjectFileName}")
        writer.WriteLine($"Stamp: {projectOptions.Stamp}")
        writer.WriteLine($"Load time: {projectOptions.LoadTime}")

        writer.WriteLine("Source files:")
        for sourceFile in projectOptions.SourceFiles do
            writer.WriteLine($"  {sourceFile}")

        writer.WriteLine("Other options:")
        for option in projectOptions.OtherOptions do
            writer.WriteLine($"  {option}")

        writer.WriteLine("Referenced projects:")
        for referencedProject in projectOptions.ReferencedProjects do
            let stamp =
                match referencedProject with
                | FSharpReferencedProject.FSharpReference(_, options) -> $"{options.Stamp}: "
                | _ -> ""
            writer.WriteLine($"  {stamp}{referencedProject.OutputFile}")

        writer.WriteLine()
