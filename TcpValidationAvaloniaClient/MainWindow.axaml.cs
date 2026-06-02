using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TcpValidationAvaloniaClient;

public partial class MainWindow : Window
{
    // TCP сервер (ДЗ 1)
    private const string TcpServer = "127.0.0.1";
    private const int TcpPort = 8080;
    
    // HTTP сервер книг (ДЗ 2)
    private static readonly HttpClient HttpClient = new HttpClient();
    private const string HttpBaseUrl = "http://localhost:5161";
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Подписываемся на события TCP
        TcpSendButton.Click += OnTcpSendClick;
        
        // Подписываемся на события HTTP
        HttpGetAllButton.Click += OnHttpGetAllClick;
        HttpGetByIdButton.Click += OnHttpGetByIdClick;
        HttpPostButton.Click += OnHttpPostClick;
        HttpPutButton.Click += OnHttpPutClick;
        HttpDeleteButton.Click += OnHttpDeleteClick;
    }
    
    // ========== TCP (ДЗ 1) ==========
    private async void OnTcpSendClick(object? sender, RoutedEventArgs e)
    {
        string request = TcpRequestTextBox.Text?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(request))
        {
            TcpResultTextBlock.Text = "Ошибка: введите запрос";
            TcpStatusTextBlock.Text = "Статус: Пустой запрос";
            return;
        }
        
        TcpSendButton.IsEnabled = false;
        TcpResultTextBlock.Text = "Отправка запроса...";
        TcpStatusTextBlock.Text = $"Статус: Отправка '{request}'";
        
        try
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(TcpServer, TcpPort);
            
            using NetworkStream stream = client.GetStream();
            
            byte[] requestData = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(requestData, 0, requestData.Length);
            
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            var parts = response.Split('|');
            if (parts.Length == 2)
            {
                TcpResultTextBlock.Text = $"Ответ сервера: {response}\n\n" +
                                           $"Статус (DictionaryResponse): {parts[0]}\n" +
                                           $"Сообщение: {parts[1]}";
                
                TcpStatusTextBlock.Text = parts[0] switch
                {
                    "OK" => $"Статус: ✓ {parts[0]}",
                    "BadRequest" => $"Статус: ✗ {parts[0]}",
                    "NotFoundResource" => $"Статус: ✗ {parts[0]}",
                    _ => $"Статус: {parts[0]}"
                };
            }
            else
            {
                TcpResultTextBlock.Text = $"Неожиданный ответ: {response}";
                TcpStatusTextBlock.Text = "Статус: Ошибка формата ответа";
            }
        }
        catch (Exception ex)
        {
            TcpResultTextBlock.Text = $"Ошибка подключения: {ex.Message}\n\n" +
                                       "Убедитесь, что TCP сервер запущен на порту 8080";
            TcpStatusTextBlock.Text = "Статус: Сервер не доступен";
        }
        finally
        {
            TcpSendButton.IsEnabled = true;
        }
    }
    
    // ========== HTTP (ДЗ 2) ==========
    private async void OnHttpGetAllClick(object? sender, RoutedEventArgs e)
    {
        await SendHttpRequestAsync("GET", "/book");
    }
    
    private async void OnHttpGetByIdClick(object? sender, RoutedEventArgs e)
    {
        string id = HttpIdTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(id))
        {
            HttpResultTextBlock.Text = "Ошибка: введите ID книги";
            return;
        }
        await SendHttpRequestAsync("GET", $"/book/{id}");
    }
    
    private async void OnHttpPostClick(object? sender, RoutedEventArgs e)
    {
        string name = HttpNameTextBox.Text?.Trim() ?? "";
        string author = HttpAuthorTextBox.Text?.Trim() ?? "";
        string description = HttpDescriptionTextBox.Text?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(author))
        {
            HttpResultTextBlock.Text = "Ошибка: название и автор обязательны";
            return;
        }
        
        var book = new { name, author, description };
        string json = JsonSerializer.Serialize(book);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            HttpResponseMessage response = await HttpClient.PostAsync($"{HttpBaseUrl}/book", content);
            string result = await response.Content.ReadAsStringAsync();
            HttpResultTextBlock.Text = $"Статус: {response.StatusCode}\n\nОтвет: {FormatJson(result)}";
        }
        catch (Exception ex)
        {
            HttpResultTextBlock.Text = $"Ошибка: {ex.Message}";
        }
    }
    
    private async void OnHttpPutClick(object? sender, RoutedEventArgs e)
    {
        string id = HttpIdTextBox.Text?.Trim() ?? "";
        string name = HttpNameTextBox.Text?.Trim() ?? "";
        string author = HttpAuthorTextBox.Text?.Trim() ?? "";
        string description = HttpDescriptionTextBox.Text?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(author))
        {
            HttpResultTextBlock.Text = "Ошибка: ID, название и автор обязательны";
            return;
        }
        
        var book = new { name, author, description };
        string json = JsonSerializer.Serialize(book);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            HttpResponseMessage response = await HttpClient.PutAsync($"{HttpBaseUrl}/book/{id}", content);
            string result = await response.Content.ReadAsStringAsync();
            HttpResultTextBlock.Text = $"Статус: {response.StatusCode}\n\nОтвет: {FormatJson(result)}";
        }
        catch (Exception ex)
        {
            HttpResultTextBlock.Text = $"Ошибка: {ex.Message}";
        }
    }
    
    private async void OnHttpDeleteClick(object? sender, RoutedEventArgs e)
    {
        string id = HttpIdTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(id))
        {
            HttpResultTextBlock.Text = "Ошибка: введите ID книги";
            return;
        }
        await SendHttpRequestAsync("DELETE", $"/book/{id}");
    }
    
    private async Task SendHttpRequestAsync(string method, string path)
    {
        try
        {
            HttpResponseMessage response;
            switch (method.ToUpper())
            {
                case "GET":
                    response = await HttpClient.GetAsync($"{HttpBaseUrl}{path}");
                    break;
                case "DELETE":
                    response = await HttpClient.DeleteAsync($"{HttpBaseUrl}{path}");
                    break;
                default:
                    HttpResultTextBlock.Text = $"Неподдерживаемый метод: {method}";
                    return;
            }
            
            string content = await response.Content.ReadAsStringAsync();
            HttpResultTextBlock.Text = $"Метод: {method} {path}\n\n" +
                                       $"Статус: {response.StatusCode}\n\n" +
                                       $"Ответ: {FormatJson(content)}";
        }
        catch (Exception ex)
        {
            HttpResultTextBlock.Text = $"Ошибка: {ex.Message}\n\n" +
                                       "Убедитесь, что HTTP сервер запущен на порту 5161";
        }
    }
    
    private string FormatJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "(пусто)";
        
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}