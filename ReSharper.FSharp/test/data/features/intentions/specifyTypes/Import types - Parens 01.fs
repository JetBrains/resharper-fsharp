namespace Test

module A =
    let x = 1, ("".GetType(), "".GetType().GetMethod(""))

module B =
    open A

    let y{caret} = x
