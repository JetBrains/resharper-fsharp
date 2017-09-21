namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AllowNullLiteral>]
type IFSharpProjectOptionsProvider =
    abstract member GetProjectOptions: IPsiSourceFile -> FSharpProjectOptions option
    abstract member GetParsingOptions: IPsiSourceFile -> FSharpParsingOptions option
    abstract member TryGetFSharpProject: IPsiSourceFile -> FSharpProject option
    abstract member HasPairFile: IPsiSourceFile -> bool
