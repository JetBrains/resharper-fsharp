type U =
    | U of a:int * b:int * c:int * d:int

match U(a = 1, b = 2, c = 3, d = 4) with
| U(a = a; b = {caret}b
    c = c; d = d) -> ()
