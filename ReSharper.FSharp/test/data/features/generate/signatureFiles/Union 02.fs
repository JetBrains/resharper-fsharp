// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

type Bar = | Bar of a:int * b:int
           static member Add x y = x + y
{caret}
