let html (s: string) = s
let sql (a: int) (s: string) = s

html "direct"
html ("parens")
"forward" |> html
html <| "backward"
sql 1 "second argument"
"second argument, forward" |> sql 1
id "other function"
