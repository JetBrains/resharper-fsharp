module Module

type T =
    { Field: int }

    static member Instance = { Field = 1 }

    static member op_GreaterThan(a: T, b) = a > b
    static member op_LessThan(a: T, b) = a < b
