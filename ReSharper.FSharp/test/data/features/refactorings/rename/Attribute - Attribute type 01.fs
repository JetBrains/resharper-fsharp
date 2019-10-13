//${NEW_NAME:BarAttribute}
module Module

type AAttribute() =
    inherit Attribute()

 
type {caret}FooAttribute() =
    class
    end
 
let foo: FooAttribute = FooAttribute()
 
[<A; Foo>]
let q =
    let [<FooAttribute>] qwe = 123
    1
