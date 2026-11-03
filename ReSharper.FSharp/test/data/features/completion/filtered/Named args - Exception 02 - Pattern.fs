exception E of x: int * y: int

match failwith "" with
| E(x = 1; {caret}) -> ()
| _ -> ()
