module Module

type R =
    { Field: int
      Another: int }

let { Another = x } = { Field = 123; Another = 123 }
