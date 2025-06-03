namespace Test

module A =
    open System.Collections
    open System.Collections.Generic
    open System.IO
    open System.Linq.Expressions
    let anon = {| A = 1; B = "".GetType().GetMethod("") |}
    let f (x: BitArray -> (Queue<LinkedList<string> * 'a> -> StreamReader) option) (y: BitArray, z: #Expression & #(int seq)) = anon


module B =
    open A
    let g{caret} x (y, z) =
        f x (y, z)
