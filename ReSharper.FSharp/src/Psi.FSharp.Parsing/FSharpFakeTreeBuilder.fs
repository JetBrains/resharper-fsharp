namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpFakeTreeBuilder(file, lexer, lifetime, logger: ILogger, options: FSharpParsingOptions option) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let filePath = file.GetLocation().FullPath
        logger.Warn("creating fake IFile for {0}, project options: {1}", filePath, options)
        let mark = x.Builder.Mark()
        x.FinishFile mark ElementType.F_SHARP_IMPL_FILE
