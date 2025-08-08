
public class Memory
{
    const int MAX_MEMORY = 1024*64; // 64KB of memory

    public byte[] data { get; set; } = new byte[MAX_MEMORY];

    public void init()
    {
        // Initialize memory with zeros
        for (int i = 0; i < MAX_MEMORY; i++)
        {
            data[i] = 0;
        }
    }
}