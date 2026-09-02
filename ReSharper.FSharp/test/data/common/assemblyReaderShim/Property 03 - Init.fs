module Module

let c = Class(GetInit = 1, Init = 1)

let _: int = c.Get
let _: int = c.GetSet
let _: int = c.Set
let _: int = c.GetInit
let _: int = c.Init

c.Get <- 1
c.GetSet <- 1
c.Set <- 1
c.GetInit <- 1
c.Init <- 1
