namespace Test

module A =
    open System

    let f (x: 'a when 'a: not struct and 'a :> IDisposable) = ()

module B =
    open A

    let g x{caret} = f x 
