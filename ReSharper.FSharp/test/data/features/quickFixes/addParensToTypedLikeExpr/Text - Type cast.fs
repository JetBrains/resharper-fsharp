module Test

let o = obj()
let _ = not o :?> string
let _ = not o :> string
