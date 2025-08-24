
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

public struct Instruction(Func<Memory, ushort> mode, Action<Memory, ushort> op, int cycles)
{
    public Func<Memory, ushort> AddressingMode = mode; // takes in Memory, returns a ushort (address)
    public Action<Memory, ushort> Operation = op; // takes in Memory and a ushort, void
    public int Cycles = cycles;
}

public class CPU
{
    ushort PC; // Program Counter
    byte SP; // Stack Pointer, add 0x100 to get true address

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
        // LDA
        LookupTable.Add(0xA9, new Instruction(Immediate, LDA, 2));
        LookupTable.Add(0xA5, new Instruction(ZeroPage, LDA, 3));
        LookupTable.Add(0xB5, new Instruction(ZeroPageX, LDA, 4));
        LookupTable.Add(0xAD, new Instruction(Absolute, LDA, 4));
        LookupTable.Add(0xBD, new Instruction(AbsoluteX, LDA, 4));
        LookupTable.Add(0xB9, new Instruction(AbsoluteY, LDA, 4));
        LookupTable.Add(0xA1, new Instruction(IndexedIndirect, LDA, 6));
        LookupTable.Add(0xB1, new Instruction(IndirectIndexed, LDA, 5));

        // LDX
        LookupTable.Add(0xA2, new Instruction(Immediate, LDX, 2));
        LookupTable.Add(0xA6, new Instruction(ZeroPage, LDX, 3));
        LookupTable.Add(0xB6, new Instruction(ZeroPageY, LDX, 4));
        LookupTable.Add(0xAE, new Instruction(Absolute, LDX, 4));
        LookupTable.Add(0xBE, new Instruction(AbsoluteY, LDX, 4));

        // LDY
        LookupTable.Add(0xA0, new Instruction(Immediate, LDY, 2));
        LookupTable.Add(0xA4, new Instruction(ZeroPage, LDY, 3));
        LookupTable.Add(0xB4, new Instruction(ZeroPageX, LDY, 4));
        LookupTable.Add(0xAC, new Instruction(Absolute, LDY, 4));
        LookupTable.Add(0xBC, new Instruction(AbsoluteX, LDY, 4));

        // JMP
        LookupTable.Add(0x4C, new Instruction(Absolute, JMP, 3));
        LookupTable.Add(0x6C, new Instruction(Indirect, JMP, 5));

        // INX, INY
        LookupTable.Add(0xE8, new Instruction(Implied, INX, 2));
        LookupTable.Add(0xC8, new Instruction(Implied, INY, 2));

        // INC
        LookupTable.Add(0xE6, new Instruction(ZeroPage, INC, 5));
        LookupTable.Add(0xF6, new Instruction(ZeroPageX, INC, 6));
        LookupTable.Add(0xEE, new Instruction(Absolute, INC, 6));
        LookupTable.Add(0xFE, new Instruction(AbsoluteX, INC, 7));

        // DEX, DEY
        LookupTable.Add(0xCA, new Instruction(Implied, DEX, 2));
        LookupTable.Add(0x88, new Instruction(Implied, DEY, 2));

        // DEC
        LookupTable.Add(0xC6, new Instruction(ZeroPage, DEC, 5));
        LookupTable.Add(0xD6, new Instruction(ZeroPageX, DEC, 6));
        LookupTable.Add(0xCE, new Instruction(Absolute, DEC, 6));
        LookupTable.Add(0xDE, new Instruction(AbsoluteX, DEC, 7));

        // CMP
        LookupTable.Add(0xC9, new Instruction(Immediate, CMP, 2));
        LookupTable.Add(0xC5, new Instruction(ZeroPage, CMP, 3));
        LookupTable.Add(0xD5, new Instruction(ZeroPageX, CMP, 4));
        LookupTable.Add(0xCD, new Instruction(Absolute, CMP, 4));
        LookupTable.Add(0xDD, new Instruction(AbsoluteX, CMP, 4));
        LookupTable.Add(0xD9, new Instruction(AbsoluteY, CMP, 4));
        LookupTable.Add(0xC1, new Instruction(IndexedIndirect, CMP, 6));
        LookupTable.Add(0xD1, new Instruction(IndirectIndexed, CMP, 5));

        // CPX
        LookupTable.Add(0xE0, new Instruction(Immediate, CPX, 2));
        LookupTable.Add(0xE4, new Instruction(ZeroPage, CPX, 3));
        LookupTable.Add(0xEC, new Instruction(Absolute, CPX, 4));

        // CPY
        LookupTable.Add(0xC0, new Instruction(Immediate, CPY, 2));
        LookupTable.Add(0xC4, new Instruction(ZeroPage, CPY, 3));
        LookupTable.Add(0xCC, new Instruction(Absolute, CPY, 4));

        // Clears
        LookupTable.Add(0x18, new Instruction(Implied, CLC, 2));
        LookupTable.Add(0xD8, new Instruction(Implied, CLD, 2));
        LookupTable.Add(0x58, new Instruction(Implied, CLI, 2));
        LookupTable.Add(0xB8, new Instruction(Implied, CLV, 2));

        // NOP
        LookupTable.Add(0xEA, new Instruction(Implied, NOP, 2));

        // Push, Pull Acc
        LookupTable.Add(0x48, new Instruction(Implied, PHA, 3));
        LookupTable.Add(0x68, new Instruction(Implied, PLA, 4));

        // STA
        LookupTable.Add(0x85, new Instruction(ZeroPage, STA, 3));
        LookupTable.Add(0x95, new Instruction(ZeroPageX, STA, 4));
        LookupTable.Add(0x8D, new Instruction(Absolute, STA, 4));
        LookupTable.Add(0x9D, new Instruction(AbsoluteX, STA, 5));
        LookupTable.Add(0x99, new Instruction(AbsoluteY, STA, 5));
        LookupTable.Add(0x81, new Instruction(IndexedIndirect, STA, 6));
        LookupTable.Add(0x91, new Instruction(IndirectIndexed, STA, 6));

        // STX
        LookupTable.Add(0x86, new Instruction(ZeroPage, STX, 3));
        LookupTable.Add(0x96, new Instruction(ZeroPageY, STX, 4));
        LookupTable.Add(0x8E, new Instruction(Absolute, STX, 4));

        // STY
        LookupTable.Add(0x84, new Instruction(ZeroPage, STY, 3));
        LookupTable.Add(0x94, new Instruction(ZeroPageX, STY, 4));
        LookupTable.Add(0x8C, new Instruction(Absolute, STY, 4));

        // Transfers
        LookupTable.Add(0xAA, new Instruction(Implied, TAX, 2));
        LookupTable.Add(0xA8, new Instruction(Implied, TAY, 2));
        LookupTable.Add(0xBA, new Instruction(Implied, TSX, 2));
        LookupTable.Add(0x8A, new Instruction(Implied, TXA, 2));
        LookupTable.Add(0x9A, new Instruction(Implied, TXS, 2));
        LookupTable.Add(0x98, new Instruction(Implied, TYA, 2));

        // Branching
        LookupTable.Add(0x90, new Instruction(Relative, BCC, 2));
        LookupTable.Add(0xB0, new Instruction(Relative, BCS, 2));
        LookupTable.Add(0xF0, new Instruction(Relative, BEQ, 2));
        LookupTable.Add(0x30, new Instruction(Relative, BMI, 2));
        LookupTable.Add(0xD0, new Instruction(Relative, BNE, 2));
        LookupTable.Add(0x10, new Instruction(Relative, BPL, 2));
        LookupTable.Add(0x50, new Instruction(Relative, BVC, 2));
        LookupTable.Add(0x70, new Instruction(Relative, BVS, 2));
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

    private ushort Implied(Memory mem)
    {
        return 0;
    }
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

    // TEST ME
    private ushort Relative(Memory mem)
    {
        ushort address = PC;
        sbyte offset = (sbyte)mem.data[PC];

        PC++;
        return (ushort)(address + offset);
    }

    /* INSTRUCTIONS */
    private void BCC(Memory mem, ushort address)
    {
        if (CarryFlag == 0)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BCS(Memory mem, ushort address)
    {
        if (CarryFlag == 1)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BEQ(Memory mem, ushort address)
    {
        if (ZeroFlag == 1)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BMI(Memory mem, ushort address)
    {
        if (NegFlag == 1)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BNE(Memory mem, ushort address)
    {
        if (ZeroFlag == 0)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BPL(Memory mem, ushort address)
    {
        if (ZeroFlag == 0)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BVC(Memory mem, ushort address)
    {
        if (OverflowFlag == 0)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void BVS(Memory mem, ushort address)
    {
        if (OverflowFlag == 1)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void LDA(Memory mem, ushort address)
    {
        Acc = mem.data[address];

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
            ZeroFlag = 1;
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
            ZeroFlag = 1;
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

    private void INX(Memory mem, ushort address)
    {
        RegX++;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void INY(Memory mem, ushort address)
    {
        RegY++;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void INC(Memory mem, ushort address)
    {
        mem.data[address]++;

        if (mem.data[address] == 0)
        {
            ZeroFlag = 1;
        }
        else if (mem.data[address] < 0)
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

    private void DEC(Memory mem, ushort address)
    {
        mem.data[address]--;

        if (mem.data[address] == 0)
        {
            ZeroFlag = 1;
        }
        else if (mem.data[address] < 0)
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

    private void DEX(Memory mem, ushort address)
    {
        RegX--;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void DEY(Memory mem, ushort address)
    {
        RegY--;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void CMP(Memory mem, ushort address)
    {
        int result = Acc - mem.data[address];

        if (result >= 0)
        {
            CarryFlag = 1;

            if (result == 0)
            {
                ZeroFlag = 1;
            }
        }
        else
        {
            NegFlag = 1;
        }

        PC++;
    }

    private void CPX(Memory mem, ushort address)
    {
        int result = RegX - mem.data[address];

        if (result >= 0)
        {
            CarryFlag = 1;

            if (result == 0)
            {
                ZeroFlag = 1;
            }
        }
        else
        {
            NegFlag = 1;
        }

        PC++;
    }

    private void CPY(Memory mem, ushort address)
    {
        int result = RegY - mem.data[address];

        if (result >= 0)
        {
            CarryFlag = 1;

            if (result == 0)
            {
                ZeroFlag = 1;
            }
        }
        else
        {
            NegFlag = 1;
        }

        PC++;
    }

    private void CLC(Memory mem, ushort address)
    {
        CarryFlag = 0;
        PC++;
    }

    private void CLD(Memory mem, ushort address)
    {
        DecMode = 0;
        PC++;
    }

    private void CLI(Memory mem, ushort address)
    {
        InterruptDisable = 0;
        PC++;
    }

    private void CLV(Memory mem, ushort address)
    {
        OverflowFlag = 0;
        PC++;
    }

    private void JMP(Memory mem, ushort address)
    {
        PC = address;
    }

    private void NOP(Memory mem, ushort address)
    {
        PC++;
    }

    private void PHA(Memory mem, ushort address)
    {
        mem.data[SP + 0x100] = Acc;
        SP++;
        PC++;
    }

    private void PLA(Memory mem, ushort address)
    {
        Acc = mem.data[SP + 0x100];

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        SP--;
        PC++;
    }

    private void STA(Memory mem, ushort address)
    {
        mem.data[address] = Acc;
        PC++;
    }

    private void STX(Memory mem, ushort address)
    {
        mem.data[address] = RegX;
        PC++;
    }

    private void STY(Memory mem, ushort address)
    {
        mem.data[address] = RegY;
        PC++;
    }

    private void TAX(Memory mem, ushort address)
    {
        RegX = Acc;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void TAY(Memory mem, ushort address)
    {
        RegY = Acc;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void TSX(Memory mem, ushort address)
    {
        RegX = SP;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void TXA(Memory mem, ushort address)
    {
        Acc = RegX;

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
            NegFlag = 0;
            ZeroFlag = 0;
        }

        PC++;
    }

    private void TXS(Memory mem, ushort address)
    {
        SP = RegX;
        PC++;
    }

    private void TYA(Memory mem, ushort address)
    {
        Acc = RegY;

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

        PC++;
    }

    public void Reset(Memory mem)
    {
        PC = 0xFFFC;
        SP = 0x00;

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