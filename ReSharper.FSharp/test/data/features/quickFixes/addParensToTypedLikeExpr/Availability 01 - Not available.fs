module Test

let o = obj()
let _ = not not o :?> string
