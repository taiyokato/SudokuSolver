# SudokuSolver

by Taiyo Kato

## What It Does

SudokuSolver solves sudoku problems, but limited to grid size of n^2

Ex. 

1. n = 3, -> 3^2 = 9 -> Grid size of 9x9
2. n = 4, -> 4^2 = 16 -> Grid size of 16x16

## How To Use

### 1. Setup Grid size
When executed, it will first prompt the user the width of the grid.

The prompt will be as the following: 

    Input grid width: 
     Usable escape strings are available.\n The default grid size will be 9x9 if escape strings are used
     Usable escape strings for default are:
     {0, null, -, --}

The input value will be the FULL width of the grid, so if user inputs 9, the grid will be setup as 9x9

Since the default size of sudoku is 9x9, escape string values has been prepared. These escape strings all will yield the same grid size of 9x9.

As shown in the code, the usable escape strings are: {0, null, -, --}

If the input value is invalid, user will have to input a value again

### 2. Setup Grid

After the grid size is specified, 

User will be prompted with: 

    Grid size will be: [Height x Width]
    Input Each line
     Enter empty values as x
    Separate values with a space:

User will have to input the sudoku problem here. Input each horizontal line in order.

Must separate each value with an empty space to avoid value misunderstanding in 16x16 or greater grids

Example line for 9x9 grid: ```x x x 3 x x 5 x 9 1```


### 3. Run it

Run it.

## License
==

Uhh, any good license?
