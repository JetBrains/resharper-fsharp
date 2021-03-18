module Module

let foo = "foo"
let s = sprintf {caret}"%d %s" 10 foo
let _ = sprintf "%d %s" s.Length foo
