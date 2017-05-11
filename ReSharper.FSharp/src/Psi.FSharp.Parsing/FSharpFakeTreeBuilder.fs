namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpFakeTreeBuilder(file, lexer, lifetime, logger : ILogger, options : FSharpProjectOptions option) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let filePath = file.GetLocation().FullPath
        logger.LogMessage(LoggingLevel.WARN, sprintf "creating fake IFile for %s, project options: %A" filePath options)
        let mark = x.Builder.Mark()
        x.FinishFile mark ElementType.F_SHARP_IMPL_FILE
