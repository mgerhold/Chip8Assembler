# CHIP-8 Assembler
This project is heavily influenced by [https://github.com/craigthomas/Chip8Assembler](https://github.com/craigthomas/Chip8Assembler). It is a little (one file) C# program that can compile assembly language into bytecode for the CHIP-8 (to run the bytecode, see my [CHIP-8 emulator over here](https://github.com/mgerhold/Chip8Emulator)). Take a look at the file `pong.asm` to see an example of a working program.
## Mnemonics table
Please refer to [https://github.com/craigthomas/Chip8Assembler#chip-8-mnemonics](https://github.com/craigthomas/Chip8Assembler#chip-8-mnemonics) for a mnemonic table that is also compatible to this emulator.
## Labels
Labels end with a colon (`:`). Example:
```
resettimer:		# this is a label
	LOAD	V0, $1
	LOADD	V0
	RTS
```
## Building
This is a Visual Studio solution without external dependencies so it should work out of the box (on Windows at least).
## Usage
Just pass the filename of the file containing the assembly code as a parameter to the executable, e.g. `Chip8Assembler.exe pong.asm`. The output file will have the same filename but with a `.ch8` extension.