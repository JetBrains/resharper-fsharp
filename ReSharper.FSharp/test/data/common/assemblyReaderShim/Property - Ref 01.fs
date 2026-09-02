module Module

let c = Class()

let _: int = c.Prop
let _: int = c.RoProp

c.Prop <- 5
c.RoProp <- 5

let _: string = c.Prop
