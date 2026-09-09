module Module

let a: string = Class.NotNull("")
let b: string = Class.NotNull<string>(null)

let c: string | null = Class.Nullable("")
let d: string = Class.Nullable("")
