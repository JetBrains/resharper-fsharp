type X = 
    | X of int * string * float

match X(1,"2",3.) with
| X({off}a,_,c) -> ()
