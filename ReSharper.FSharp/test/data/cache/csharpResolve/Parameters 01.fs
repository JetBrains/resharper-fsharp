module Module

let f (x: string, [<System.ParamArray>] y: obj array) = ()
  
type A(x: string, [<System.ParamArray>] y: obj array) =
  new(x1: int, [<System.ParamArray>] y2: obj array) = ResourceString("", 1)

type B(x: obj array) = class end
