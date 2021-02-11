module M

match () with
| _ -> (ignore ();
    ignore ())
| _ -> ()

match () with
| _ -> (ignore ();
    ignore ();
        ignore ())
| _ -> ()

match () with
| _ -> (ignore ();
    ignore ();
        ignore ())
| _ -> ()
