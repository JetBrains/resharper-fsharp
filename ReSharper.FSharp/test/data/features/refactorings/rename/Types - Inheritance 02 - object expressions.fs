module Module

type T() =
    abstract M: int
    default x.M = 123

let t1 = {
    new T() with
        override x.M = 234
}

let t2 = {
    new T{caret}() with
        override x.M = 234
}
