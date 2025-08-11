using System.IO;

public class Input
{
    // Parallel arrays to put addresses and data found in .txt file.
    public List<Int32> address { get; set; } = new();
    public List<byte> data { get; set; } = new();

    // Get user input for file to run, make sure its either .asm or .txt and use appropriate method
    public void GetFile()
    {
        while (true)
        {
            Console.WriteLine("Enter the absolute path of the file to run: ");
            string? path = Console.ReadLine();
            string? extension = Path.GetExtension(path);

            try
            {
                if (path == null)
                {
                    Console.WriteLine("Path cannot be null.");
                }
                else if (path == "test")
                {
                    // Parse test.txt from repo
                    Console.WriteLine("Parsing Test");
                    ParseTxtFile("../../../test.txt"); 
                    break;
                }
                else
                {
                    if (File.Exists(path))
                    {
                        if (extension == ".txt")
                        {
                            ParseTxtFile(path);
                            break;
                        }
                        else if (extension == ".asm")
                        {
                            // Use Asembler
                            break;
                        }
                        else
                        {
                            Console.WriteLine("File must be either a .txt or .asm file.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"File not found at location: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception raised: {ex.ToString()}");
            }
        }
    }

    private void ParseTxtFile(string path)
    {

        using (StreamReader reader = new StreamReader(path))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                bool isData = false;

                foreach (string token in line.Split(" "))
                {
                    if (isData)
                    {
                        // 16 at the end of these denotes what number base to use when converting
                        byte dataVal = Convert.ToByte(token.Replace("0x", ""), 16);
                        data.Add(dataVal);
                        isData = false;
                    }
                    else
                    {
                        Int32 addressVal = Convert.ToInt32(token.Replace("0x", ""), 16);
                        address.Add(addressVal);
                        isData = true;
                    }
                }
            }
        }
    }
}

