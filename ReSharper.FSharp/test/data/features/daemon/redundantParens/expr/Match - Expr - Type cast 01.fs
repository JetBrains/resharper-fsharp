module M

match ("" :> string) with | _ -> ()
match ("" :?> string) with | _ -> ()
