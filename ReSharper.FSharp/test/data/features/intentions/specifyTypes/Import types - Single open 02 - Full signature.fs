namespace Test

module A =
    open System

    let f (x: IDisposable) (y: IDisposable) = "".GetType()

module B =
    open A

    let g{caret} x y = f x y 
