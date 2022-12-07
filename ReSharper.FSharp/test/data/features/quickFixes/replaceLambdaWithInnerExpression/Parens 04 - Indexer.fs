let f x y = ()
let funcs = [id]

f (fun x ->{caret} funcs[0] x) 1 
