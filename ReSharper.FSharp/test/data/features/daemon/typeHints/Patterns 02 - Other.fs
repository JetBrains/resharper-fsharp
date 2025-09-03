module Test

let _ = 
    for i = 0 to 10 do ()
    for i in Seq.empty do ()
    function x -> ()

match [Some 5] with
| [] -> ()
| [x] -> ()
| [x: int option] -> ()
| [x; y] -> ()
| [x; y: int option] -> ()
| [_; x] -> ()
| [_; _] -> ()
| [x; _; _] -> ()
| [x; _; Some 5] -> ()
| x :: tail -> ()
| _ :: (tail: int option list) -> ()
| x: int option :: tail -> ()
| x: int option :: _ -> ()
| x :: (tail: int option list) -> ()
| ((x :: tail): int option list) -> ()
| _ :: _ -> ()
| x :: _ -> ()
| _ :: x :: _ -> ()
| x :: _ :: _ -> ()
| x :: Some y :: _ -> ()
| x :: (y :: _) -> ()
| x :: y :: [] -> ()
| x :: [y] -> ()
| Some(x) :: tail -> () 
| [Some(x)]
| [_; Some(x)] when let x = 5 in true ->
    let y = 5 in ()

match [[5]] with
| [[]] -> ()
| [[]; x] -> ()

match [|5|] with
| [||] -> ()
| [|x|] -> ()
| [|x; y|] -> ()

exception MyException of string
try () with | MyException(x) -> ()
