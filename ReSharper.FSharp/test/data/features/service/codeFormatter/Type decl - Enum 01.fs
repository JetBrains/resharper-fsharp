namespace Namespace

type E1 =
    | A = 0
    | B = 1

type E2 =
    private
        | A = 0
        | B = 1

type E3 =
    private | A = 0
            | B = 1

type E4 = | A = 0
          | B = 1

type E5 = private
              | A = 1
              | B = 2

type E6 = private | A = 1
                  | B = 2
