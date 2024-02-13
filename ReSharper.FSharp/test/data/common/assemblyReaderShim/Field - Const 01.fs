module Module

match Class.ConstZero with
| 1 -> ()


match 0 with
| Class.ConstZero -> ()


match 0 with
| Class.ConstOne -> ()


match 0 with
| Class.ConstZero -> ()
| Class.ConstOne -> ()


let _: int = Class.ConstZero
