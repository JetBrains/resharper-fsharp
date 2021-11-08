//${OCCURRENCE0:Bind 'U' computation with let!}
//${OCCURRENCE1:Deconstruct 'A' union case}

module Module =
    type U =
        | A of int * float

async {
    {selstart}async { return Module.A(1, 1.0) }{selend}{caret}
    return 1
}
