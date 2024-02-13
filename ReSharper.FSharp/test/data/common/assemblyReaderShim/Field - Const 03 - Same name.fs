module Module

match 1 with
| Class1.Const -> ()
| Class2.Const -> ()
| _ -> ()

match "" with
| Class1.Const -> ()
| Class2.Const -> ()
| _ -> ()
