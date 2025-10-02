module Module

[1]
|> Seq.map (fun i ->
    let s: string = i{caret}
    ()
)
