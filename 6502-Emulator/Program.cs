public class Program
{
    public static void Main(string[] args)
    {
        Memory Memory = new();
        CPU Cpu = new();
        Input Input = new();

        Cpu.Reset(Memory);
        Cpu.LoadLookupTable();

        Input.GetFile();
        Memory.LoadROM(Input);

        Cpu.Execute(Memory);

        Cpu.PrintPCSP();
        Cpu.PrintRegisters();
        Cpu.PrintFlags();
    }
}