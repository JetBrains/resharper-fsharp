type Record0 = { Foo: int; Bar: int }
type Record1 = { Foo: int; Bar: int; Zoo: Record0 }

[<AutoOpen>]
module Module =
    type Record2 = { Foo: Record1; Bar: Record1 }
    let item: Record2 = null

let Zoo = { Record0.Foo = 3; Bar = 3 }
let item = Module.item
let item2 = item

let f<'a> = Zoo

// Available
ignore { item with Foo = { item.Foo with Foo = 3 } }
ignore { item with Foo.Zoo = { item.Foo.Zoo with Foo = 3 } }
ignore { Module.item with Foo = { Module.item.Foo with Foo = 3 } }
ignore { (Module.item) with Foo = ({ Module.item.Foo with Foo = 3 }) }
ignore { item with Foo = { item.Foo with Zoo = { Foo = 3; Bar = 4 } } }
ignore { item with Module.Record2.Foo.Zoo = { item.Foo.Zoo with Foo = 3 } }
ignore { item with Foo = { item.Foo with Zoo = { item.Bar.Zoo with Foo = 3 } } }
ignore { item with Foo = { item.Foo with Zoo = { item.Foo.Zoo with Foo = 3 } } }
ignore { item with Bar = { item.Foo with Zoo = { item.Foo.Zoo with Bar = 3 } } }
ignore { Module.item with Foo = { item.Foo with Zoo = { item.Foo.Zoo with Foo = 3 } } }
ignore { item with Foo = { item.Foo with Foo = 3 }; Bar = { item.Bar with Foo = 3 } }

// Not available
ignore { item with Foo = { item.Bar with Foo = 3 } }
ignore { item with Foo.Zoo = { Zoo with Foo = 3 } }
ignore { item with Foo.Zoo = { f<int> with Foo = 3 } }
ignore { Module.item with Foo = { item.Foo with Foo = 3 } }
ignore { item with Foo = { Module.item.Foo with Foo = 3 } }
ignore { item with Foo = { Foo = 3; Bar = 3; Zoo = null } }
ignore { item with Foo = { item.Foo with Foo = 3; Bar = 3 } }
ignore { (id item) with Foo = { (id item).Foo with Foo = 3 } }
ignore { item with Foo = { item2.Foo with Foo = 3 } }
ignore { item with Foo = { item.Foo with Unresolved = 3 } }
ignore { item with Unresolved = { item.Unresolved with Unresolved = 3 } }
