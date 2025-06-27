namespace Test

module A =
    let f x = ()

module B =
    open A

    let y x{caret} = f x
