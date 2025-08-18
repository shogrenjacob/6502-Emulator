
/* Reserved Memory Ranges
 * ----------------------
 * Zero Page ($0000 - $00FF)
 * System Stack ($0100 - $01FF)
 * Last 6 bytes of memory ($FFFA - $FFFF)
 * 
 * Notes
 * _______________________
 * Processor is Little Endian
*/

public struct Instruction
{
    public Func<Memory, ushort> AddressingMode; // takes in Memory, returns a ushort (address)
    public Action<Memory, ushort> Operation; // takes in Memory and a ushort, returns void
    public int Cycles;

    public Instruction(Func<Memory, ushort> mode, Action<Memory, ushort> op, int cycles)
    {
        this.AddressingMode = mode;
        this.Operation = op;
        this.Cycles = cycles;
    }
}

public class CPU
{
    ushort PC; // Program Counter
    ushort SP; // Stack Pointer

    byte Acc;  // Accumulator
    byte RegX;  // Index Register X
    byte RegY;  // Index Register Y

    byte CarryFlag = 1;
    byte ZeroFlag = 1;
    byte InterruptDisable = 1;
    byte DecMode = 1;
    byte BreakCmd = 1;
    byte OverflowFlag = 1;
    byte NegFlag = 1;

    Dictionary<byte, Instruction> LookupTable = new();

    public void LoadLookupTable()
    {
        // LDA Instructions
        LookupTable.Add(0xA9, new Instruction(Immediate, LDA, 2));
        LookupTable.Add(0xA5, new Instruction(ZeroPage, LDA, 3));
        LookupTable.Add(0xB5, new Instruction(ZeroPageX, LDA, 4));
        LookupTable.Add(0xAD, new Instruction(Absolute, LDA, 4));
        LookupTable.Add(0xBD, new Instruction(AbsoluteX, LDA, 4));
        LookupTable.Add(0xB9, new Instruction(AbsoluteY, LDA, 4));
        LookupTable.Add(0xA1, new Instruction(IndexedIndirect, LDA, 6));
        LookupTable.Add(0xB1, new Instruction(IndirectIndexed, LDA, 5));

        // LDX Instructions
        LookupTable.Add(0xA2, new Instruction(Immediate, LDX, 2));
        LookupTable.Add(0xA6, new Instruction(ZeroPage, LDX, 3));
        LookupTable.Add(0xB6, new Instruction(ZeroPageY, LDX, 4));
        LookupTable.Add(0xAE, new Instruction(Absolute, LDX, 4));
        LookupTable.Add(0xBE, new Instruction(AbsoluteY, LDX, 4));

        // LDY Instructions
        LookupTable.Add(0xA0, new Instruction(Immediate, LDY, 2));
        LookupTable.Add(0xA4, new Instruction(ZeroPage, LDY, 3));
        LookupTable.Add(0xB4, new Instruction(ZeroPageX, LDY, 4));
        LookupTable.Add(0xAC, new Instruction(Absolute, LDY, 4));
        LookupTable.Add(0xBC, new Instruction(AbsoluteX, LDY, 4));

        // JMP Instructions
        LookupTable.Add(0x4C, new Instruction(Absolute, JMP, 3));
        LookupTable.Add(0x6C, new Instruction(Indirect, JMP, 5));
    }

    // For Debugging
    public void PrintPCSP()
    {
        Console.WriteLine("---------- PC/SP ----------");
        Console.WriteLine($"Program Counter: 0x{PC.ToString("X")} \n Stack Pointer: 0x{SP.ToString("X")}");
    }

    public void PrintRegisters()
    {
        Console.WriteLine("-------- Registers --------");
        Console.WriteLine($"Accumulator: 0x{Acc.ToString("X")} \n X Register: 0x{RegX.ToString("X")} \n Y Register: 0x{RegY.ToString("X")}");
    }

    public void PrintFlags()
    {
        Console.WriteLine("---------- Flags ----------");
        Console.WriteLine($"Carry Flag: {CarryFlag} \n Zero Flag: {ZeroFlag} \n Interrupt Disable: {InterruptDisable}");
        Console.WriteLine($"Decimal Mode: {DecMode} \n Break Command: {BreakCmd} \n Overflow Flag: {OverflowFlag} \n Negative Flag: {NegFlag}");
    }

    /* ADRESSING MODES */
    private ushort Immediate(Memory mem)
    {
        return PC;
    }

    private ushort ZeroPage(Memory mem)
    {
        byte address = mem.data[PC];
        return address;
    }

    private ushort ZeroPageX(Memory mem)
    {
        byte startingAddress = mem.data[PC];
        
        return (byte)(startingAddress + RegX);
    }

    private ushort ZeroPageY(Memory mem)
    {
        byte startingAddress = mem.data[PC];

        return (byte)(startingAddress + RegY);
    }

    private ushort Absolute(Memory mem)
    {
        byte lo = mem.data[PC];
        PC++;
        byte hi = mem.data[PC];

        return (ushort)((hi << 8) | lo);
    }

    private ushort AbsoluteX(Memory mem)
    {
        ushort startingAddress = Absolute(mem);

        return (ushort)(startingAddress + RegX);
    }

    private ushort AbsoluteY(Memory mem)
    {
        ushort startingAddress = Absolute(mem);

        return (ushort)(startingAddress + RegY);
    }

    private ushort Indirect(Memory mem)
    {
        ushort initialAddress = Absolute(mem);
        ushort address = (ushort)((mem.data[initialAddress + 1] << 8) | mem.data[initialAddress]);

        return address;
    }

    private ushort IndexedIndirect(Memory mem)
    {
        byte zeroPageAddress = (byte)((mem.data[PC] + RegX) & 0xFF);
        byte lo = mem.data[zeroPageAddress];
        byte hi = mem.data[(zeroPageAddress + 1) & 0xFF];

        return (ushort)((hi << 8) | lo);
    }

    private ushort IndirectIndexed(Memory mem)
    {
        byte zeroPageAddress = mem.data[PC];
        byte lo = mem.data[zeroPageAddress];

        // Ensure we don't leave zero page
        byte hi = mem.data[(zeroPageAddress + 1) & 0xFF];

        ushort baseAddress = (ushort)((hi << 8) | lo);

        return (ushort)(baseAddress + RegY);
    }

    /* INSTRUCTIONS */
    private void LDA(Memory mem, ushort address)
    {
        Acc = mem.data[address];

        if (Acc == 0)
        {
            ZeroFlag = 0;
        }
        else if (Acc < 0)
        {
            NegFlag = 1;
        }
        else
        {
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void LDX(Memory mem, ushort address)
    {
        RegX = mem.data[address];

        if (RegX == 0)
        {
            ZeroFlag = 0;
        }
        else if (RegX < 0)
        {
            NegFlag = 1;
        }
        else
        {
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void LDY(Memory mem, ushort address)
    {
        RegY = mem.data[address];

        if (RegY == 0)
        {
            ZeroFlag = 0;
        }
        else if (RegY < 0)
        {
            NegFlag = 1;
        }
        else
        {
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void JMP(Memory mem, ushort address)
    {
        PC = address;
    }

    public void Reset(Memory mem)
    {
        PC = 0xFFFC;
        SP = 0x0100;

        DecMode = 0;
        Acc = 0;
        RegX = 0;
        RegY = 0;
        CarryFlag = 0;
        ZeroFlag = 0;
        InterruptDisable = 0;
        BreakCmd = 0;
        OverflowFlag = 0;
        NegFlag = 0;

        mem.init();
    }

    /* FETCH, DECODE, EXECUTE */

    public byte Read(Memory mem)
    {
        return mem.data[PC];
    }

    public byte Write(Memory mem, byte val)
    {
        mem.data[PC] = val;
        return mem.data[PC];
    }

    public void Execute(Int32 cycles, Memory mem)
    {
        // One cycle needed per command
        while (cycles > 0)
        {
            byte instruction = Read(mem);
            PC++;
            cycles--;

            Instruction CurrentInstruction = LookupTable[instruction];
            ushort address = CurrentInstruction.AddressingMode(mem);

            CurrentInstruction.Operation(mem, address);
        }
    }
}

/* ENTRY POINT */
public class Program
{
    public static void Main(string[] args)
    {
        Memory Memory = new();
        CPU Cpu = new();
        Input input = new();

        Cpu.Reset(Memory);
        Cpu.LoadLookupTable();

        input.GetFile();
        for (int i = 0; i < input.data.Count; i++)
        {
            Memory.data[input.address[i]] = input.data[i];
        }

        Cpu.Execute(2, Memory);

        Cpu.PrintPCSP();
        Cpu.PrintRegisters();
        Cpu.PrintFlags();
    }
}