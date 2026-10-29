module Module

open System.ComponentModel

type T() =
    [<EditorBrowsable(EditorBrowsableState.Always)>]
    member this.AlwaysProp = 1

    [<EditorBrowsable(EditorBrowsableState.Advanced)>]
    member this.AdvancedProp = 1

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.NeverProp = 1

    member this.UnmarkedProp = 1

    [<EditorBrowsable(EditorBrowsableState.Always)>]
    member this.AlwaysMethod() = 1

    [<EditorBrowsable(EditorBrowsableState.Advanced)>]
    member this.AdvancedMethod() = 1

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.NeverMethod() = 1

    member this.UnmarkedMethod() = 1

let t = T()
t.{caret}
