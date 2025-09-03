namespace Test

module A =
    open System.IO
    open System.Collections.Generic

    let f (x: MemoryStream) (y: Queue<int>) = "".GetType()

module B =
    open A

    let g{caret} x y = f x y 
