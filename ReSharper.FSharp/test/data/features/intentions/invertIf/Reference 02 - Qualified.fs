type System.String with
    member x.IsEmpty = x.Length = 0

let foo = ""

if{caret} foo.IsEmpty then "a" else "b"
