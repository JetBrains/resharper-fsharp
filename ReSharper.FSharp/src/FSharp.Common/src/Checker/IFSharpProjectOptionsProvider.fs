namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AllowNullLiteral>]
type IFSharpProjectOptionsProvider =
    abstract member GetProjectOptions: IPsiSourceFile * FSharpChecker * updateScriptOptions: bool -> FSharpProjectOptions option
    abstract member GetParsingOptions: IPsiSourceFile * FSharpChecker * updateScriptOptions: bool -> FSharpParsingOptions option
    abstract member TryGetFSharpProject: IPsiSourceFile * FSharpChecker -> FSharpProject option
    abstract member GetFileIndex: IPsiSourceFile * FSharpChecker -> int
    abstract member HasPairFile: IPsiSourceFile * FSharpChecker -> bool
