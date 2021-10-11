# An emulator for the Zilog Z80 processor in C# #

This repo contains the code for a Zilog Z80 processor emulator library.

<br>

The Z80Emulator solution consists of the following projects:

- **Z80EmulatorConsoleTestApp**: A simple console application that demonstrates the functionality of the library.
- **Z80EmulatorLib**: The code for the library itself.
- **Z80Emulator.Tests**: An extensive set of tests.

<br>

### Prerequisites

- [.NET Core 3.1 SDK](https://www.microsoft.com/net/download/core)
  
<br>

### Why was this created?

For the very same reason I also wrote a 6502 processor emulator... to relive a little of my youth, but mainly "just for fun" :-)  
  
<br>

### What isn't supported?

This library does not currently support any of the 'undocumented' Z80 instructions or flags.  
It does very little in the way of interrupt handling (and by that I mean almost nothing!).  
  
<br>

### Usage

The included **Z80EmulatorConsoleTestApp** project demonstrates how to use the emulator. This application has a couple of simple Z80 code examples that it runs through the emulator.

From a developer's point of view, the emulator is used as follows:
1. Create an instance of the `Machine` class, supplying an instance of a class that implements the `IPort` interface (that performs input/output used for the Z80 IN/OUT instructions) - NOTE: The emulator has its own 'dummy' implementation of this interface (that is used if no `IPort` implementation is supplied).
2. Load binary executable data into the machine by calling the `LoadExecutableData` method, supplying a byte array containing the binary data and the address at which the data should be loaded in memory.
3. Load any other binary data into the machine [if required] by calling the `LoadData` method, supplying a byte array containing the binary data and the address at which the data should be loaded in memory. The final parameter passed to `LoadData` should be `false` to avoid clearing all memory before loading the data (otherwise any previously loaded executable data will be lost).
4. Set the initial state of the machine (e.g. register values, flags, etc.) [if required] by calling the `SetCPUState` method.
5. Call the `Execute` method to execute the loaded Z80 code.
6. Once execution has completed, the `GetCPUState` method can be called to retrieve the final state of the machine (register values, flags, etc.).
7. The `Dump` method can be called to get a string detailing the final state of the machine (which can be useful for debugging purposes).

<br>

### Disassembler

The disassembler takes binary Z80 code and converts it to Z80 Assembly Language instructions. The included **Z80EmulatorConsoleTestApp** project includes a simple demonstration of this.  

The disassembler functionality is used as follows:  
1. Create an instance of the `Machine` class and load executable data into it (as above).  
2. Create an instance of the `Disassembler` class, supplying the `Machine` class instance, the address in memory at which to start disassembly, and the length of the code (in bytes).  
3. Add non-executable data sections as required - these allow you to mark specific blocks of memory as containing data that is not executable (this tells the disassembler not to attempt to disassemble this data into [what would be invalid] Z80 instructions; instead, it will output them as byte values using a DB assembler directive).  
4. Call the `Disassemble` method to perform the disassembly. This method returns a collection of tuple values, each of which consists of the address of the instruction and a string containing the disassembled instruction; for example, (0x2000, "LD BC,0400h").  
5. Iterate through the collection of tuples, outputting each address and disassembled instruction string (see the **Z80EmulatorConsoleTestApp** project for an example of this).

<br>

### What next?

The following are features that are being considered for the future:  
1. Implement some form of interactive debugger (with features such as single stepping, breakpoint handling, etc.).
2. Implement a Z80 assembler (because it's more than a tad frustrating having to supply Z80 executable data in binary format).
3. Add support for 'undocumented' instructions.


<br>

### History

| Version | Details
|---:| ---
| 1.0.0 | Initial implementation of Z80 emulator.


