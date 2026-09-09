module Module

let a: string = Class.NotNull
let b: string = Class.Nullable
let c: string | null = Class.Nullable

Class.NotNull <- null
Class.Nullable <- null

let d: string = Class().InstanceNullable
