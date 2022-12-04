using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Test
{
    // Виды алгоритмов для определения двумерного вейвлет-преобразования Хаара
    enum DecompositionTypes
    {
        // стандартное
        Standard,

        // пирамидальное
        Pyramidal
    }

    // Количество итераций, которые надо выполнить в вейвлет-преобразовании последовательности
    enum TransformIterationTypes
    {
        // одна итерация
        One,

        // все итерации
        All
    }

    class Program
    {
        // Метод, вычисляющий обратное вейвлет-преобразование Хаара последовательности 
        private static double[] OneDimensionalHaarReverseWaveletTransform(double[] array, int length)
        {
            // условие выхода из рекурсии (значит мы добрались до состояния последовательности
            // на последнем этапе прямого вейвлет-преобразования)
            if (length < 2)
            {
                return array;
            }

            // далее "раскручиваем" последовательность обратными операциями к операциям усреднения и детелизации
            array = OneDimensionalHaarReverseWaveletTransform(array, length / 2);

            var tmp = new double[array.Length];
            Array.Copy(array, tmp, tmp.Length);
            for (var i = 0; i < length / 2; i++)
            {
                array[2 * i] = tmp[i] + tmp[i + length / 2];
                array[2 * i + 1] = tmp[i] - tmp[i + length / 2];
            }

            return array;
        }

        // Метод, вычисляющий вейвлет-преобразование Хаара последовательности 
        private static double[] OneDimensionalHaarWaveletTransform(double[] array,
            TransformIterationTypes iterationType = TransformIterationTypes.All)
        {
            var l = array.Length;
            var constraint = iterationType is TransformIterationTypes.All ? 2 : l;

            // применяем операции усреднения и детализации до тех пор, пока останется лишь один усреднённый элемент,
            // а все остальные - детализированные (в случае, если нужно выполнить все итерации)
            while (l >= constraint)
            {
                var tmp = new double[array.Length];
                Array.Copy(array, tmp, tmp.Length);
                for (var i = 0; i < l / 2; i++)
                {
                    // усреднение
                    array[i] = (tmp[2 * i] + tmp[2 * i + 1]) / 2;

                    // детализация
                    array[i + l / 2] = (tmp[2 * i] - tmp[2 * i + 1]) / 2;
                }

                l /= 2;
            }

            return array;
        }

        // Метод, осуществляющий двумерное вейвлет-преобразование Хаара на 2n x 2n матрице
        // decompositionType - вид алгоритма, с помощью которого нужно выполнить преобразование
        private static double[,] TwoDimensionalHaarWaveletTransform(double[,] matrix,
            DecompositionTypes decompositionType)
        {
            var n = matrix.GetUpperBound(0) + 1;

            switch (decompositionType)
            {
                case DecompositionTypes.Standard:
                {
                    // применяем вейвлет-преобразование к каждой строке матрицы
                    for (var i = 0; i < n; i++)
                    {
                        var array = new double[n];
                        for (var j = 0; j < n; j++)
                        {
                            array[j] = matrix[i, j];
                        }

                        array = OneDimensionalHaarWaveletTransform(array);
                        for (var j = 0; j < n; j++)
                        {
                            matrix[i, j] = array[j];
                        }
                    }

                    // применяем вейвлет-преобразование к каждому столбцу матрицы
                    for (var j = 0; j < n; j++)
                    {
                        var array = new double[n];
                        for (var i = 0; i < n; i++)
                        {
                            array[i] = matrix[i, j];
                        }

                        array = OneDimensionalHaarWaveletTransform(array);
                        for (var i = 0; i < n; i++)
                        {
                            matrix[i, j] = array[i];
                        }
                    }

                    break;
                }

                case DecompositionTypes.Pyramidal:
                {
                    while (n >= 2)
                    {
                        // применяем одну итерацию вейвлет-преобразования к каждой строке матрицы
                        for (var i = 0; i < n; i++)
                        {
                            var array = new double[n];
                            for (var j = 0; j < n; j++)
                            {
                                array[j] = matrix[i, j];
                            }

                            array = OneDimensionalHaarWaveletTransform(array, TransformIterationTypes.One);
                            for (var j = 0; j < n; j++)
                            {
                                matrix[i, j] = array[j];
                            }
                        }

                        // применяем одну итерацию вейвлет-преобразования к каждому столбцу матрицы
                        for (var j = 0; j < n; j++)
                        {
                            var array = new double[n];
                            for (var i = 0; i < n; i++)
                            {
                                array[i] = matrix[i, j];
                            }

                            array = OneDimensionalHaarWaveletTransform(array, TransformIterationTypes.One);
                            for (var i = 0; i < n; i++)
                            {
                                matrix[i, j] = array[i];
                            }
                        }

                        n /= 2;
                    }

                    break;
                }
            }

            return matrix;
        }

        // Метод, выполняющий вейвлет-сжатие с настраиваемым количеством отбрасываемых элементов (numberToDiscard)
        private static double[] WaveletCompression(double[] array, int numberItemsToDiscard)
        {
            if (numberItemsToDiscard > array.Length)
            {
                throw new ArgumentException("Невозможно отбросить элементов больше, чем размерность массива");
            }

            // применияем прямое вейвлет-преобразование к последовательности
            array = OneDimensionalHaarWaveletTransform(array);

            // сортируем полученную последовательность, сравнивая числа по модулю, в порядке убывания
            Array.Sort(array, (d, d1) =>
            {
                var result = 0;
                if (Math.Abs(d) < Math.Abs(d1))
                {
                    result = 1;
                }
                else if (Math.Abs(d) > Math.Abs(d1))
                {
                    result = -1;
                }

                return result;
            });

            // обнуляем указанное количество элементов в последовательности
            for (var i = array.Length - numberItemsToDiscard; i < array.Length; i++)
            {
                array[i] = 0;
            }

            //применяем к последовательности, полученной после обнуления, обратное вейвлет-преобразование
            array = OneDimensionalHaarReverseWaveletTransform(array, array.Length);
            return array;
        }

        private static void Main(string[] args)
        {
            // Задание 1
            var matrix1 = new double[,]
            {
                { 20, 12, 13, 11 },
                { 6, 2, 8, 12 },
                { 15, 17, 14, 8 },
                { 10, 6, 4, 10 }
            };
            var matrix2 = new double[,]
            {
                { 20, 12, 13, 11 },
                { 6, 2, 8, 12 },
                { 15, 17, 14, 8 },
                { 10, 6, 4, 10 }
            };

            var resultStandard = TwoDimensionalHaarWaveletTransform(matrix1, DecompositionTypes.Standard);
            ShowMatrix(resultStandard);
            Console.WriteLine();
            var resultPyramidal = TwoDimensionalHaarWaveletTransform(matrix2, DecompositionTypes.Pyramidal);
            ShowMatrix(resultPyramidal);
            Console.WriteLine();

            // Задание 3
            var array = new double[] { 20, 12, 13, 11, 6, 2, 8, 12 };
            array = WaveletCompression(array, 3);
            ShowArray(array);
            Console.ReadLine();
        }

        private static void ShowArray(double[] array)
        {
            foreach (var number in array)
            {
                Console.Write($"{number}\t");
            }

            Console.WriteLine();
        }

        private static void ShowMatrix(double[,] matrix)
        {
            var n = matrix.GetUpperBound(0) + 1;
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    Console.Write($"{matrix[i, j]}\t");
                }

                Console.WriteLine();
            }
        }
    }
}