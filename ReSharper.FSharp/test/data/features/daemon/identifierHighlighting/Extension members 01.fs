open System

type String with    
    static member StaticMethod() = "M"
    static member StaticProperty = "P"
    member x.Method() = "m"
    member x.Property = "p"
