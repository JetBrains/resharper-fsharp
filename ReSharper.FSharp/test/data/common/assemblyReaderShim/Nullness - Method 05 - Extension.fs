module Module

let notNull = ""
let nullable: string | null = null

let a: string = notNull.NotNull()
let b: string = nullable.NotNull()

let c: string | null = notNull.Nullable()
let d: string = notNull.Nullable()
