module A

type X =
    | X of x : int * y: int

let (X(a{caret},b)) = X (1,2)
