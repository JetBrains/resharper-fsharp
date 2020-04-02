type 'a MyRecord = { MyOption : 'a option }

let x = { MyOption = Some 15. }
match x with
| { MyOption = None } -> ()
| { MyOption = Some x } -> ()
