namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System.Collections.Generic
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpProject =
    {
        Options: FSharpProjectOptions option
        ConfigurationDefines: string list
        FileIndices: Dictionary<FileSystemPath, int>
        FilesWithPairs: HashSet<FileSystemPath>
        ReferencingProjects: IProject list
        mutable ParsingOptions: FSharpParsingOptions option
    }
    member x.ContainsFile (file: IPsiSourceFile) =
        x.FileIndices.ContainsKey(file.GetLocation())
