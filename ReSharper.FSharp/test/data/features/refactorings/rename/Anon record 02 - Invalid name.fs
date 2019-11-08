//${NEW_NAME:1}
module Module

type AR = {| Field: int |}
let ar = {| Field = 123 |}
ar.Field{caret}
