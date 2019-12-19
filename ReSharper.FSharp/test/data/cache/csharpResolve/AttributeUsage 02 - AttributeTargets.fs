namespace global

open System

[<AttributeUsage(AttributeTargets.Method)>]
type FooAttribute() =        
    inherit Attribute()

type BarAttribute() =        
    inherit Attribute()
