namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Services
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpLiteralService() =
    inherit LiteralService()

    override this.IsConstantLiteral _ = failwith "todo"

    override this.GetValueTextRange(element) = element.GetTreeTextRange()

    override this.ParseStringLiteral(text: string, translator: byref<RangeTranslator>): string =
        translator <- RangeTranslator()
        translator.StartMapping(text.Length)
        translator.EndMapping(text.Length)
        text
