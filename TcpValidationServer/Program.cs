using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpValidationServer;

internal class Program
{
    private const int Port = 8080;

    public static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        Console.WriteLine($"Сервер запущен на порту {Port}");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Task.Run(() => HandleClient(client));
        }
    }

    private static void HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Запрос: {request}");
            
            var (status, message) = ValidateAndProcess(request);

            string response = $"{status}|{message}";
            byte[] responseData = Encoding.UTF8.GetBytes(response);
            stream.Write(responseData, 0, responseData.Length);
        }
    }

    private static (DictionaryResponse status, string message) ValidateAndProcess(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return (DictionaryResponse.BadRequest, "Ошибка: пустой запрос.");
        }

        if (!request.StartsWith("GET "))
        {
            return (DictionaryResponse.BadRequest, "Ошибка: поддерживается только метод GET");
        }

        string path = request.Substring(4).Trim();

        if (path == "/user")
        {
            return (DictionaryResponse.OK, "Данные пользователя: Иван Иванов");
        }

        return (DictionaryResponse.NotFoundResource, $"Ресурс '{path}' не найден");
    }
}

