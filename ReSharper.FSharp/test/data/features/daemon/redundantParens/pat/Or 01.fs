module Module

match (), () with
| (_ | _) -> ()
| (_ | _) & _ -> ()
| _, (_ | _) -> ()
