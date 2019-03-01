module Module

type Ta() =
    abstract M: int
    default x.M = 123

let t = {
    new Ta() with
        override x.M = 234
}
