namespace Test

module A =
    open System

    let f (x: #IDisposable) = ()

module B =
    open A

    let g x{caret} = f x 
