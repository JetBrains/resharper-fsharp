module Module

let foo = "foo"
let s = foo |> sprintf {caret}"Number is %d and text is %s" 10
