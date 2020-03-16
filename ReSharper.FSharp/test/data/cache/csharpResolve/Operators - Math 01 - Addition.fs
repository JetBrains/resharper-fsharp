namespace global

type T =
    { Field: int }

    static member Instance = { Field = 1 }

    static member (+) (a: T, b: T) = a
    static member (+) (a: T, b: int) = a
