// TODO: add other addressing modes to use with instructions

/* Reserved Memory Ranges
 * ----------------------
 * Zero Page ($0000 - $00FF)
 * System Stack ($0100 - $01FF)
 * Last 6 bytes of memory ($FFFA - $FFFF)
 * 
 * Notes
 * -------------------------
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

    /* -------- ADDRESSING MODES -------- */
    private ushort Immediate(Memory mem)
    {
        return PC++;
    }

    // Implicit needs no extra code

    private byte ZeroPage(Memory mem)
    {
        byte address = Fetch(mem);
        return address;
    }

    private byte ZeroPageX(Memory mem)
    {
        byte zeroPageAddress = Fetch(mem);
        // C# implicitly truncates to be below 0x100 when casting to a byte, no need to manually wrap
        return (byte)(zeroPageAddress + RegX);
    }

    private byte ZeroPageY(Memory mem)
    {
        byte zeroPageAddress = Fetch(mem);
        // C# implicitly truncates to be below 0x100 when casting to a byte, no need to manually wrap
        return (byte)(zeroPageAddress + RegY);
    }

    private ushort Relative(Memory mem)
    {
        sbyte offset = (sbyte)(Fetch(mem));
        return (ushort)(PC + offset);
    }

    private ushort Absolute(Memory mem)
    {
        byte lo = Fetch(mem);
        byte hi = Fetch(mem);

        return (ushort)((hi << 8) | lo);
    }

    private ushort AbsoluteX(Memory mem)
    {
        return (ushort)(Absolute(mem) + RegX);
    }

    private ushort AbsoluteY(Memory mem)
    {
        return (ushort)(Absolute(mem) + RegY);
    }

    private ushort Indirect(Memory mem)
    {
        ushort indirectAdress = Absolute(mem);

        byte lo = mem.data[indirectAdress];
        byte hi = mem.data[indirectAdress + 1];

        return (ushort)((hi << 8) | lo);
    }

    /* -------- INSTRUCTIONS -------- */
    // Immediate Load Accumulator
    private void LDA(Memory mem, string addressingMode)
    {
        if (addressingMode == "Immediate")
        {
            ushort address = Immediate(mem);
            Acc = mem.data[address];
        }

        // Set zero and Negative flags as appropriate
        if (Acc == 0)
        {
            ZeroFlag = 1;
        }
        else if (Acc < 0)
        {
            NegFlag = 1;
        }
        else
        {
            ZeroFlag = 0;
            NegFlag = 0;
        }
    }

    // Immediate Load Register X
    private void INS_LDX_IM(Memory mem)
    {
        byte value = Fetch(mem);
        RegX = value;

        if (RegX == 0)
        {
            ZeroFlag = 1;
        }
        else if (RegX < 0)
        {
            NegFlag = 1;
        }
        else
        {
            ZeroFlag = 0;
            NegFlag = 0;
        }
    }

    // Immediate Load Register Y
    private void INS_LDY_IM(Memory mem)
    {
        byte value = Fetch(mem);
        RegY = value;

        if (RegY == 0)
        {
            ZeroFlag = 1;
        }
        else if (RegY < 0)
        {
            NegFlag = 1;
        }
        else
        {
            ZeroFlag = 0;
            NegFlag = 0;
        }
    }

    // Absolute Jump
    private void INS_JMP_ABS(Memory mem)
    {
        byte byte1 = Fetch(mem);
        byte byte2 = Fetch(mem);

        ushort byte2Converted = (ushort)(byte2 << 8);

        // Combine the two bytes for a 16 bit address, little endian so byte2 is the most significant byte.
        ushort address = (ushort)(byte2Converted | (ushort)byte1);

        if (address <= 0x01FF || address >= 0xFFFA )
        {
            Console.WriteLine($"Jump address invalid | address: {address}");
            return;
        }

        PC = address;
    }

    // Indirect Jump
    private void INS_JMP_IND(Memory mem)
    {
        INS_JMP_ABS(mem);
        INS_JMP_ABS(mem);
    }

    // Set Carry Flag
    private void INS_SEC(Memory mem)
    {
        CarryFlag = 1;
    }

    // Set decimal flag
    private void INS_SED(Memory mem)
    {
        DecMode = 1;
    }

    private void INS_AND_IM(Memory mem)
    {
        byte value = Fetch(mem);
        byte result = (byte)(Acc & value);

        if (result == 0)
        {
            ZeroFlag = 1;
        }
        else if (result < 0)
        {
            NegFlag = 1;
        }
        else
        {
            ZeroFlag = 0;
            NegFlag = 0;
        }

        Acc = result;
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

    public byte Fetch(Memory mem)
    {
        byte data = mem.data[PC];
        PC++;

        return data;
    }

    public void Execute(Int32 cycles, Memory mem)
    {
        // One cycle needed per command
        while (cycles > 0)
        {
            byte instruction = Fetch(mem);
            cycles--;

            switch (instruction)
            {
                case 0xA9:
                    LDA(mem, "Immedatiate");
                    break;

                case 0xA2:
                    INS_LDX_IM(mem);
                    break;

                case 0xA0:
                    INS_LDY_IM(mem);
                    break;

                case 0x4C:
                    INS_JMP_ABS(mem);
                    break;

                case 0x6C:
                    INS_JMP_IND(mem);
                    break;

                case 0x38:
                    INS_SEC(mem);
                    break;

                case 0xF8:
                    INS_SED(mem);
                    break;

                case 0x29:
                    INS_AND_IM(mem);
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

        Cpu.Execute(4, Memory);

        Cpu.PrintPCSP();
        Cpu.PrintRegisters();
        Cpu.PrintFlags();
    }
}