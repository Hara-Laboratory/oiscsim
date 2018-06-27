# OiscSim
### One Instruction-Set Computer Simulator and Assembler
We have developed a toolchain for SubRISC, small and energy-efficient RISC processor. It has the limited number of simple instructions extended from Subtract and branch on NeGative with 4 operands (SNG4).

## Dependencies
- .NET Framework 4.0 or Mono

## Directory Hierarchy
- OiscSim
  - OiscSim.sln - Solution file
  - Interface - Solution Derectory
    - Interface.csproj - Project file
    - Program.cs - Entry point
    - Assemble - Contining source codes to parse and assemble
    - Execute - Contining source codes to simulate
    - bin
      - Debug - Binary directory
        - Interface.exe - Binary
        - Irony.dll - Dependency Library
        - sbrsc - Containing sample assembly files for SubRISC

## To assemble SubRISC programs
For Windows:
```
.\Interface.exe <input-path> -o <output-path> [-l <log-levels>]
-l: Specify log detail level.
    <log-levels> p = Progress, P = Detail Progress
	             i = Analysis Information, I = Detail Analysis Information
    * For example, specify "-l pPi".
```
For Mac/Linux:
```
mono Interface.exe <input-path> -o <output-path> [-l <log-levels>]
-l: Specify log detail level.
    <log-levels> p = Progress, P = Detail Progress
	             i = Analysis Information, I = Detail Analysis Information
    * For example, specify "-l pPi".
```

## To simulate and execute SubRISC programs
For Windows:
```
.\Interface.exe <input-path> -e [-l <log-levels>]
-l: Specify log detail level.
    <log-levels> p = Progress, P = Detail Progress
	               i = Analysis Information, I = Detail Analysis Information
				         e = Execution Step
    * For example, specify "-l pPie".
```
For Mac/Linux:
```
mono Interface.exe <input-path> -e [-l <log-levels>]
-l: Specify log detail level.
    <log-levels> p = Progress, P = Detail Progress
	              i = Analysis Information, I = Detail Analysis Information
				        e = Execution Step
    * For example, specify "-l pPie".
```

## Assembler Grammer
### Value types
- Integer type
  - Representing integer number and allowing binary, decimal, hexadecimal notation (specify prefixes '0b' and '0h' respectively for binary and hexadecimal).
  - `10, -10, 0x7FFF, 0b11011`
- Reference type
  - Refering some objects placed at memory (such as variable, block, and label), and representing the address.
  - `&INPUTS, INPUTS //Representing the head address of INPUTS`
  - `&INPUTS[10] //Representing the address of the 10th element in INPUTS`
- Symbol type
  - Refering the value defined as "symbol", and representing the defined content. (Something like arias)
  - `where .symbol HW_HALT_ADDRESS = -1;`
  - `HW_HALT_ADDRESS //It is understood as -1`
### Assembly file structure
Assembly file is composed of several sections and include statements.
To include sections in another files, include statements should be written like:
`#include "common.S"`
To place a section, it should be defined as:
`section <Name> { <Section-Contents> }`
### Section contents
- Contant variable
  - Representing read-only variable. There are two variations for single content and array contents
  - `.constant <name> = <value>;`
  - `.constant <name> [<length>] = { <value1>, <value2>, ... };`
- Variable
  - Representing read-write variable. 4 Variations are categorized by 2 aspects, whether to have initializer, and whether to be array variable.
  - `.variable <name> = <value>;`
  - `.variable <name>;`
  - `.variable <name> [<length>] = { <value1>, <value2>, ... };`
  - `.variable <name> [<length>];`
- Instruction
  - We can specify values or register entries as its operands. For the 2nd entry of register file, specify '$2'.
  - `[<label>: ] <mnemonic> <operand1>, <operand2>, ... ;`
- Symbol
  - Named alias targeting to a specific memory location.
  - `.symbol HW_HALT_ADDRESS := -1;`
- Block
  - Block is useful to make the code structural.
  - It can be named, and by doing so the name can be utilized as label pointing the head instruction in the block.
  - The scope of variable which is defined without initializer is valid only in the block where the variable is defined.
- Startup specifier
  - Specifier to specify the section starts from memory address 0.
  - `.startup`

## Acknowledgements
Irony - .NET Language Implementation Kit (refer https://github.com/IronyProject/Irony) Thanks for the great parser library.
  
## License
Copyright (c) 2018 Kaoru Saso.
Released under the MIT license.

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the 
"Software"), to deal in the Software without restriction, including 
without limitation the rights to use, copy, modify, merge, publish, 
distribute, sublicense, and/or sell copies of the Software, and to 
permit persons to whom the Software is furnished to do so, subject to 
the following conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
