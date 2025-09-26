type U =
    | U of a:int * b:int * c:int

match U(a = 1, b = 2, c = 3) with
| U(a = a // I'm a comment
    b = {caret}b; c = c) -> ()
