"""
This script generates csharp code with jittered sampling offsets.

usage: samples.py [grid size]
"""

import sys


def main():
    if len(sys.argv) != 2:
        print(
            "usage: samples.py [grid size]"
        )
        return

    size = int(sys.argv[1])

    def gen(n: int) -> list[tuple[int, int]]:
        assert(
            n & (n - 1) == False
        )
        assert(n > 1)

        if n == 2:
            return [
                (0, 0),
                (1, 0),
                (1, 1),
                (0, 1)
            ]

        return [
            (
                pos[0] + offset_x * n // 2,
                pos[1] + offset_y * n // 2
            )
            for pos in gen(n // 2) for offset_y in [0, 1] for offset_x in [0, 1]
        ]

    list_offsets = gen(size)

    print("int[][] offsets = new int[][] { ", end="")

    for offset in list_offsets:
        print(
            f"new int[] {'{'} {offset[0]}, {offset[1]} {'}'},",
            end=" "
        )
    print("};")


if __name__ == '__main__':
    main()
