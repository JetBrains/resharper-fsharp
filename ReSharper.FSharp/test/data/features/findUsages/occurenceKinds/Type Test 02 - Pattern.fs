module Module

match () with
| :? string -> ()
| :? System.String -> ()
| _ -> ()
