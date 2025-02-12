using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using guy_s_sudoku;

namespace TestinSudoku
{
    [TestFixture]
    public class SudokuTests
    {
        private static readonly string TestingPuzzlesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestinSudoku", "TestingPuzzles");
        /// <summary>
        /// run the tests from the testPuzzles file
        /// </summary>
        [Test]
        public void RunTestingPuzzles()
        {
            var files = Directory.GetFiles(TestingPuzzlesPath, "*.txt");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var shouldFail = fileName.Contains("illegals");
                var shouldWork = !shouldFail && !fileName.Contains("massMonsters");

                var lines = File.ReadAllLines(file);
                var input = string.Join("", lines);
                var size = (int)Math.Sqrt(lines.Length);

                var board = new Board(input, size); // Ensure Board class exists

                var stopwatch = Stopwatch.StartNew();
                var result = board.Solve();
                stopwatch.Stop();

                if (shouldWork)
                {
                    Assert.That(result, Is.True, $"{fileName} should work but failed.");
                }
                else if (shouldFail)
                {
                    Assert.That(result, Is.False, $"{fileName} should fail but passed.");
                }

                if (!fileName.Contains("massMonsters"))
                {
                    Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(1), $"{fileName} took too long to solve.");
                }

                TestContext.WriteLine($"{fileName} was tested.");
            }
        }
    }
}
