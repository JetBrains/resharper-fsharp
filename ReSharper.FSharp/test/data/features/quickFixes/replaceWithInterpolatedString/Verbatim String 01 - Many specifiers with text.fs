module Module

let foo = "foo"
let s = sprintf {caret}@"Number \n is %d and text is %s" 10 foo
