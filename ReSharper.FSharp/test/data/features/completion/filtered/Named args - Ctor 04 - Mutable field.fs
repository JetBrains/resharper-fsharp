// ${FILTER_ITEMS:F}
type Z =
    val mutable F: int
    new() = { F = 0 }

let z = Z({caret})
