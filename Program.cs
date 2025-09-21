using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

// Простые классы для хранения событий
public class InputEvent
{
    public string EventType { get; set; } = "";
    public long Timestamp { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int KeyCode { get; set; }
    public int MouseButton { get; set; }
    public bool IsPressed { get; set; }
}

public class MacroRecorder
{
    private TaskPoolGlobalHook? hook;
    private List<InputEvent> recordedEvents = new List<InputEvent>();
    private bool isRecording = false;
    private DateTime recordingStartTime;

    public void StartRecording()
    {
        recordedEvents.Clear();
        isRecording = true;
        recordingStartTime = DateTime.Now;
        
        hook = new TaskPoolGlobalHook();
        
        hook.MousePressed += OnMousePressed;
        hook.MouseReleased += OnMouseReleased;
        hook.MouseMoved += OnMouseMoved;
        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;
        
        Console.WriteLine("Запись начата! Нажмите Enter в этом окне чтобы остановить и сохранить...");
        hook.RunAsync();
    }

    public void StopRecording()
    {
        isRecording = false;
        hook?.Dispose();
        hook = null;
        Console.WriteLine("Запись остановлена.");
    }

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseMove",
            X = e.Data.X,
            Y = e.Data.Y,
            Timestamp = elapsed.Ticks
        });
        
        Console.WriteLine($"Мышь перемещена: X={e.Data.X}, Y={e.Data.Y}");
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseClick",
            X = e.Data.X,
            Y = e.Data.Y,
            MouseButton = (int)e.Data.Button,
            IsPressed = true,
            Timestamp = elapsed.Ticks
        });
        
        Console.WriteLine($"Кнопка мыши нажата: Кнопка={e.Data.Button}, X={e.Data.X}, Y={e.Data.Y}");
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseClick",
            X = e.Data.X,
            Y = e.Data.Y,
            MouseButton = (int)e.Data.Button,
            IsPressed = false,
            Timestamp = elapsed.Ticks
        });
        
        Console.WriteLine($"Кнопка мыши отпущена: Кнопка={e.Data.Button}, X={e.Data.X}, Y={e.Data.Y}");
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "KeyPress",
            KeyCode = (int)e.Data.KeyCode,
            IsPressed = true,
            Timestamp = elapsed.Ticks
        });
        
        Console.WriteLine($"Клавиша нажата: Код={(int)e.Data.KeyCode}, Символ={e.Data.KeyCode}");
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "KeyPress",
            KeyCode = (int)e.Data.KeyCode,
            IsPressed = false,
            Timestamp = elapsed.Ticks
        });
        
        Console.WriteLine($"Клавиша отпущена: Код={(int)e.Data.KeyCode}, Символ={e.Data.KeyCode}");
    }

    public void SaveToFile(string filename = "macro.json")
{
    try
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        string json = JsonSerializer.Serialize(recordedEvents, options);
        File.WriteAllText(filename, json);
        Console.WriteLine($"Макрос сохранен в файл: {filename} ({recordedEvents.Count} событий)");
        
        // Детальный вывод событий
        Console.WriteLine("\nДетальная информация о событиях:");
        int keyEvents = 0, mouseClicks = 0, mouseMoves = 0;
        
        foreach (var e in recordedEvents)
        {
            switch (e.EventType)
            {
                case "KeyPress":
                    keyEvents++;
                    Console.WriteLine($"Клавиша: Code={e.KeyCode}, Pressed={e.IsPressed}");
                    break;
                case "MouseClick":
                    mouseClicks++;
                    Console.WriteLine($"Клик: X={e.X}, Y={e.Y}, Button={e.MouseButton}, Pressed={e.IsPressed}");
                    break;
                case "MouseMove":
                    mouseMoves++;
                    Console.WriteLine($"Перемещение: X={e.X}, Y={e.Y}");
                    break;
            }
        }
        
        Console.WriteLine($"\nИтого: Клавиш={keyEvents}, Кликов={mouseClicks}, Перемещений={mouseMoves}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
    }
}

    public List<InputEvent>? LoadFromFile(string filename = "macro.json")
    {
        if (!File.Exists(filename))
        {
            Console.WriteLine($"Файл {filename} не существует!");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filename);
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var events = JsonSerializer.Deserialize<List<InputEvent>>(json, options);
            
            Console.WriteLine($"Загружено {events?.Count ?? 0} событий");
            if (events != null && events.Count > 0)
            {
                Console.WriteLine("Первые 5 загруженных событий:");
                for (int i = 0; i < Math.Min(5, events.Count); i++)
                {
                    var e = events[i];
                    Console.WriteLine($"{i}: {e.EventType}, X={e.X}, Y={e.Y}, Key={e.KeyCode}");
                }
            }
            
            return events;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            Console.WriteLine("Содержимое файла:");
            Console.WriteLine(File.ReadAllText(filename));
            return null;
        }
    }
}

public class MacroPlayer
{
    public async Task PlayEvents(List<InputEvent>? events, bool loop = false)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("Нет событий для воспроизведения!");
            return;
        }

        Console.WriteLine($"Воспроизведение {events.Count} событий...");
        Console.WriteLine("Переключитесь в нужное окно за 3 секунды...");
        await Task.Delay(3000);
        
        events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        
        do
        {
            long previousTime = events[0].Timestamp;

            foreach (var e in events)
            {
                long delay = e.Timestamp - previousTime;
                previousTime = e.Timestamp;
                
                if (delay > 0)
                {
                    int milliseconds = (int)(delay / TimeSpan.TicksPerMillisecond);
                    if (milliseconds > 0)
                    {
                        await Task.Delay(milliseconds);
                    }
                }

                switch (e.EventType)
                {
                    case "KeyPress":
                        Console.WriteLine($"Клавиша: {e.KeyCode}, Нажата: {e.IsPressed}");
                        
                        // Эмуляция клавиш
                        if (e.IsPressed)
                        {
                            keybd_event((byte)e.KeyCode, 0, KEYEVENTF_KEYDOWN, 0);
                        }
                        else
                        {
                            keybd_event((byte)e.KeyCode, 0, KEYEVENTF_KEYUP, 0);
                        }
                        break;
                        
                    case "MouseClick":
                        Console.WriteLine($"Мышь: X={e.X}, Y={e.Y}, Кнопка={e.MouseButton}, Нажата: {e.IsPressed}");
                        
                        // Сначала перемещаем мышь, потом кликаем
                        SetCursorPos(e.X, e.Y);
                        Thread.Sleep(10); // Небольшая задержка для стабильности
                        
                        if (e.MouseButton == 1) // Левая кнопка
                        {
                            if (e.IsPressed)
                                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)e.X, (uint)e.Y, 0, 0);
                            else
                                mouse_event(MOUSEEVENTF_LEFTUP, (uint)e.X, (uint)e.Y, 0, 0);
                        }
                        else if (e.MouseButton == 2) // Правая кнопка
                        {
                            if (e.IsPressed)
                                mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)e.X, (uint)e.Y, 0, 0);
                            else
                                mouse_event(MOUSEEVENTF_RIGHTUP, (uint)e.X, (uint)e.Y, 0, 0);
                        }
                        break;
                        
                    case "MouseMove":
                        Console.WriteLine($"Перемещение: X={e.X}, Y={e.Y}");
                        SetCursorPos(e.X, e.Y);
                        break;
                }
                
                // Небольшая пауза между событиями для стабильности
                await Task.Delay(10);
            }
            
            Console.WriteLine("Воспроизведение завершено.");
            
        } while (loop);
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
}

class Program
{
    static async Task Main(string[] args)
    {
        var recorder = new MacroRecorder();
        var player = new MacroPlayer();

        Console.WriteLine("Smart Macro Recorder");
        Console.WriteLine("1 - Начать запись");
        Console.WriteLine("2 - Воспроизвести последний макрос");
        Console.WriteLine("3 - Показать содержимое файла");
        Console.WriteLine("4 - Выход");

        while (true)
        {
            Console.Write("\nВыберите действие: ");
            var choice = Console.ReadKey().KeyChar;
            Console.WriteLine();

            switch (choice)
            {
                case '1':
                    recorder.StartRecording();
                    Console.WriteLine("Запись идет... Нажмите Enter в этом окне для остановки");
                    while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
                    recorder.StopRecording();
                    recorder.SaveToFile();
                    break;

                case '2':
                    var events = recorder.LoadFromFile();
                    if (events != null)
                    {
                        await player.PlayEvents(events, false);
                    }
                    break;

                case '3':
                    if (File.Exists("macro.json"))
                    {
                        Console.WriteLine("Содержимое macro.json:");
                        Console.WriteLine(File.ReadAllText("macro.json"));
                    }
                    else
                    {
                        Console.WriteLine("Файл macro.json не существует!");
                    }
                    break;

                case '4':
                    return;

                default:
                    Console.WriteLine("Неверный выбор!");
                    break;
            }
        }
    }
}  
