module Module

let foo = "foo"
foo |> sprintf {caret}"Text is %s" |> ignore
