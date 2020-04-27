let printer{caret} a : string =
    let dotted = a |> snd |> String.concat "."
    (a |> fst) + dotted

[<EntryPoint>]
let main (argv : string []) : int =
    printer ("string", ["a"; "b"; "c"]) |> stdout.WriteLine
    0 