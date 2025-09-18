match x with
| { F1 = f1
    F2 = f2{caret} // F2 is unused
    F3 = f3 } -> ()
