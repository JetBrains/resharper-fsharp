module Test

let o = obj()
let _ = not not o :?> string
let _ = not true o :?> string
