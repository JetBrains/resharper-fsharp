module Module

let s = "12345"
let _ = s.Substring(1, 3).Sub{caret}string(1).Substring(1)

