namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AllowNullLiteral>]
type IFSharpProjectOptionsProvider =
    abstract member GetProjectOptions: IPsiSourceFile * updateScriptOptions: bool -> FSharpProjectOptions option
    abstract member GetParsingOptions: IPsiSourceFile -> FSharpParsingOptions option
    abstract member TryGetFSharpProject: IPsiSourceFile -> FSharpProject option
    abstract member GetFileIndex: IPsiSourceFile * FSharpChecker -> int
    abstract member HasPairFile: IPsiSourceFile * FSharpChecker -> bool
