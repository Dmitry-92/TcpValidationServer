using System.Net.Sockets;
using System.Text;

namespace TcpValidationHttpClient;

internal class Program
{
    private const string Server = "127.0.0.1";
    private const int Port = 8080;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== TCP Клиент для TCP-сервера ===\n");

        while (true)
        {
            Console.Write("Введите запрос (например, GET /user) или 'exit' для выхода: ");
            string? request = Console.ReadLine();

            if (string.IsNullOrEmpty(request) || request.ToLower() == "exit")
                break;

            await SendRequestAsync(request);
        }
    }

    private static async Task SendRequestAsync(string request)
    {
        try
        {
            using TcpClient client = new TcpClient(Server, Port);
            using NetworkStream stream = client.GetStream();

            byte[] requestData = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(requestData, 0, requestData.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"\n=== РЕЗУЛЬТАТ ===");
            Console.WriteLine($"Ответ сервера: {response}");

            var parts = response.Split('|');
            if (parts.Length == 2)
            {
                Console.WriteLine($"Статус (DictionaryResponse): {parts[0]}");
                Console.WriteLine($"Сообщение: {parts[1]}");
            }
            Console.WriteLine("================\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Ошибка: {ex.Message}\n");
        }
    }
}