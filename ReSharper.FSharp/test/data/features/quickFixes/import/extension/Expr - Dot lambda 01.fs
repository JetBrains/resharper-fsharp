module Extensions =
    type System.String with
        member this.Ext() = ()

let s = ""
s |> _.Ext{caret}
