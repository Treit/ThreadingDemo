using System.IO.MemoryMappedFiles;

var commfile = @"task_demo_comm";
using var mmf = MemoryMappedFile.CreateOrOpen(commfile, 4);
using var view = mmf.CreateViewAccessor(0, 8);

int last = -1;

while (true)
{
    var live = view.ReadBoolean(4);
    var threads = view.ReadInt32(0);

    if (threads != last)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{threads}");
        Console.ResetColor();
        Console.Write(" running thread pool threads.");
        Console.CursorVisible = false;

        if (threads > 16 && threads < 100)
        {
            Console.Write("\n🤨");
        }
        else if (threads >= 100 && threads < 1000)
        {
            Console.Write("\n😬");
        }
        else if (threads >= 1000)
        {
            Console.Write("\n😱");
        }
        else if (!live || threads < 0)
        {
            Console.Clear();
            Console.Write("<Program not running>\n💀");
        }
        else
        {
            Console.Write("\n😊");
        }

        Console.ResetColor();
        last = threads;
    }

    Thread.Sleep(250);
}