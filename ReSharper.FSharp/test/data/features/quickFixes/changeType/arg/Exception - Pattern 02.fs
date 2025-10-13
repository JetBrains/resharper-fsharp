module Module

exception E of int * int

try () with E(1, ""{caret}) -> ()
