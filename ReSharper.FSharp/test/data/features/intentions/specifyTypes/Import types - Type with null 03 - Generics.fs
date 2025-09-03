namespace Test

module A =
    let x: List<int * List<int> | null> | null = []


module B =
    open A

    let y{caret} = x
