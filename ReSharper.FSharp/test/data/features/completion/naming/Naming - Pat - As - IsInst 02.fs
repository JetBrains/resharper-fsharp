module Module

open System.Text

match obj() with
| :? StringBuilder as {caret}
