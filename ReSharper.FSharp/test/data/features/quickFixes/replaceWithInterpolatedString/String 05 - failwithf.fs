module Module

let foo = "foo"
let s = failwithf {caret}"Number is %d and text is %s" 10 foo
