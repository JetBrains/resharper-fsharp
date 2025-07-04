namespace Test

module A =
    open System
    open System.Collections

    let f (x: #IDisposable & #IEnumerator) = ()

module B =
    open A

    let g x{caret} = f x 
