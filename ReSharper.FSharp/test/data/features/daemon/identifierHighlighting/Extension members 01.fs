open System

type String with    
    static member StaticMethod() = "Lul"
    static member StaticProperty = "Two"
    member x.Method() = "Two"
    member x.Property = "OneOne"