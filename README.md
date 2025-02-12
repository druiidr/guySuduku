# Guy's Sudoku Solver
A High-Performance Sudoku Solver in C#
## Overview
Guy's Sudoku Solver is a powerful C# application designed to efficiently solve Sudoku puzzles of various sizes. It leverages advanced solving techniques such as constraint propagation, backtracking, and heuristics, making it capable of handling even the most complex puzzles.

## Features
✔ Supports Various Sizes – Solves standard 9×9 Sudoku puzzles as well as larger grids up to 25×25

✔ Advanced Heuristics – Uses techniques like Naked Singles, Hidden Singles, and constraint propagation

✔ Debug Mode – Provides detailed output for debugging and analysis

✔ Parallel Processing – Can solve multiple puzzles simultaneously using multi-threading

## Installation
1️⃣ Clone the Repository
bash
Copy
Edit
git clone https://github.com/druiidr/guys_sudoku_solver.git
cd guys-sudoku-solver

2️⃣ Open in Visual Studio 2022
Ensure you have .NET 8 installed before proceeding.

3️⃣ Build the Project
Use the Build option in Visual Studio to compile the project.

## Usage
### Running the Solver
#### 1. Direct Input Mode
Run the application and select Direct Input mode.
Enter a Sudoku puzzle as a single continuous string, where 0 represents an empty cell.

#### Example:

```bash
000006000059000008200008000045000000003000000006003054000325006000000000000000000
```
The solver will process the puzzle and display the completed grid.

#### 2. File Input Mode
Run the application and select File Input mode.
Provide the path to a file containing Sudoku puzzles, each on a new line. make sure you input the correct and full file path!!
The solver will process each puzzle and print the results.

#### Example:

```bash
C:\Users\ASUS\Desktop\puzzleOverload.txt
```
### 📝 Example Usage
```bash
Enter a Sudoku puzzle to solve (single continuous string, must be a square, 0 indicates empty space), or 'quit' to quit:


000006000059000008200008000045000000003000000006003054000325006000000000000000000
```

```c#
 Solved Sudoku:

3  8  1  9  5  6  4  2  7

7  5  9  2  3  4  1  6  8

2  6  4  7  1  8  5  9  3

8  4  5  6  7  2  3  1  9

1  7  3  5  4  9  6  8  2

9  2  6  1  8  3  7  5  4

4  1  8  3  2  5  9  7  6

6  3  7  8  9  1  2  4  5

5  9  2  4  6  7  8  3  1

🔹 Completed in 0.0716753 seconds.
```
## Project Structure
### Program.cs – 
Entry point of the application

### InputHandling.cs -
handles user input and manages the solving process
### Board.cs –
 Represents the Sudoku board, with methods for initialization, constraint handling, and solving
### Tile.cs – 
Represents a single tile on the board, storing values and possible candidates
### Heuristics.cs –
 Implements Naked Singles, Hidden Singles, and other advanced heuristics

## Algorithms and Techniques

🔹 Constraint Propagation – Reduces the search space by eliminating impossible values for each cell

🔹 Backtracking – Recursively explores possible values, backtracking when a contradiction is found

🔹 Heuristics – Applies smart techniques to optimize solving efficiency

## Performance Considerations
### Optimized Backtracking –
 Uses forward checking and heuristics to minimize recursive calls
### Parallel Processing –
 Solves multiple puzzles simultaneously utilizing multi-core processors

## License

This project is open-source under the MIT License.
