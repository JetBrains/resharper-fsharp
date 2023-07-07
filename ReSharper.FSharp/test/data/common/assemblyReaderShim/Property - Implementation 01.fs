module Module


{ new Interface with
    member _.Prop with get() = "" } |> ignore

type T() =
    interface Interface with
        member this.Prop = ""
