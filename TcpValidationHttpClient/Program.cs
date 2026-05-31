using System.Net.Http;

namespace TcpValidationHttpClient;

internal class Program
{
    private static readonly HttpClient HttpClient = new HttpClient();
    private const string BaseUrl = "http://localhost:8080";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== HTTP Клиент для TCP-сервера ===\n");

        while (true)
        {
            Console.Write("Введите путь (например, /user или /unknown) или 'exit' для выхода: ");
            string? path = Console.ReadLine();

            if (string.IsNullOrEmpty(path) || path.ToLower() == "exit")
                break;

            await SendRequestAsync(path);
        }
    }

    private static async Task SendRequestAsync(string path)
    {
        try
        {
            string url = $"{BaseUrl}{path}";
            HttpResponseMessage response = await HttpClient.GetAsync(url);
            
            string responseBody = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"\n=== РЕЗУЛЬТАТ ===");
            Console.WriteLine($"HTTP Status: {response.StatusCode}");
            Console.WriteLine($"Ответ сервера: {responseBody}");
            
            // Парсим ответ сервера (формат: статус|сообщение)
            var parts = responseBody.Split('|');
            if (parts.Length == 2)
            {
                Console.WriteLine($"Статус ошибки (DictionaryResponse): {parts[0]}");
                Console.WriteLine($"Сообщение: {parts[1]}");
            }
            Console.WriteLine("================\n");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n❌ Ошибка подключения: {ex.Message}");
            Console.WriteLine("Убедитесь, что сервер запущен на порту 8080\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Непредвиденная ошибка: {ex.Message}\n");
        }
    }
}