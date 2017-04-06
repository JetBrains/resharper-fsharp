namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.Util

type FSharpFakeTreeBuilder(file, lexer, lifetime, logger : ILogger) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        logger.LogMessage(LoggingLevel.WARN, "FSharpFakeTreeBuilder: creating fake IFSharpFile")
        let mark = x.Builder.Mark()
        x.FinishFile mark ElementType.F_SHARP_IMPL_FILE
