
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
    Int32 PC; // Program Counter
    Int32 SP; // Stack Pointer

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

    // Immediate Load Accumulator
    private void INS_LDA_IM(Memory mem)
    {
        // Load byte of memory into Accumulator
        byte value = Fetch(mem);
        Acc = value;

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
                    INS_LDA_IM(mem);
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