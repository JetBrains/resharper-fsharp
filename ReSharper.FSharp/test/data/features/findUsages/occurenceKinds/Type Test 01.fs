module Module

let s: string = ""
obj :?> string

obj() :? string
obj() :? System.String

match obj() with
| :? string -> ()
| :? System.String -> ()
| foo -> ()
