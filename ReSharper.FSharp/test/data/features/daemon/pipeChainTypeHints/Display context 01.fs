module A =
    open System.IO

    let f _ = new StreamReader("")

    let _ = ""
            |> f

module B =
    open A

    let _ = ""
            |> f
