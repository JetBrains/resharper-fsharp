module Module

type Record =
    { Field: string }

let foo = { Field = "" }
let x{caret} = foo.Field
