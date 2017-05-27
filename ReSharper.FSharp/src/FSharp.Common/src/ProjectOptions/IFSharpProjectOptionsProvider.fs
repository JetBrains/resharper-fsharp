namespace JetBrains.ReSharper.Plugins.FSharp.Common.ProjectOptions

open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AllowNullLiteral>]
type IFSharpProjectOptionsProvider =
    abstract member GetProjectOptions : IPsiSourceFile * FSharpChecker -> FSharpProjectOptions option
    abstract member GetProjectOptions : IProject -> FSharpProjectOptions option
    abstract member TryGetFSharpProject : IProject -> FSharpProject option
    abstract member GetFileIndex : IPsiSourceFile -> int
    abstract member HasPairFile : IPsiSourceFile -> bool
