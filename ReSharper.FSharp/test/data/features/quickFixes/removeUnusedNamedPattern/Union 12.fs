type U =
    | U of a:int * b:int * c:int

match U(a = 1, b = 2, c = 3) with
| U(a = {caret}a
    b = b // b is unused
    c = c) -> ()
