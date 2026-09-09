module Module

open System
open System.Collections.Generic

let a: Nullable<int> = Class.NullableInt()
let b: List<int> = Class.Ints()
let c: KeyValuePair<string, int> = Class.Pair()
let d: Nullable<KeyValuePair<int, string>> = Class.NullablePair()
