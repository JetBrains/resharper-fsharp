namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate

open FSharp.Compiler.Symbols

type IFSharpGeneratorElement =
    abstract Mfv: FSharpMemberOrFunctionOrValue
    abstract DisplayContext: FSharpDisplayContext
    abstract Substitution: (FSharpGenericParameter * FSharpType) list
    abstract AddTypes: bool
    abstract IsOverride: bool
