module Module

let f _ _ = ()

let s = f (sprintf {caret}"%d %s" 10 foo) "hi"
