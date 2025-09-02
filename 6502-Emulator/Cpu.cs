
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
    byte ProcessorStatusReg; // N V E B D I Z C

    byte currentOpcode;

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

        // Sets
        LookupTable.Add(0x38, new Instruction(Implied, SEC, 2));
        LookupTable.Add(0xF8, new Instruction(Implied, SED, 2));
        LookupTable.Add(0x78, new Instruction(Implied, SEI, 2));

        // AND
        LookupTable.Add(0x29, new Instruction(Immediate, AND, 2));
        LookupTable.Add(0x25, new Instruction(ZeroPage, AND, 3));
        LookupTable.Add(0x35, new Instruction(ZeroPageX, AND, 4));
        LookupTable.Add(0x2D, new Instruction(Absolute, AND, 4));
        LookupTable.Add(0x3D, new Instruction(AbsoluteX, AND, 4));
        LookupTable.Add(0x39, new Instruction(AbsoluteY, AND, 4));
        LookupTable.Add(0x21, new Instruction(IndexedIndirect, AND, 6));
        LookupTable.Add(0x31, new Instruction(IndirectIndexed, AND, 5));

        // BIT
        LookupTable.Add(0x24, new Instruction(ZeroPage, BIT, 3));
        LookupTable.Add(0x2C, new Instruction(Absolute, BIT, 4));

        // EOR
        LookupTable.Add(0x49, new Instruction(Immediate, EOR, 2));
        LookupTable.Add(0x45, new Instruction(ZeroPage, EOR, 3));
        LookupTable.Add(0x55, new Instruction(ZeroPageX, EOR, 4));
        LookupTable.Add(0x4D, new Instruction(Absolute, EOR, 4));
        LookupTable.Add(0x5D, new Instruction(AbsoluteX, EOR, 4));
        LookupTable.Add(0x59, new Instruction(AbsoluteY, EOR, 4));
        LookupTable.Add(0x41, new Instruction(IndexedIndirect, EOR, 6));
        LookupTable.Add(0x51, new Instruction(IndirectIndexed, EOR, 5));

        // ORA
        LookupTable.Add(0x09, new Instruction(Immediate, ORA, 2));
        LookupTable.Add(0x05, new Instruction(ZeroPage, ORA, 3));
        LookupTable.Add(0x15, new Instruction(ZeroPageX, ORA, 4));
        LookupTable.Add(0x0D, new Instruction(Absolute, ORA, 4));
        LookupTable.Add(0x1D, new Instruction(AbsoluteX, ORA, 4));
        LookupTable.Add(0x19, new Instruction(AbsoluteY, ORA, 4));
        LookupTable.Add(0x01, new Instruction(IndexedIndirect, ORA, 6));
        LookupTable.Add(0x11, new Instruction(IndirectIndexed, ORA, 5));

        // Subroutines
        LookupTable.Add(0x60, new Instruction(Implied, RTS, 6));
        LookupTable.Add(0x20, new Instruction(Absolute, JSR, 6));

        // PHP, PLP
        LookupTable.Add(0x08, new Instruction(Implied, PHP, 3));
        LookupTable.Add(0x28, new Instruction(Implied, PLP, 4));

        // BRK, RTI
        LookupTable.Add(0x00, new Instruction(Implied, BRK, 7));
        LookupTable.Add(0x40, new Instruction(Implied, RTI, 6));

        // ADC
        LookupTable.Add(0x69, new Instruction(Immediate, ADC, 2));
        LookupTable.Add(0x65, new Instruction(ZeroPage, ADC, 3));
        LookupTable.Add(0x75, new Instruction(ZeroPageX, ADC, 4));
        LookupTable.Add(0x6D, new Instruction(Absolute, ADC, 4));
        LookupTable.Add(0x7D, new Instruction(AbsoluteX, ADC, 4));
        LookupTable.Add(0x79, new Instruction(AbsoluteY, ADC, 4));
        LookupTable.Add(0x61, new Instruction(IndexedIndirect, ADC, 6));
        LookupTable.Add(0x71, new Instruction(IndirectIndexed, ADC, 5));

        /* SBC
        LookupTable.Add(0xE9, new Instruction(Immediate, SBC, 2));
        LookupTable.Add(0xE5, new Instruction(ZeroPage, SBC, 3));
        LookupTable.Add(0xF5, new Instruction(ZeroPageX, SBC, 4));
        LookupTable.Add(0xED, new Instruction(Absolute, SBC, 4));
        LookupTable.Add(0xFD, new Instruction(AbsoluteX, SBC, 4));
        LookupTable.Add(0xF9, new Instruction(AbsoluteY, SBC, 4));
        LookupTable.Add(0xE1, new Instruction(IndexedIndirect, SBC, 6));
        LookupTable.Add(0xF1, new Instruction(IndirectIndexed, SBC, 5));
        */

        // ASL
        LookupTable.Add(0x0A, new Instruction(Accumulator, ASL, 2));
        LookupTable.Add(0x06, new Instruction(ZeroPage, ASL, 5));
        LookupTable.Add(0x16, new Instruction(ZeroPageX, ASL, 6));
        LookupTable.Add(0x0E, new Instruction(Absolute, ASL, 6));
        LookupTable.Add(0x1E, new Instruction(AbsoluteX, ASL, 7));

        // LSR
        LookupTable.Add(0x4A, new Instruction(Accumulator, LSR, 2));
        LookupTable.Add(0x46, new Instruction(ZeroPage, LSR, 5));
        LookupTable.Add(0x56, new Instruction(ZeroPageX, LSR, 6));
        LookupTable.Add(0x4E, new Instruction(Absolute, LSR, 6));
        LookupTable.Add(0x5E, new Instruction(AbsoluteX, LSR, 7));

        // ROL
        LookupTable.Add(0x2A, new Instruction(Accumulator, ROL, 2));
        LookupTable.Add(0x26, new Instruction(ZeroPage, ROL, 5));
        LookupTable.Add(0x36, new Instruction(ZeroPageX, ROL, 6));
        LookupTable.Add(0x2E, new Instruction(Absolute, ROL, 6));
        LookupTable.Add(0x3E, new Instruction(AbsoluteX, ROL, 7));

        // ROR
        LookupTable.Add(0x6A, new Instruction(Accumulator, ROR, 2));
        LookupTable.Add(0x66, new Instruction(ZeroPage, ROR, 5));
        LookupTable.Add(0x76, new Instruction(ZeroPageX, ROR, 6));
        LookupTable.Add(0x6E, new Instruction(Absolute, ROR, 6));
        LookupTable.Add(0x7E, new Instruction(AbsoluteX, ROR, 7));
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
        Console.WriteLine($"Carry Flag: {getFlag("C")} \n Zero Flag: {getFlag("Z")} \n Interrupt Disable: {getFlag("I")}");
        Console.WriteLine($"Decimal Mode: {getFlag("D")} \n Break Command: {getFlag("B")} \n Overflow Flag: {getFlag("V")} \n Negative Flag: {getFlag("N")}");
    }

    /* ADRESSING MODES */

    private ushort Implied(Memory mem)
    {
        return 0;
    }

    private ushort Accumulator(Memory mem)
    {
        return Acc;
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
        if ((ProcessorStatusReg & 0x01) == 0)
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
        if ((ProcessorStatusReg & 0x01) == 1)
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
        if ((ProcessorStatusReg & 0x02) == 1)
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
        if ((ProcessorStatusReg & 0x80) == 1)
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
        if ((ProcessorStatusReg & 0x80) == 0)
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
        if ((ProcessorStatusReg & 0x80) == 0)
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
        if ((ProcessorStatusReg & 0x40) == 0)
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
        if ((ProcessorStatusReg & 0x40) == 1)
        {
            PC = address;
        }
        else
        {
            PC++;
        }
    }

    private void SEC(Memory mem, ushort address)
    {
        setFlag("C");
    }

    private void SED(Memory mem, ushort address)
    {
        setFlag("D");
    }

    private void SEI(Memory mem, ushort address)
    {
        setFlag("I");
    }

    private void LDA(Memory mem, ushort address)
    {
        Acc = mem.data[address];

        if (Acc == 0)
        {
            setFlag("Z");
        }
        if (Acc < 0)
        {
            setFlag("N");
        }

        PC++;
    }

    private void LDX(Memory mem, ushort address)
    {
        RegX = mem.data[address];

        if (RegX == 0)
        {
            setFlag("Z");
        }
        if (RegX < 0)
        {
            setFlag("N");
        }

        PC++;
    }

    private void LDY(Memory mem, ushort address)
    {
        RegY = mem.data[address];

        if (RegY == 0)
        {
            setFlag("Z");
        }
        if (RegY < 0)
        {
            setFlag("N");
        }

        PC++;
    }

    private void INX(Memory mem, ushort address)
    {
        RegX++;

        if (RegX == 0)
        {
            setFlag("Z");
        }
        if (RegX < 0)
        {
            setFlag("N");
        }
    }

    private void INY(Memory mem, ushort address)
    {
        RegY++;

        if (RegY == 0)
        {
            setFlag("Z");
        }
        if (RegY < 0)
        {
            setFlag("N");
        }
    }

    private void INC(Memory mem, ushort address)
    {
        mem.data[address]++;

        if (mem.data[address] == 0)
        {
            setFlag("Z");
        }
        else if (mem.data[address] < 0)
        {
            setFlag("N");
        }

        PC++;
    }

    private void DEC(Memory mem, ushort address)
    {
        mem.data[address]--;

        if (mem.data[address] == 0)
        {
            setFlag("Z");
        }
        else if (mem.data[address] < 0)
        {
            setFlag("N");
        }

        PC++;
    }

    private void DEX(Memory mem, ushort address)
    {
        RegX--;

        if (RegX == 0)
        {
            setFlag("Z");
        }
        else if (RegX < 0)
        {
            setFlag("N");
        }
    }

    private void DEY(Memory mem, ushort address)
    {
        RegY--;

        if (RegY == 0)
        {
            setFlag("Z");
        }
        else if (RegY < 0)
        {
            setFlag("N");
        }
    }

    private void CMP(Memory mem, ushort address)
    {
        int result = Acc - mem.data[address];

        if (result >= 0)
        {
            setFlag("C");

            if (result == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
            setFlag("N");
        }

        PC++;
    }

    private void CPX(Memory mem, ushort address)
    {
        int result = RegX - mem.data[address];

        if (result >= 0)
        {
            setFlag("C");

            if (result == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
            setFlag("N");
        }

        PC++;
    }

    private void CPY(Memory mem, ushort address)
    {
        int result = RegY - mem.data[address];

        if (result >= 0)
        {
            setFlag("C");

            if (result == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
           setFlag("N");
        }

        PC++;
    }

    private void CLC(Memory mem, ushort address)
    {
        setFlag("C");
    }

    private void CLD(Memory mem, ushort address)
    {
        setFlag("D");
    }

    private void CLI(Memory mem, ushort address)
    {
        setFlag("I");
    }

    private void CLV(Memory mem, ushort address)
    {
        setFlag("V");
    }

    private void JMP(Memory mem, ushort address)
    {
        PC = address;
    }

    private void NOP(Memory mem, ushort address)
    {

    }

    private void PHA(Memory mem, ushort address)
    {
        mem.data[SP + 0x100] = Acc;
        SP--;
    }

    private void PLA(Memory mem, ushort address)
    {
        SP++;
        Acc = mem.data[SP + 0x100];

        if (Acc == 0)
        {
            setFlag("Z");
        }
        if (Acc < 0)
        {
            setFlag("N");
        }
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
            setFlag("Z");
        }
        if (RegX < 0)
        {
            setFlag("N");
        }
    }

    private void TAY(Memory mem, ushort address)
    {
        RegY = Acc;

        if (RegY == 0)
        {
            setFlag("Z");
        }
        if (RegY < 0)
        {
            setFlag("N");
        }
    }

    private void TSX(Memory mem, ushort address)
    {
        RegX = SP;

        if (RegX == 0)
        {
            setFlag("Z");
        }
        if (RegX < 0)
        {
            setFlag("N");
        }
    }

    private void TXA(Memory mem, ushort address)
    {
        Acc = RegX;

        if (Acc == 0)
        {
           setFlag("Z");
        }
        if (Acc < 0)
        {
            setFlag("N");
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
            setFlag("Z");
        }
        else if (Acc < 0)
        {
            setFlag("N");
        }
    }

    private void AND(Memory mem, ushort address)
    {
        Acc = (byte)(Acc & mem.data[address]);

        if (Acc == 0)
        {
            setFlag("Z");
        }
        if ((Acc & 128) == 128)
        {
            setFlag("N");
        }
    }

    private void ASL(Memory mem, ushort address)
    {

        if (currentOpcode == 0x0A)
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((Acc & (1 << 7)) != 0)
            {
                setFlag("C");
            }

            Acc = (byte)(Acc << 1);

            // Check 7th bit AFTER to determine if it is negative
            if ((Acc & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (Acc == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((mem.data[address] & (1 << 7)) != 0)
            {
                setFlag("C");
            }

            mem.data[address] = (byte)(mem.data[address] << 1);

            // Check 7th bit AFTER to determine if it is negative
            if ((mem.data[address] & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (mem.data[address] == 0)
            {
                setFlag("Z");
            }

            PC++;
        }
    }

    private void LSR(Memory mem, ushort address)
    {

        if (currentOpcode == 0x4A) // Check for Accumulator opcode
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((Acc & 1) != 0)
            {
                setFlag("C");
            }

            Acc = (byte)(Acc >> 1);

            // Check 7th bit AFTER to determine if it is negative
            if ((Acc & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (Acc == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((mem.data[address] & 1) != 0)
            {
                setFlag("C");
            }

            mem.data[address] = (byte)(mem.data[address] >> 1);

            // Check 7th bit AFTER to determine if it is negative
            if ((mem.data[address] & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (mem.data[address] == 0)
            {
                setFlag("Z");
            }

            PC++;
        }
    }

    private void ROL(Memory mem, ushort address)
    {
        int cStatus = getFlag("C");

        if (currentOpcode == 0x2A)
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((Acc & (1 << 7)) != 0)
            {
                setFlag("C");
            }

            Acc = (byte)(Acc << 1);
            Acc |= (byte)cStatus;

            // Check 7th bit AFTER to determine if it is negative
            if ((Acc & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (Acc == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((mem.data[address] & (1 << 7)) != 0)
            {
                setFlag("C");
            }

            mem.data[address] = (byte)(mem.data[address] << 1);
            mem.data[address] |= (byte)cStatus;

            // Check 7th bit AFTER to determine if it is negative
            if ((mem.data[address] & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (mem.data[address] == 0)
            {
                setFlag("Z");
            }

            PC++;
        }
    }

    private void ROR(Memory mem, ushort address)
    {
        int cStatus = getFlag("C");

        if (currentOpcode == 0x6A) // Check for Accumulator opcode
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((Acc & 1) != 0)
            {
                setFlag("C");
            }

            Acc = (byte)(Acc >> 1);
            Acc |= (byte)cStatus;

            // Check 7th bit AFTER to determine if it is negative
            if ((Acc & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (Acc == 0)
            {
                setFlag("Z");
            }
        }
        else
        {
            // Check if 7th bit is set before shift to determine if we need to carry
            if ((mem.data[address] & 1) != 0)
            {
                setFlag("C");
            }

            mem.data[address] = (byte)(mem.data[address] >> 1);
            mem.data[address] |= (byte)cStatus;

            // Check 7th bit AFTER to determine if it is negative
            if ((mem.data[address] & (1 << 7)) != 0)
            {
                setFlag("N");
            }

            if (mem.data[address] == 0)
            {
                setFlag("Z");
            }

            PC++;
        }
    }

    private void BIT(Memory mem, ushort address)
    {
        byte result = (byte)(Acc & mem.data[address]);

        if ((result & 128) == 128)
        {
            setFlag("N");
        }

        if ((result & 64) == 64)
        {
            setFlag("V");
        }

        if (result == 0)
        {
            setFlag("Z");
        }
    }

    private void EOR(Memory mem, ushort address)
    {
        Acc = (byte)(Acc ^ mem.data[address]);

        if (Acc == 0)
        {
            setFlag("Z");
        }
        if ((byte)(Acc & 128) == 128)
        {
            setFlag("N");
        }
    }

    private void ORA(Memory mem, ushort address)
    {
        Acc = (byte)(Acc | mem.data[address]);

        if (Acc == 0)
        {
            setFlag("Z");
        }
        if ((byte)(Acc & 128) == 128)
        {
            setFlag("N");
        }
    }

    private void JSR(Memory mem, ushort address)
    {
        byte returnAddressHi = (byte)((PC >> 8) & 0xFF);
        byte returnAddressLo = (byte)(PC & 0xFF);

        mem.data[SP + 0x100] = returnAddressHi;
        SP--;
        mem.data[SP + 0x100] = returnAddressLo;
        SP--;

        PC = address; 
    }

    private void RTS(Memory mem, ushort address)
    {
        SP++;
        ushort targetAddressLo = (ushort)(mem.data[SP + 0x100]);
        SP++;
        ushort targetAddressHi = (ushort)((mem.data[SP + 0x100] << 8));

        ushort targetAddress = (ushort)(targetAddressHi | targetAddressLo);

        PC = targetAddress;
        PC++;
    }

    private void PHP(Memory mem, ushort address)
    {
        mem.data[SP + 0x100] = ProcessorStatusReg;
        SP--;
    }

    private void PLP(Memory mem, ushort address)
    {
        SP++;
        ProcessorStatusReg = mem.data[SP + 0x100];
    }

    private void BRK(Memory mem, ushort address)
    {
        byte pcHi = (byte)(PC >> 8);
        mem.data[SP + 0x100] = pcHi;
        SP--;

        byte pcLo = (byte)(PC & 0xFF);
        mem.data[SP + 0x100] = pcLo;
        SP--;

        setFlag("B");
        mem.data[SP + 0x100] = ProcessorStatusReg;
        SP--;

        PC = (ushort)((mem.data[0xFFFD] << 8) | mem.data[0xFFFC]);
    }

    private void RTI(Memory mem, ushort address)
    {
        SP++;
        ProcessorStatusReg = mem.data[SP + 0x100];

        SP++;
        byte pcLo = mem.data[SP + 0x100];

        SP++;
        byte pcHi = mem.data[SP + 0x100];

        PC = (ushort)(pcHi << 8 | pcLo);
    }

    private void ADC(Memory mem, ushort address)
    {
        byte value = mem.data[address];
        ushort temp = (byte)(value + (byte)(getFlag("C")));

        if (temp > 255)
        {
            setFlag("C");
        }
        if (temp == 0)
        {
            setFlag("Z");
        }
        if (((~((byte)Acc ^ (byte)value) & ((byte)address ^ (byte)temp)) & 0x0080) != 0)
        {
            setFlag("V");
        }
        if ((temp & 0x80) != 0)
        {
            setFlag("N");
        }

        Acc += (byte)temp;

        PC++;
    }

    public void Reset(Memory mem)
    {
        mem.init();
        PC = (ushort)((mem.data[0xFFFD] << 8) | mem.data[0xFFFC]);
        SP = 0xFD;

        ProcessorStatusReg = 0;
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

    public void setFlag(string flag)
    {
        switch (flag)
        {
            case "N":
                ProcessorStatusReg |= 0x80;
                break;

            case "V":
                ProcessorStatusReg |= 0x40;
                break;

            case "B":
                ProcessorStatusReg |= 0x10;
                break;

            case "D":
                ProcessorStatusReg |= 0x08;
                break;

            case "I":
                ProcessorStatusReg |= 0x04;
                break;

            case "Z":
                ProcessorStatusReg |= 0x02;
                break;

            case "C":
                ProcessorStatusReg |= 0x01;
                break;
        }
    }

    public int getFlag(string flag)
    {
        int flagStatus = 0;
        byte mask;

        switch (flag)
        {
            case "N":

                mask = (byte)(1 << 7);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;

            case "V":

                mask = (byte)(1 << 6);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;

            case "B":

                mask = (byte)(1 << 4);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;

            case "D":
                mask = (byte)(1 << 3);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;

            case "I":
                mask = (byte)(1 << 2);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;

            case "Z":
                mask = (byte)(1 << 1);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;

            case "C":
                mask = (byte)(1 << 0);
                if ((mask & ProcessorStatusReg) != 0)
                {
                    flagStatus = 1;
                }
                break;
        }

        return flagStatus;
    }

    /* FETCH, DECODE, EXECUTE */
    public void Execute(Int32 cycles, Memory mem)
    {
        // One cycle needed per command
        while (cycles > 0)
        {
            currentOpcode = Read(mem);
            PC++;
            cycles--;

            Instruction CurrentInstruction = LookupTable[currentOpcode];
            ushort address = CurrentInstruction.AddressingMode(mem);

            CurrentInstruction.Operation(mem, address);
        }
    }
}