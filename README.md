# A 6502 Processor Emulator

This is (obviously) my C# implementation of a 6502 Processor. Currently, you can write out a .txt file with a set memory state to run programs on the processor. A .txt file wanting to achieve this should be formatted like the following:

```
0x8000 0xA9
0x8001 0xFF
```

Where the left column is the memory location and the right column is the opcode or data. Note that you don't have to write the memory contents in ascending order, that's just an organization preference for me.

As for the memory format, program memory starts at _0x8000_ and ends at _0xFFFC_ where the reset vector lives. As with a typical 6502 there is also the zero page to use as you wish. Finally, _0x0100 - 0x7FFF_ is for I/O and expansions, here's that visually:

![Memory Layout Image](/imgs/memory_layout.png "My 6502 Memory Layout")

When developing a ROM, currently the processor has a quirk with cycle counts where, to terminate a program, you must end your program with a _JMP 0x9999_ command. Otherwise, the program will run infinitely. Other than that, the source code in its current form will work as you expect and print the processor status after your program runs to show the results of your program.

> This is actually accurate to how the real processor runs! You wouldn't want the processor to shut down once it's done executing your instructions would you? In real life, the processor is totally happy just running infinitely and getting instructions, but for the current form of my emulator stopping it after it finishes makes a bit more sense.

## Running The Emulator
The emulator is just a Visual Studio project running on .NET 8, so as long as you have the .NET 8 runtime and Visual Studio (or Vscode with the necessary extensions) it should run on any machine!

Once you start up the program, you'll just need to provide the absolute path to a .txt file in the format I described earlier or one of the built in test files provided along with the source code. The built in test arguments you could provide are below:

| Test Name  | Description  | 
|---|---|
| test | Loads 0xFF into the Accumulator. |
| fib | Calculates and loads the 6th Fibonacci Number into the Accumulator.|

In the future, I want to fix that cycling quirk as well as write an assembler to write 6502 Assembly for the Emulator.

## Resources I Used
http://www.6502.org/users/obelisk/6502/ \
An incredible resource for anything 6502. This is where I got the _vast_ majority of information I needed for developing the emulator. This includes the overall architecture, addressing mode info, instructions reference, opcode layout, and memory layout.

https://github.com/OneLoneCoder/olcNES \
A repo featuring a 6502 implementation from [OneLoneCoder.com](https://onelonecoder.com/) YouTuber [Javidx9](https://www.youtube.com/@javidx9). This repo and its [YouTube video](https://www.youtube.com/watch?v=nViZg02IMQo&list=PLrOv9FMX8xJHqMvSGB_9G9nZZ_4IgteYf) were helpful when I was implementing some trickier instructions like SBC.