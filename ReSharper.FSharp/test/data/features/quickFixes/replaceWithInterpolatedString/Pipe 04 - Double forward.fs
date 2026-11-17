module Module

let foo = "foo"
let s = (10, foo) ||> sprintf {caret}"Number is %d and text is %s"
