// ${RUN:Annotate all parameters}
module Module

let{off} f{off} x{on} y{on} = ()
let{off} f{off} x{on} y{on}: int = ()
let{off} f{off} x{off} (y{off}: int) = ()
let{off} f{off} (x{on}, y{on}) z{on} = ()
let{off} f{off} (x{off}: int, y{on}) z{on} = ()
let{off} f{off} (x{on}, (y{off}, z{off})) = ()
let{off} f{off} (So{off}me(x{off}), y{on}, z{on}) = ()

match Some(5, 5) with
| Some (x{off}, y{off}) -> ()

type A1(a{off}) = class end
type A2(a{on}, b{on}) = class end
type A3(a: int, b{on}, ?c{on}) = class end
type A4(a{on}, [<Attr>] ?b{on}) = class end
type A5() =
  member _.M{off} (x{on}, y{off}: int) (z{on}) = ()
  member _.Prop{off} = 3
