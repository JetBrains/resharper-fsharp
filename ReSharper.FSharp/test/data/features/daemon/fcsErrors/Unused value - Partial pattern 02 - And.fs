module Module

match () with
| a & _
| (_ as a) & _ -> ()

match () with
| _ as a & _
| a & _ -> ()

match () with
| _ & a
| _ & a -> ()
