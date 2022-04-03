public static class JitterPattern {
  public static int[][] Get(int size) {
    return size switch {
      16 =>
      new int[][] { new int[] { 0, 0 }, new int[] { 8, 0 }, new int[] { 0, 8 }, new int[] { 8, 8 }, new int[] { 4, 0 }, new int[] { 12, 0 }, new int[] { 4, 8 }, new int[] { 12, 8 }, new int[] { 0, 4 }, new int[] { 8, 4 }, new int[] { 0, 12 }, new int[] { 8, 12 }, new int[] { 4, 4 }, new int[] { 12, 4 }, new int[] { 4, 12 }, new int[] { 12, 12 }, new int[] { 2, 0 }, new int[] { 10, 0 }, new int[] { 2, 8 }, new int[] { 10, 8 }, new int[] { 6, 0 }, new int[] { 14, 0 }, new int[] { 6, 8 }, new int[] { 14, 8 }, new int[] { 2, 4 }, new int[] { 10, 4 }, new int[] { 2, 12 }, new int[] { 10, 12 }, new int[] { 6, 4 }, new int[] { 14, 4 }, new int[] { 6, 12 }, new int[] { 14, 12 }, new int[] { 0, 2 }, new int[] { 8, 2 }, new int[] { 0, 10 }, new int[] { 8, 10 }, new int[] { 4, 2 }, new int[] { 12, 2 }, new int[] { 4, 10 }, new int[] { 12, 10 }, new int[] { 0, 6 }, new int[] { 8, 6 }, new int[] { 0, 14 }, new int[] { 8, 14 }, new int[] { 4, 6 }, new int[] { 12, 6 }, new int[] { 4, 14 }, new int[] { 12, 14 }, new int[] { 2, 2 }, new int[] { 10, 2 }, new int[] { 2, 10 }, new int[] { 10, 10 }, new int[] { 6, 2 }, new int[] { 14, 2 }, new int[] { 6, 10 }, new int[] { 14, 10 }, new int[] { 2, 6 }, new int[] { 10, 6 }, new int[] { 2, 14 }, new int[] { 10, 14 }, new int[] { 6, 6 }, new int[] { 14, 6 }, new int[] { 6, 14 }, new int[] { 14, 14 }, new int[] { 1, 0 }, new int[] { 9, 0 }, new int[] { 1, 8 }, new int[] { 9, 8 }, new int[] { 5, 0 }, new int[] { 13, 0 }, new int[] { 5, 8 }, new int[] { 13, 8 }, new int[] { 1, 4 }, new int[] { 9, 4 }, new int[] { 1, 12 }, new int[] { 9, 12 }, new int[] { 5, 4 }, new int[] { 13, 4 }, new int[] { 5, 12 }, new int[] { 13, 12 }, new int[] { 3, 0 }, new int[] { 11, 0 }, new int[] { 3, 8 }, new int[] { 11, 8 }, new int[] { 7, 0 }, new int[] { 15, 0 }, new int[] { 7, 8 }, new int[] { 15, 8 }, new int[] { 3, 4 }, new int[] { 11, 4 }, new int[] { 3, 12 }, new int[] { 11, 12 }, new int[] { 7, 4 }, new int[] { 15, 4 }, new int[] { 7, 12 }, new int[] { 15, 12 }, new int[] { 1, 2 }, new int[] { 9, 2 }, new int[] { 1, 10 }, new int[] { 9, 10 }, new int[] { 5, 2 }, new int[] { 13, 2 }, new int[] { 5, 10 }, new int[] { 13, 10 }, new int[] { 1, 6 }, new int[] { 9, 6 }, new int[] { 1, 14 }, new int[] { 9, 14 }, new int[] { 5, 6 }, new int[] { 13, 6 }, new int[] { 5, 14 }, new int[] { 13, 14 }, new int[] { 3, 2 }, new int[] { 11, 2 }, new int[] { 3, 10 }, new int[] { 11, 10 }, new int[] { 7, 2 }, new int[] { 15, 2 }, new int[] { 7, 10 }, new int[] { 15, 10 }, new int[] { 3, 6 }, new int[] { 11, 6 }, new int[] { 3, 14 }, new int[] { 11, 14 }, new int[] { 7, 6 }, new int[] { 15, 6 }, new int[] { 7, 14 }, new int[] { 15, 14 }, new int[] { 1, 1 }, new int[] { 9, 1 }, new int[] { 1, 9 }, new int[] { 9, 9 }, new int[] { 5, 1 }, new int[] { 13, 1 }, new int[] { 5, 9 }, new int[] { 13, 9 }, new int[] { 1, 5 }, new int[] { 9, 5 }, new int[] { 1, 13 }, new int[] { 9, 13 }, new int[] { 5, 5 }, new int[] { 13, 5 }, new int[] { 5, 13 }, new int[] { 13, 13 }, new int[] { 3, 1 }, new int[] { 11, 1 }, new int[] { 3, 9 }, new int[] { 11, 9 }, new int[] { 7, 1 }, new int[] { 15, 1 }, new int[] { 7, 9 }, new int[] { 15, 9 }, new int[] { 3, 5 }, new int[] { 11, 5 }, new int[] { 3, 13 }, new int[] { 11, 13 }, new int[] { 7, 5 }, new int[] { 15, 5 }, new int[] { 7, 13 }, new int[] { 15, 13 }, new int[] { 1, 3 }, new int[] { 9, 3 }, new int[] { 1, 11 }, new int[] { 9, 11 }, new int[] { 5, 3 }, new int[] { 13, 3 }, new int[] { 5, 11 }, new int[] { 13, 11 }, new int[] { 1, 7 }, new int[] { 9, 7 }, new int[] { 1, 15 }, new int[] { 9, 15 }, new int[] { 5, 7 }, new int[] { 13, 7 }, new int[] { 5, 15 }, new int[] { 13, 15 }, new int[] { 3, 3 }, new int[] { 11, 3 }, new int[] { 3, 11 }, new int[] { 11, 11 }, new int[] { 7, 3 }, new int[] { 15, 3 }, new int[] { 7, 11 }, new int[] { 15, 11 }, new int[] { 3, 7 }, new int[] { 11, 7 }, new int[] { 3, 15 }, new int[] { 11, 15 }, new int[] { 7, 7 }, new int[] { 15, 7 }, new int[] { 7, 15 }, new int[] { 15, 15 }, new int[] { 0, 1 }, new int[] { 8, 1 }, new int[] { 0, 9 }, new int[] { 8, 9 }, new int[] { 4, 1 }, new int[] { 12, 1 }, new int[] { 4, 9 }, new int[] { 12, 9 }, new int[] { 0, 5 }, new int[] { 8, 5 }, new int[] { 0, 13 }, new int[] { 8, 13 }, new int[] { 4, 5 }, new int[] { 12, 5 }, new int[] { 4, 13 }, new int[] { 12, 13 }, new int[] { 2, 1 }, new int[] { 10, 1 }, new int[] { 2, 9 }, new int[] { 10, 9 }, new int[] { 6, 1 }, new int[] { 14, 1 }, new int[] { 6, 9 }, new int[] { 14, 9 }, new int[] { 2, 5 }, new int[] { 10, 5 }, new int[] { 2, 13 }, new int[] { 10, 13 }, new int[] { 6, 5 }, new int[] { 14, 5 }, new int[] { 6, 13 }, new int[] { 14, 13 }, new int[] { 0, 3 }, new int[] { 8, 3 }, new int[] { 0, 11 }, new int[] { 8, 11 }, new int[] { 4, 3 }, new int[] { 12, 3 }, new int[] { 4, 11 }, new int[] { 12, 11 }, new int[] { 0, 7 }, new int[] { 8, 7 }, new int[] { 0, 15 }, new int[] { 8, 15 }, new int[] { 4, 7 }, new int[] { 12, 7 }, new int[] { 4, 15 }, new int[] { 12, 15 }, new int[] { 2, 3 }, new int[] { 10, 3 }, new int[] { 2, 11 }, new int[] { 10, 11 }, new int[] { 6, 3 }, new int[] { 14, 3 }, new int[] { 6, 11 }, new int[] { 14, 11 }, new int[] { 2, 7 }, new int[] { 10, 7 }, new int[] { 2, 15 }, new int[] { 10, 15 }, new int[] { 6, 7 }, new int[] { 14, 7 }, new int[] { 6, 15 }, new int[] { 14, 15 }, },
        8 =>
        new int[][] { new int[] { 0, 0 }, new int[] { 4, 0 }, new int[] { 0, 4 }, new int[] { 4, 4 }, new int[] { 2, 0 }, new int[] { 6, 0 }, new int[] { 2, 4 }, new int[] { 6, 4 }, new int[] { 0, 2 }, new int[] { 4, 2 }, new int[] { 0, 6 }, new int[] { 4, 6 }, new int[] { 2, 2 }, new int[] { 6, 2 }, new int[] { 2, 6 }, new int[] { 6, 6 }, new int[] { 1, 0 }, new int[] { 5, 0 }, new int[] { 1, 4 }, new int[] { 5, 4 }, new int[] { 3, 0 }, new int[] { 7, 0 }, new int[] { 3, 4 }, new int[] { 7, 4 }, new int[] { 1, 2 }, new int[] { 5, 2 }, new int[] { 1, 6 }, new int[] { 5, 6 }, new int[] { 3, 2 }, new int[] { 7, 2 }, new int[] { 3, 6 }, new int[] { 7, 6 }, new int[] { 1, 1 }, new int[] { 5, 1 }, new int[] { 1, 5 }, new int[] { 5, 5 }, new int[] { 3, 1 }, new int[] { 7, 1 }, new int[] { 3, 5 }, new int[] { 7, 5 }, new int[] { 1, 3 }, new int[] { 5, 3 }, new int[] { 1, 7 }, new int[] { 5, 7 }, new int[] { 3, 3 }, new int[] { 7, 3 }, new int[] { 3, 7 }, new int[] { 7, 7 }, new int[] { 0, 1 }, new int[] { 4, 1 }, new int[] { 0, 5 }, new int[] { 4, 5 }, new int[] { 2, 1 }, new int[] { 6, 1 }, new int[] { 2, 5 }, new int[] { 6, 5 }, new int[] { 0, 3 }, new int[] { 4, 3 }, new int[] { 0, 7 }, new int[] { 4, 7 }, new int[] { 2, 3 }, new int[] { 6, 3 }, new int[] { 2, 7 }, new int[] { 6, 7 }, },
        4 =>
        new int[][] { new int[] { 0, 0 }, new int[] { 2, 0 }, new int[] { 0, 2 }, new int[] { 2, 2 }, new int[] { 1, 0 }, new int[] { 3, 0 }, new int[] { 1, 2 }, new int[] { 3, 2 }, new int[] { 1, 1 }, new int[] { 3, 1 }, new int[] { 1, 3 }, new int[] { 3, 3 }, new int[] { 0, 1 }, new int[] { 2, 1 }, new int[] { 0, 3 }, new int[] { 2, 3 }, },
        2 =>
        new int[][] { new int[] { 0, 0 }, new int[] { 1, 0 }, new int[] { 1, 1 }, new int[] { 0, 1 }, },
        1 =>
        new int[][] { new int[] { 0, 0 }, },
        _ =>
        throw new System.Exception($"invalid jitter Size: size"),
    };
  }
}
