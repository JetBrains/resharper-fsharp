namespace global

open System

[<AttributeUsage(AttributeTargets.All, AllowMultiple = true)>]
type FooAttribute() =        
    inherit Attribute()

type BarAttribute() =        
    inherit Attribute()
