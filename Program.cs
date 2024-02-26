
class Program
{
    static async Task Main(string[] args)
    {
        var server = new WebSocketServer();

        // Start the server first
        _ = server.RunServerAsync();

        // Wait for the server to start
        await Task.Delay(1000); // Wait for 1 second

        // Keep the application running until the user presses Enter
        Console.WriteLine("Press Enter to quit...");
        Console.ReadLine();
    }
}