namespace Namespace

type E1 =
    private
        | A = 0
        | B = 1

type E2 =
    private | A = 0
            | B = 1

type E3 = private
              | A = 1
              | B = 2

type E4 = private | A = 1
                  | B = 2

type E6 = private | A = 0 | B = 1

type E7 = private A = 0 | B = 1

type E8 =
    private | A = 0 | B = 1
