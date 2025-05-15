using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IrcClient;

class Program
{
    static async Task Main()
    {
        string server = "localhost"; // Change to your server's IP (e.g., "192.168.1.100")
        int port = 6667;
        string nick = "User" + new Random().Next(1000);
        string channel = "#mainC";

        using TcpClient client = new TcpClient();
        await client.ConnectAsync(server, port);
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // Register with server
        await writer.WriteLineAsync($"NICK {nick}");
        await writer.WriteLineAsync($"USER {nick} 0 * :C# IRC Client");

        // Handle server messages and user input concurrently
        Task serverTask = Task.Run(async () =>
        {
            while (true)
            { 
                string? line = await reader.ReadLineAsync();
                if (line == null) { Console.WriteLine("Disconnected"); break; }

                // Display server messages
                if (line.Contains("PRIVMSG"))
                {
                    string sender = line.Substring(1, line.IndexOf('!') - 1);
                    string message = line.Substring(line.IndexOf(':') + 1);
                    
                    if(sender != nick)
                        Console.WriteLine($"<{sender}> {message}");
                }
                else if (line.Contains("NOTICE") || line.Contains("001"))
                {
                    Console.WriteLine(line.Substring(line.IndexOf(':') + 1));
                }
                else if (line.Contains(" 353 "))
                {
                    // Example: ":server 353 nick = #mainC :user1 user2 user3"
                    int namesIndex = line.IndexOf(":", 1);
                    if (namesIndex != -1)
                    {
                        string users = line.Substring(namesIndex + 1);
                        Console.WriteLine($"Users in {channel}: {users}");
                    }
                }
                // Handle PING
                if (line.StartsWith("PING"))
                {
                    await writer.WriteLineAsync($"PONG {line.Split(' ')[1]}");
                }
                Console.Write($"<{nick}> ");

            }
        });

        // Handle user input
        while (true)
        {
            Console.Write($"<{nick}> ");
            string? input = Console.ReadLine();
            if (input == "/quit")
            {
                await writer.WriteLineAsync("QUIT :Bye");
                break;
            }

            if (input.StartsWith("/nick"))
            {
                string[] parts = input.Split(' ', 2);
                nick = parts[1];
                await writer.WriteLineAsync($"NICK {nick}");
            }
            if (input == "/list")
            {
                await writer.WriteLineAsync("LIST");
            }
            if (input.StartsWith("/msg "))
            {
                string[] parts = input.Split(' ', 3);
                if (parts.Length < 3)
                {
                    Console.WriteLine("Usage: /msg <user> <message>");
                    continue;
                }
                string target = parts[1];
                string message = parts[2];
                await writer.WriteLineAsync($"PRIVMSG {target} :{message}");
                break;
            }
            if (!string.IsNullOrEmpty(input))
            {
                await writer.WriteLineAsync($"PRIVMSG {channel} :{input}");
            }
        }

        await serverTask;
        client.Close();
    }

}