module Module

let s = sprintf {caret}"%d" 10
let _ = sprintf "%d" s.Length

let i = 10
let _ = sprintf "%d" i
