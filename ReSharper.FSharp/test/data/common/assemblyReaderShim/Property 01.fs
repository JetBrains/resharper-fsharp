module Module

let _: int = Class.StaticGet
let _: int = Class.StaticGetSet
let _: int = Class.StaticSet

Class.StaticGet <- 1
Class.StaticGetSet <- 1
Class.StaticSet <- 1


let _: int = Class.StaticExpressionBody
Class.StaticExpressionBody <- 1

let _: int = Class.StaticProp
Class.StaticProp <- 1


let c = Class()

let _: int = c.Get
let _: int = c.GetSet
let _: int = c.GetGet
let _: int = c.GetInit
let _: int = c.Init

c.Get <- 1
c.GetSet <- 1
c.GetGet <- 1
c.GetInit <- 1
c.Init <- 1


let _: int = Class.SetWithoutBody
Class.SetWithoutBody <- 1

let _: int = Class.InitWithoutBody
Class.InitWithoutBody <- 1

let _: int = Class.WrongAccessorName1
Class.WrongAccessorName1 <- 1

let _: int = Class.WrongAccessorName2
Class.WrongAccessorName2 <- 1

let _: int = Class.NoAccessors
Class.NoAccessors <- 1
