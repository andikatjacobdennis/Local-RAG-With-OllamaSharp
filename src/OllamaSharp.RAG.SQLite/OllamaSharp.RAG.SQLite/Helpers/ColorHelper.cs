namespace OllamaSharp.RAG.SQLite.Helpers;

public static class ColorHelper
{
    // Store original console colors
    private static readonly ConsoleColor OriginalForeground = Console.ForegroundColor;
    private static readonly ConsoleColor OriginalBackground = Console.BackgroundColor;

    // ASCII dividers for different states (no Unicode/emojis)
    private static readonly Dictionary<MessageType, string> Dividers = new()
    {
        { MessageType.Success, "[OK]" },
        { MessageType.Error, "[ERR]" },
        { MessageType.Warning, "[WARN]" },
        { MessageType.Info, "[INFO]" },
        { MessageType.Question, "[?]" },
        { MessageType.Thinking, "[*]" },
        { MessageType.Processing, "[>]" },
        { MessageType.Search, "[SEARCH]" },
        { MessageType.Document, "[DOC]" },
        { MessageType.Database, "[DB]" },
        { MessageType.AI, "[AI]" },
        { MessageType.Complete, "[DONE]" },
        { MessageType.Book, "[BOOK]" },
        { MessageType.Magnifying, "[MAG]" },
        { MessageType.Thought, "[THOUGHT]" },
        { MessageType.Speech, "[SAY]" },
        { MessageType.Star, "[*]" }
    };

    // Color schemes for different message types
    private static readonly Dictionary<MessageType, ConsoleColor> Colors = new()
    {
        { MessageType.Success, ConsoleColor.Green },
        { MessageType.Error, ConsoleColor.Red },
        { MessageType.Warning, ConsoleColor.Yellow },
        { MessageType.Info, ConsoleColor.Cyan },
        { MessageType.Question, ConsoleColor.Cyan },
        { MessageType.Thinking, ConsoleColor.Magenta },
        { MessageType.Processing, ConsoleColor.Yellow },
        { MessageType.Search, ConsoleColor.DarkYellow },
        { MessageType.Document, ConsoleColor.Gray },
        { MessageType.Database, ConsoleColor.DarkGray },
        { MessageType.AI, ConsoleColor.Magenta },
        { MessageType.Complete, ConsoleColor.Green },
        { MessageType.Book, ConsoleColor.DarkCyan },
        { MessageType.Magnifying, ConsoleColor.DarkYellow },
        { MessageType.Thought, ConsoleColor.DarkMagenta },
        { MessageType.Speech, ConsoleColor.DarkCyan },
        { MessageType.Star, ConsoleColor.Yellow }
    };

    // Animation frames for loading states (ASCII only)
    private static readonly string[] SpinnerFrames = { "/", "-", "\\", "|", "/", "-", "\\", "|", "/", "-" };
    private static readonly string[] DotsFrames = { "   ", ".  ", ".. ", "..." };
    private static readonly string[] ThinkingFrames = { " o ", " o.", "o..", "..." };

    public static void Write(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public static void WriteLine(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }

    public static void WriteDivider(char lineChar, int length, ConsoleColor color = ConsoleColor.DarkGray)
    {
        WriteLine(new string(lineChar, length), color);
    }

    public static void WriteMessage(MessageType type, string message, bool newLine = true)
    {
        var divider = Dividers.GetValueOrDefault(type, "[ ]");
        var color = Colors.GetValueOrDefault(type, ConsoleColor.White);

        if (newLine)
            WriteLine($"{divider} {message}", color);
        else
            Write($"{divider} {message}", color);
    }

    public static void WriteColoredMessage(string message, ConsoleColor color, string divider = "", bool newLine = true)
    {
        var dividerText = string.IsNullOrEmpty(divider) ? "" : $"{divider} ";

        if (newLine)
            WriteLine($"{dividerText}{message}", color);
        else
            Write($"{dividerText}{message}", color);
    }

    public static void WriteSuccess(string message, bool newLine = true)
        => WriteMessage(MessageType.Success, message, newLine);

    public static void WriteError(string message, bool newLine = true)
        => WriteMessage(MessageType.Error, message, newLine);

    public static void WriteWarning(string message, bool newLine = true)
        => WriteMessage(MessageType.Warning, message, newLine);

    public static void WriteInfo(string message, bool newLine = true)
        => WriteMessage(MessageType.Info, message, newLine);

    public static void WriteThinking(string message, bool newLine = true)
        => WriteMessage(MessageType.Thinking, message, newLine);

    public static void WriteAIMessage(string message, bool newLine = true)
        => WriteMessage(MessageType.AI, message, newLine);

    public static void WriteHeader(string title)
    {
        WriteLine(new string('=', 50), ConsoleColor.Cyan);
        WriteLine($" {title}", ConsoleColor.Cyan);
        WriteLine(new string('=', 50), ConsoleColor.Cyan);
    }

    public static async Task WriteAnimatedThinkingAsync(string message, int durationMs = 1000)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        var startTime = DateTime.Now;
        var frameIndex = 0;

        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.Write($"\r{ThinkingFrames[frameIndex % ThinkingFrames.Length]} {message}");
            frameIndex++;
            await Task.Delay(100);
        }

        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
        Console.ForegroundColor = originalColor;
    }

    public static async Task WriteSpinnerAsync(string message, int durationMs = 1500)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;

        var startTime = DateTime.Now;
        var frameIndex = 0;

        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.Write($"\r{SpinnerFrames[frameIndex % SpinnerFrames.Length]} {message}");
            frameIndex++;
            await Task.Delay(80);
        }

        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
        Console.ForegroundColor = originalColor;
    }

    public static async Task WriteProgressAsync(string message, int progress, int total, string prefix = "")
    {
        var percentage = (int)((double)progress / total * 100);
        var barLength = 30;
        var filledLength = (int)((double)progress / total * barLength);

        var bar = new string('#', filledLength) + new string('-', barLength - filledLength);

        var color = percentage switch
        {
            < 30 => ConsoleColor.Red,
            < 70 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };

        Write($"\r{prefix} [{bar}] {percentage}% {message}", color);

        if (progress == total)
        {
            await Task.Delay(200);
            Console.WriteLine();
        }
    }

    public static void WriteTable(Dictionary<string, string> data, int padding = 2)
    {
        if (!data.Any()) return;

        var maxKeyLength = data.Keys.Max(k => k.Length);
        var maxValueLength = data.Values.Max(v => v.Length);

        WriteLine(new string('=', maxKeyLength + maxValueLength + padding * 2 + 4), ConsoleColor.DarkGray);

        foreach (var (key, value) in data)
        {
            Write(" ", ConsoleColor.DarkGray);
            Write(key.PadRight(maxKeyLength), ConsoleColor.Cyan);
            Write("  ", ConsoleColor.DarkGray);
            Write(value.PadRight(maxValueLength), ConsoleColor.White);
            WriteLine(" ", ConsoleColor.DarkGray);
        }

        WriteLine(new string('=', maxKeyLength + maxValueLength + padding * 2 + 4), ConsoleColor.DarkGray);
    }

    public static void WriteHighlighted(string text, string highlight, ConsoleColor highlightColor = ConsoleColor.Yellow)
    {
        var parts = text.Split(new[] { highlight }, StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            Write(parts[i], ConsoleColor.White);

            if (i < parts.Length - 1)
                Write(highlight, highlightColor);
        }
    }

    public static void ClearCurrentLine()
    {
        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
    }

    public static void WriteBanner(string[] lines, ConsoleColor borderColor = ConsoleColor.Cyan, ConsoleColor textColor = ConsoleColor.White)
    {
        var maxLength = lines.Max(l => l.Length);
        var border = new string('*', maxLength + 4);

        WriteLine(border, borderColor);
        foreach (var line in lines)
        {
            Write("* ", borderColor);
            Write(line.PadRight(maxLength), textColor);
            WriteLine(" *", borderColor);
        }
        WriteLine(border, borderColor);
    }
}

public enum MessageType
{
    Success,
    Error,
    Warning,
    Info,
    Question,
    Thinking,
    Processing,
    Search,
    Document,
    Database,
    AI,
    Complete,
    Book,
    Magnifying,
    Thought,
    Speech,
    Star
}