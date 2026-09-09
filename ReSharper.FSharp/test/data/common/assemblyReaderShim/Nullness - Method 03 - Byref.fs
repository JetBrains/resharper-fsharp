module Module

let mutable notNull = ""
let mutable nullable: string | null = null

Class.Out(&nullable)
Class.Out(&notNull)

Class.OutNotNull(&notNull)
Class.OutNotNull(&nullable)

Class.Ref(&notNull)
Class.Ref(&nullable)

Class.In(&nullable)
Class.In(&notNull)
