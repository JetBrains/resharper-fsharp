namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions

[<ContextActionGroup(Id = "F#", Name = "F#")>]
[<AbstractClass; Sealed>]
type FSharpContextActions private () =
    class
    end
