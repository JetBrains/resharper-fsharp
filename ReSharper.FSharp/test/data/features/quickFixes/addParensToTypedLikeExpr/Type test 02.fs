let f _ _ = ()

let o = obj()
f 1 o :?{caret} string
