let f (x:int) (y:int) = x + y
let g = (f 2 3) 4 + (f 4 5 {caret}6 7 8 9)