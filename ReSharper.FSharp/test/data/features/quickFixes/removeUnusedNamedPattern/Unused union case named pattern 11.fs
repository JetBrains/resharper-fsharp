type U =
    | A of i: int
    | B

match B with
| A(i = i{caret}) -> 1
| B -> 2
