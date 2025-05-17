using System.Net;
using System.Net.Sockets;
using System.Text;
using Spectre.Console;

namespace IrcClient;

class Program
{
    static async Task Main()
    {
        // Ask user for server address
        string? server = AnsiConsole.Prompt(
            new TextPrompt<string>("[lime]Enter server address:[/] ")
                .PromptStyle("white")
                .Validate(server =>
                {
                    if (string.IsNullOrWhiteSpace(server))
                    {
                        return ValidationResult.Error("[red]Server address cannot be empty.[/]");
                    }
                    // Basic IP or hostname validation (you might want more robust validation)
                    /*if (!IPAddress.TryParse(server, out _) && !server.Contains('.'))
                    {
                        return ValidationResult.Error("[red]Invalid server address format.[/]");
                    }*/
                    return ValidationResult.Success();
                }));

        int port = 6667;
        string nick = "User" + new Random().Next(1000);
        string channel = "#mainC";

        using TcpClient client = new TcpClient();
        try
        {
            await client.ConnectAsync(server, port);
        }
        catch (SocketException ex)
        {
            AnsiConsole.Write(new Markup($"[bold red]Error:[/] Could not connect to [blue]{server}:{port}[/]. {ex.Message}\n"));
            return;
        }

        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // Register with server
        await writer.WriteLineAsync($"NICK {nick}");
        await writer.WriteLineAsync($"USER {nick} 0 * :C# IRC Client");

        AnsiConsole.Write(new Markup($"[green]Connected to[/] [blue]{server}:{port}[/]\n"));
        AnsiConsole.Write(new Markup($"[green]Your nick is[/] [yellow]{nick}[/]\n"));
        await writer.WriteLineAsync($"JOIN {channel}");
        AnsiConsole.Write(new Markup($"[green]Joining channel[/] [magenta]{channel}[/]\n"));

        // Handle server messages and user input concurrently
        Task serverTask = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        AnsiConsole.Write(new Markup("[red]Disconnected from server.[/]\n"));
                        break;
                    }

                    // Display server messages
                    if (line.Contains("PRIVMSG"))
                    {
                        string sender = line.Substring(1, line.IndexOf('!') - 1);
                        string message = line.Substring(line.IndexOf(':') + 1);

                        if (sender != nick)
                        {
                            AnsiConsole.Write(new Markup($"[[[blue]{sender}[/]]] {message}\n"));
                        }
                    }
                    else if (line.Contains("NOTICE"))
                    {
                        string noticeMessage = line.Substring(line.IndexOf(':') + 1);
                        AnsiConsole.Write(new Markup($"[italic grey]Notice:[/] {noticeMessage}\n"));
                    }
                    else if (line.Contains("001")) // RPL_WELCOME
                    {
                        string welcomeMessage = line.Substring(line.IndexOf(':') + 1);
                        AnsiConsole.Write(new Markup($"[green]{welcomeMessage}[/]\n"));
                    }
                    else if (line.Contains(" 353 ")) // RPL_NAMREPLY
                    {
                        int namesIndex = line.IndexOf(":", 1);
                        if (namesIndex != -1)
                        {
                            string users = line.Substring(namesIndex + 1);
                            AnsiConsole.Write(new Markup($"[yellow]Users in[/] [magenta]{channel}[/]: [lime]{users}[/]\n"));
                        }
                    }
                    else if (line.Contains("JOIN"))
                    {
                        string joinedUser = line.Substring(1, line.IndexOf('!') - 1);
                        if (joinedUser != nick)
                        {
                            AnsiConsole.Write(new Markup($"[green]{joinedUser}[/] [green]joined[/] [magenta]{channel}[/]\n"));
                        }
                    }
                    else if (line.Contains("PART"))
                    {
                        string partedUser = line.Substring(1, line.IndexOf('!') - 1);
                        if (partedUser != nick)
                        {
                            AnsiConsole.Write(new Markup($"[red]{partedUser}[/] [red]left[/] [magenta]{channel}[/]\n"));
                        }
                    }
                    else if (line.StartsWith("PING"))
                    {
                        await writer.WriteLineAsync($"PONG {line.Split(' ')[1]}");
                    }
                    else
                    {
                        AnsiConsole.Write(new Markup($"[grey]{line}[/]\n")); // Display other server messages in grey
                    }

                }
                catch (IOException)
                {
                    AnsiConsole.Write(new Markup("[red]Connection to server lost.[/]\n"));
                    break;
                }
                catch (Exception ex)
                {
                    AnsiConsole.Write(new Markup($"[bold red]Error reading from server:[/] {ex.Message}\n"));
                    break;
                }
            }
        });

        // Handle user input
        while (client.Connected)
        {
            var prompt = new TextPrompt<string>($"[[[yellow]{nick}[/]]]> ")
                .PromptStyle("white");
            string? input = AnsiConsole.Prompt(prompt);

            if (input == "/quit")
            {
                await writer.WriteLineAsync("QUIT :Bye");
                break;
            }
            else if (input.StartsWith("/nick "))
            {
                string[] parts = input.Split(' ', 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    nick = parts[1];
                    await writer.WriteLineAsync($"NICK {nick}");
                    AnsiConsole.Write(new Markup($"[green]Your nick is now[/] [yellow]{nick}[/]\n"));
                }
                else
                {
                    AnsiConsole.Write(new Markup("[red]Usage: /nick <new_nickname>[/]\n"));
                }
            }
            else if (input == "/list")
            {
                await writer.WriteLineAsync("LIST");
                AnsiConsole.Write(new Markup("[italic grey]Requesting channel list...[/]\n"));
            }
            else if (input.StartsWith("/join "))
            {
                string[] parts = input.Split(' ', 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    channel = parts[1];
                    await writer.WriteLineAsync($"JOIN {channel}");
                    AnsiConsole.Write(new Markup($"[green]Joining channel[/] [magenta]{channel}[/]\n"));
                }
                else
                {
                    AnsiConsole.Write(new Markup("[red]Usage: /join <channel_name>[/]\n"));
                }
            }
            else if (input.StartsWith("/msg "))
            {
                string[] parts = input.Split(' ', 3);
                if (parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[1]) && !string.IsNullOrWhiteSpace(parts[2]))
                {
                    string target = parts[1];
                    string message = parts[2];
                    await writer.WriteLineAsync($"PRIVMSG {target} :{message}");
                    AnsiConsole.Write(new Markup($"[italic grey]>[/] [blue]{nick}[/] [italic grey]to[/] [yellow]{target}[/]: {message}\n"));
                }
                else
                {
                    AnsiConsole.Write(new Markup("[red]Usage: /msg <user> <message>[/]\n"));
                }
            }
            else if (!string.IsNullOrEmpty(input))
            {
                await writer.WriteLineAsync($"PRIVMSG {channel} :{input}");
            }
        }

        await serverTask;
        client.Close();
        AnsiConsole.Write(new Markup("[grey]Connection closed.[/]\n"));
    }
}