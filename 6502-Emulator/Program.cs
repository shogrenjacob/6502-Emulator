
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

    private byte ZeroPage(Memory mem)
    {
        byte address = mem.data[PC];
        return address;
    }

    private byte ZeroPageX(Memory mem)
    {
        byte startingAddress = mem.data[PC];
        
        return (byte)(startingAddress + RegX);
    }

    private byte ZeroPageY(Memory mem)
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

    private ushort IndirectIndexed(Memory mem)
    {

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

            switch (instruction)
            {
                case 0xA9:
                    LDA(mem, Immediate(mem));
                    break;

                case 0xA5:
                    LDA(mem, ZeroPage(mem));
                    break;

                case 0xB5:
                    LDA(mem, ZeroPageX(mem));
                    break;

                case 0xAD:
                    LDA(mem, Absolute(mem));
                    break;

                case 0xBD:
                    LDA(mem, AbsoluteX(mem));
                    break;

                case 0xB9:
                    LDA(mem, AbsoluteY(mem));
                    break;

                case 0xA2:
                    LDX(mem, Immediate(mem));
                    break;

                case 0xA0:
                    LDY(mem, Immediate(mem));
                    break;

                case 0x4C:
                    JMP(mem, Absolute(mem));
                    break;

                case 0x6C: 
                    JMP(mem, Indirect(mem));
                    break;

                default:
                    Console.WriteLine($"Unrecognized command: {instruction}");
                    break;
            }
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Memory Memory = new();
        CPU Cpu = new();
        Input input = new();

        Cpu.Reset(Memory);

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