type Bar = { Baz : int }
type Record = { Foo : Bar }
let { Foo = { Bar.Baz = 1 } } = failwith ""
