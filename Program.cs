using System;

namespace SudokuGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            bool repeat = true;

            while (repeat)
            {
                int n = 0;
                string sudokuType;
                string difficulty;

                // Pedir tipo de Sudoku y validar entrada
                do
                {
                    Console.WriteLine("¿Qué tipo de Sudoku desea generar? (Clasico/Submatrices)");
                    sudokuType = Console.ReadLine().ToLower();
                } while (sudokuType != "clasico" && sudokuType != "submatrices");

                // Pedir tamaño de Sudoku y validar entrada
                do
                {
                    Console.WriteLine("Ingrese el tamaño del Sudoku (2, 3 o 4):");
                } while (!int.TryParse(Console.ReadLine(), out n) || (n != 2 && n != 3 && n != 4));

                // Pedir dificultad y validar entrada
                do
                {
                    Console.WriteLine("¿Qué dificultad desea? (facil/medio/dificil)");
                    difficulty = Console.ReadLine().ToLower();
                } while (difficulty != "facil" && difficulty != "medio" && difficulty != "dificil");

                // Generar Sudoku
                int[,] sudoku;
                if (sudokuType == "clasico")
                {
                    sudoku = ClassicSudoku.Generate(n);
                }
                else
                {
                    sudoku = SubgridSudoku.Generate(n);
                }

                // Rectificar Sudoku para asegurar solución y ajustar dificultad
                SudokuRectifier.RectifySudoku(ref sudoku, difficulty);

                // Imprimir Sudoku con dificultad ajustada
                Console.WriteLine("Sudoku con dificultad ajustada:");
                SudokuPrinter.PrintSudoku(sudoku);

                // Preguntar al usuario si desea resolver el Sudoku
                Console.WriteLine("¿Desea resolver el Sudoku? (s/n)");
                string response = Console.ReadLine().ToLower();
                if (response == "s" || response == "si")
                {
                    // Resolver Sudoku
                    if (SudokuSolver.SodokuIntelligentSolver(sudoku))
                    {
                        Console.WriteLine("Sudoku resuelto:");
                        SudokuPrinter.PrintSudoku(sudoku);
                    }
                    else
                    {
                        Console.WriteLine("No se pudo resolver el Sudoku.");
                    }
                }

                // Esperar entrada para repetir o salir
                Console.WriteLine("Presione cualquier tecla para generar otro Sudoku o ESC para salir.");
                var key = Console.ReadKey(true).Key;
                repeat = key != ConsoleKey.Escape;
            }
        }
    }

    static class ClassicSudoku
    {
        public static int[,] Generate(int n)
        {
            int size = n;
            int[,] sudoku = new int[size, size];
            int[] availableNumbers = new int[size * size];

            // Llenar el arreglo con los números del 1 al nxn
            for (int i = 0; i < availableNumbers.Length; i++)
            {
                availableNumbers[i] = i + 1;
            }

            FillSudoku(sudoku, availableNumbers);
            return sudoku;
        }

        private static void FillSudoku(int[,] sudoku, int[] availableNumbers)
        {
            int size = sudoku.GetLength(0);
            Random random = new Random();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    // Elegir un número aleatorio de los números disponibles
                    int index = random.Next(0, availableNumbers.Length);
                    int number = availableNumbers[index];

                    // Colocar el número en la matriz
                    sudoku[i, j] = number;

                    // Eliminar el número usado de los disponibles
                    availableNumbers[index] = availableNumbers[availableNumbers.Length - 1];
                    Array.Resize(ref availableNumbers, availableNumbers.Length - 1);
                }
            }
        }
    }

    static class SubgridSudoku
    {
        public static int[,] Generate(int n)
        {
            int size = n * n;
            int[,] sudoku = new int[size, size];

            FillSudoku(sudoku, n);
            return sudoku;
        }

        private static void FillSudoku(int[,] sudoku, int n)
        {
            int size = sudoku.GetLength(0);
            Random random = new Random();

            // Llenar la diagonal principal con números aleatorios sin repetir
            for (int i = 0; i < size; i += n)
            {
                FillDiagonalBlock(sudoku, i, i, n);
            }

            // Resolver Sudoku generado
            SodokuGenerator.SodokuFirstGenerator(sudoku);
        }

        private static void FillDiagonalBlock(int[,] sudoku, int row, int col, int n)
        {
            int size = sudoku.GetLength(0);
            Random random = new Random();

            for (int i = 0; i < n; i++)
            {
                int num;
                for (int j = 0; j < n; j++)
                {
                    do
                    {
                        num = random.Next(1, size + 1);
                    } while (!SudokuValidator.IsSafe(sudoku, row + i, col + j, num));
                    sudoku[row + i, col + j] = num;
                }
            }
        }
    }

    static class SodokuGenerator
    {
        public static bool SodokuFirstGenerator(int[,] sudoku)
        {
            int size = sudoku.GetLength(0);
            int row = -1;
            int col = -1;
            bool isEmpty = true;

            // Buscar una celda vacía
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (sudoku[i, j] == 0)
                    {
                        row = i;
                        col = j;
                        isEmpty = false;
                        break;
                    }
                }
                if (!isEmpty)
                {
                    break;
                }
            }

            // No se encontró ninguna celda vacía, Sudoku está resuelto
            if (isEmpty)
            {
                return true;
            }

            // Intentar colocar números del 1 al tamaño del Sudoku en la celda vacía
            for (int num = 1; num <= size; num++)
            {
                if (SudokuValidator.IsSafe(sudoku, row, col, num))
                {
                    sudoku[row, col] = num;

                    // Resolver recursivamente las celdas restantes
                    if (SodokuFirstGenerator(sudoku))
                    {
                        return true;
                    }

                    // Si colocar num en sudoku[row, col] no conduce a una solución, retroceder
                    sudoku[row, col] = 0;
                }
            }

            // No se puede colocar ningún número en esta celda, retroceder
            return false;
        }
    }

    static class SudokuRectifier
    {
        private static Random random = new Random();

        public static void RectifySudoku(ref int[,] sudoku, string difficulty)
        {
            int size = sudoku.GetLength(0);
            double removalPercentage = 0;

            // Determinar el porcentaje de celdas a eliminar según la dificultad
            switch (difficulty)
            {
                case "facil":
                    removalPercentage = 0.4;
                    break;
                case "medio":
                    removalPercentage = 0.6;
                    break;
                case "dificil":
                    removalPercentage = 0.8;
                    break;
            }

            int cellsToRemove = (int)(size * size * removalPercentage);

            // Eliminar celdas aleatoriamente
            while (cellsToRemove > 0)
            {
                int row = random.Next(0, size);
                int col = random.Next(0, size);

                if (sudoku[row, col] != 0)
                {
                    sudoku[row, col] = 0;
                    cellsToRemove--;
                }
            }
        }
    }

    static class SudokuValidator
    {
        public static bool IsSafe(int[,] sudoku, int row, int col, int num)
        {
            int size = sudoku.GetLength(0);
            // Verificar la fila y columna
            for (int i = 0; i < size; i++)
            {
                if (sudoku[row, i] == num || sudoku[i, col] == num)
                {
                    return false;
                }
            }
            // Verificar la submatriz n x n
            int subgridSize = (int)Math.Sqrt(size);
            int startRow = row - row % subgridSize;
            int startCol = col - col % subgridSize;
            for (int i = 0; i < subgridSize; i++)
            {
                for (int j = 0; j < subgridSize; j++)
                {
                    if (sudoku[i + startRow, j + startCol] == num)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    static class SudokuPrinter
    {
        public static void PrintSudoku(int[,] sudoku)
        {
            int size = sudoku.GetLength(0);
            for (int i = 0; i < size; i++)
            {
                if (i != 0 && i % Math.Sqrt(size) == 0)
                {
                    Console.WriteLine(new string('-', size * 2 + (int)Math.Sqrt(size) - 1));
                }
                for (int j = 0; j < size; j++)
                {
                    if (j != 0 && j % Math.Sqrt(size) == 0)
                    {
                        Console.Write("| ");
                    }
                    Console.Write(sudoku[i, j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }

    static class SudokuSolver
    {
        public static bool SodokuIntelligentSolver(int[,] sudoku)
        {
            int size = sudoku.GetLength(0);
            return SudokuSolver.SolveSudokuHelper(sudoku, size);
        }

        private static bool SolveSudokuHelper(int[,] sudoku, int size)
        {
            int row = -1;
            int col = -1;
            bool isEmpty = true;

            // Buscar una celda vacía
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (sudoku[i, j] == 0)
                    {
                        row = i;
                        col = j;
                        isEmpty = false;
                        break;
                    }
                }
                if (!isEmpty)
                {
                    break;
                }
            }

            // No se encontró ninguna celda vacía, Sudoku está resuelto
            if (isEmpty)
            {
                return true;
            }

            // Intentar colocar números del 1 al tamaño del Sudoku en la celda vacía
            for (int num = 1; num <= size; num++)
            {
                if (IsSafe(sudoku, row, col, num))
                {
                    sudoku[row, col] = num;

                    // Resolver recursivamente las celdas restantes
                    if (SolveSudokuHelper(sudoku, size))
                    {
                        return true;
                    }

                    // Si colocar num en sudoku[row, col] no conduce a una solución, retroceder
                    sudoku[row, col] = 0;
                }
            }

            // No se puede colocar ningún número en esta celda, retroceder
            return false;
        }

        private static bool IsSafe(int[,] sudoku, int row, int col, int num)
        {
            int size = sudoku.GetLength(0);

            // Verificar la fila y columna
            for (int i = 0; i < size; i++)
            {
                if (sudoku[row, i] == num || sudoku[i, col] == num)
                {
                    return false;
                }
            }

            // Verificar la submatriz n x n
            int subgridSize = (int)Math.Sqrt(size);
            int startRow = row - row % subgridSize;
            int startCol = col - col % subgridSize;

            for (int i = 0; i < subgridSize; i++)
            {
                for (int j = 0; j < subgridSize; j++)
                {
                    if (sudoku[i + startRow, j + startCol] == num)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
