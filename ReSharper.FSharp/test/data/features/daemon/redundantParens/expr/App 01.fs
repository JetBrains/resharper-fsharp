module M

type T() =
    static member M() = ""
    member this.P = 1

id (id())
id (id ())

id (T().P)
id (T.M().Length)

(T().P) |> ignore
(T.M().Length) |> ignore

ignore <| (T().P)
ignore <| (T.M().Length)
