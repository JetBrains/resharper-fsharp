module A

type X = { X : int; Y : int }

let { X = a{caret}; Y = b }  = { X = 1; Y = 2 }
