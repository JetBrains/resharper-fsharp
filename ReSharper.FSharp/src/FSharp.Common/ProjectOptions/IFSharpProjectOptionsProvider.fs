namespace JetBrains.ReSharper.Plugins.FSharp.Common.ProjectOptions

open JetBrains.ReSharper.Psi
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AllowNullLiteral>]
type IFSharpProjectOptionsProvider =
    abstract member GetProjectOptions : IPsiSourceFile * FSharpChecker * bool -> FSharpProjectOptions option
    abstract member GetDefines : IPsiSourceFile -> string list