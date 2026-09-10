module Test

module A = 
    module StreamReader =
        ()

let nameof x = x

nameof StreamReader{caret}
