Based on the provided context and the gathered information, here is a comprehensive README for the project:
---
Guy's Sudoku Solver
Overview
Guy's Sudoku Solver is a C# application designed to solve Sudoku puzzles of various sizes efficiently. The solver uses advanced techniques such as constraint propagation, backtracking, and heuristics to handle even the most complex puzzles.
Features
•	Supports Various Sizes: Can solve standard 9x9 Sudoku puzzles as well as larger grids up to 25x25.
•	Advanced Heuristics: Utilizes techniques like Naked Singles, Hidden Singles, and constraint propagation to reduce the search space.
•	Debug Mode: Provides detailed output of the solving process for debugging and analysis.
•	Parallel Processing: Capable of solving multiple puzzles simultaneously using parallel processing.
Installation
1.	Clone the repository:
   git clone https://github.com/yourusername/guys-sudoku-solver.git
   cd guys-sudoku-solver
  	2.	Open the project in Visual Studio 2022:
•	Ensure you have .NET 8 installed.
3.	Build the project:
•	Use the build option in Visual Studio to compile the project.
Usage
Running the Solver
1.	Direct Input:
•	Run the application and choose the direct input method.
•	Enter a Sudoku puzzle as a single continuous string (e.g., 000006000059000008200008000045000000003000000006003054000325006000000000000000000).
•	The solver will attempt to solve the puzzle and display the result.
2.	File Input:
•	Run the application and choose the file input method.
•	Provide the path to a file containing Sudoku puzzles, each on a new line.
•	The solver will process each puzzle and display the results.
Example
Enter a Sudoku puzzle to solve (single continuous string, must be a square, 0 indicates empty space), or 'quit' to quit:
000006000059000008200008000045000000003000000006003054000325006000000000000000000
Solved Sudoku:
3 8 1 9 5 6 4 2 7
7 5 9 2 3 4 1 6 8
2 6 4 7 1 8 5 9 3
8 4 5 6 7 2 3 1 9
1 7 3 5 4 9 6 8 2
9 2 6 1 8 3 7 5 4
4 1 8 3 2 5 9 7 6
6 3 7 8 9 1 2 4 5
5 9 2 4 6 7 8 3 1
Completed in 0.0716753 seconds
Project Structure
•	Program.cs: Entry point of the application. Handles user input and manages the solving process.
•	Board.cs: Represents the Sudoku board and contains methods for initializing the board, updating constraints, and solving the puzzle.
•	Tile.cs: Represents a single tile on the Sudoku board, including its value and possible values.
•	Heuristics.cs: Contains heuristic methods used to optimize the solving process (e.g., Naked Singles, Hidden Singles).
Algorithms and Techniques
•	Constraint Propagation: Reduces the search space by eliminating impossible values for each cell based on current constraints.
•	Backtracking: Recursively explores possible values for each cell, backtracking when a contradiction is found.
•	Heuristics: Applies advanced techniques to further reduce the search space and improve solving efficiency.
Performance Considerations
•	Optimized Backtracking: Uses forward checking and heuristics to minimize the number of recursive calls.
•	Parallel Processing: Solves multiple puzzles simultaneously to take advantage of multi-core processors.

