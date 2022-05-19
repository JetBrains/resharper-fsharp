type Thing() =
    class
    end

module A =
    type B() =
        class
        end

 
let mkBee () : Thing = A.B(){caret}
