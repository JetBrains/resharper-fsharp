let _: int = Class.IntConst
let _: string = Class.StringConst

match 1 with
| Class.IntConst -> ()
| _ -> ()

match "" with
| Class.StringConst -> ()
| _ -> ()
