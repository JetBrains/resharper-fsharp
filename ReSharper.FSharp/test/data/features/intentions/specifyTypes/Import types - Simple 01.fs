namespace Test

module A =
    let x = "".GetType()

module B =
    open A

    let y{caret} = x
