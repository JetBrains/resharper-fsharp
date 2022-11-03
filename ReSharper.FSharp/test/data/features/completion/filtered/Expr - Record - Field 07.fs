module Module

type Test = {
    FirstProp: int
    SecondProp: string
}

type ITest =
    abstract member Test: Test

type ABC =
    interface ITest with
        member _.Test = {
            FirstProp = 1
            S{caret}
        }
