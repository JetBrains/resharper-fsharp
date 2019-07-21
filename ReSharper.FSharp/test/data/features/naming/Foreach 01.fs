module Module


type Foo = { Field: int }
type Bar = { Mice: Foo list }

let bar: Bar = { Mice = [] }

for x{caret} in bar.Mice do ()
