namespace Test

module A =
    let x: System.Type | null = "".GetType()

module B =
    open A

    let y{caret} = x
