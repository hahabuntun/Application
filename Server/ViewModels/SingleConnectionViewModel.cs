using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Server.Commands;
using Server.Models;
using Server.Services;
using Server.Services.Server;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Server.ViewModels
{
    public class SingleConnectionViewModel : ViewModelBase
    {
        private readonly ILogger<SingleConnectionViewModel> _logger;
        private readonly ITCPServerService _tcpServerService;
        private CancellationTokenSource _tokenSource;
        private readonly IWindowManagerService _windowManagerService;
        private readonly ViewModelLocatorService _viewModelLocatorService;
        private AllMessagesViewModel _allMessagesViewModel;
        public ITCPServerService TCPServerService { get => _tcpServerService; }


        public ICommand CloseSelfCommand { get; } // команда закрытия себя
        public ICommand StartServerCommand { get; } //команда старта сервера
        public ICommand StopServerCommand { get; } //команда остановки сервера
        public ICommand OpenFileCommand { get; } //команда открытия файла
        public ICommand OpenAllMessagesCommand { get; } //команда открытия всех сообщений сессии


        public SingleConnectionViewModel(ILogger<SingleConnectionViewModel> logger, ITCPServerService tcpServerService, IWindowManagerService windowManagerService, ViewModelLocatorService viewModelLocatorService)
        {
            _logger = logger;
            _windowManagerService = windowManagerService;
            _viewModelLocatorService = viewModelLocatorService;
            _allMessagesViewModel = _viewModelLocatorService.AllMessagesViewModel;
            _tcpServerService = tcpServerService;
            _allMessagesViewModel.TCPServerService = _tcpServerService;
            _tcpServerService.messageSent += _allMessagesViewModel.AddMessage;
            CloseSelfCommand = new RelayCommand(param => CloseSelf());
            StopServerCommand = new RelayCommand(param => StopServer(), (param) => !_tcpServerService.IsServerStopped);
            StartServerCommand = new RelayCommand(param =>  StartServer(), (param) => _tcpServerService.IsServerStopped);
            OpenFileCommand = new RelayCommand(param => OpenFile(), (param) => ((_tcpServerService.Clients.Count > 0) && (_tcpServerService.Message == null)));
            OpenAllMessagesCommand = new RelayCommand(param => OpenAllMessages(), (param) => true);
        }


        /// <summary>
        /// Начало работы сервера
        /// </summary>
        public void StartServer()
        {
            _logger.LogInformation("Вызвана команда start");
            _tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _tokenSource.Token;
            Task.Run(() => _tcpServerService.StartServerAsync(cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Остановка работы сервера
        /// </summary>
        public void StopServer()
        {
            _logger.LogInformation("Вызвана команда stop");
            _tokenSource?.Cancel();
        }


        /// <summary>
        /// Открытия окна всех сообщений
        /// </summary>
        public void OpenAllMessages()
        {
            _logger.LogInformation("Вызвана команда открытия всех сообщений");
            _windowManagerService.ShowWindow(_allMessagesViewModel);
        }

        public event Action<SingleConnectionViewModel> Closing;
        /// <summary>
        /// Очищение ресурсов окна
        /// </summary>
        public override void ClearResources()
        {
            _logger.LogInformation("Очистка ресурсов");
            _allMessagesViewModel?.CloseAction?.Invoke();
            StopServer();
            Closing(this);
        }


        /// <summary>
        /// Открытие файла, содержимое которого необходимо спарсить
        /// </summary>
        public void OpenFile()
        {
            _logger.LogInformation("Вызвана команда открытия файла");
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "XML files (*.xml)|*.xml";

                if (openFileDialog.ShowDialog() == true)
                {
                    Task.Run(() => ParseXmlFile(openFileDialog.FileName)); //парсим файл
                }
            }
            catch (Exception ex)
            {
            }
        }


        /// <summary>
        /// Функция парсинга файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task ParseFile(string filePath)
        {
            _logger.LogInformation("Начинаем парсить файл");
            try
            {
                _tcpServerService.ErrorMessage = "";
                Message fileData = null;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath); // загружаем файл
                _logger.LogInformation("Файл загружены");

                XmlNode messageNode = xmlDoc.SelectSingleNode("Message"); //ищем в нем узел messages

                if (messageNode != null)
                {
                    //проверяем наличие необходимых данных
                    string? formatVersion = messageNode.SelectSingleNode("FormatVersion")?.InnerText;
                    string? from = messageNode.SelectSingleNode("From")?.InnerText;
                    string? to = messageNode.SelectSingleNode("To")?.InnerText;
                    string? color = messageNode.SelectSingleNode("Color")?.InnerText;
                    string? text = messageNode.SelectSingleNode("Text")?.InnerText;
                    string? imagePath = messageNode.SelectSingleNode("ImagePath")?.InnerText;
                    
                    if (string.IsNullOrEmpty(formatVersion) ||
                        string.IsNullOrEmpty(from) || 
                        string.IsNullOrEmpty(to) || 
                        string.IsNullOrEmpty(color) ||
                        string.IsNullOrEmpty(text) ||
                        string.IsNullOrEmpty(imagePath))
                    {
                        TCPServerService.ErrorMessage = "Не все обязательные поля заполнены в файле.";
                        _logger.LogWarning("Не все обязательные поля заполнены в файле.");
                        return;
                    }

                    //проверяем поле id
                    if (!int.TryParse(messageNode.SelectSingleNode("Id")?.InnerText, out int id))
                    {
                        TCPServerService.ErrorMessage = "Не удалось преобразовать поле 'Id' в числовое значение.";
                        _logger.LogWarning("Не удалось преобразовать поле 'Id' в числовое значение.");
                        return; // Возвращаем, если не удалось преобразовать Id
                    }

                    //проверяем существует ли imagePath
                    if (!File.Exists(imagePath))
                    {
                        TCPServerService.ErrorMessage = "Неверный путь к картинке";
                        _logger.LogWarning("Неверный путь к картинке");
                        return;
                        
                    }

                    //считываем картинку
                    _logger.LogInformation("Читаем картинку с диска");
                    byte[] imageBytes = await File.ReadAllBytesAsync(imagePath, _tokenSource.Token);
                    _logger.LogInformation("Картинка прочитана");
                    //создаем сообщение
                    fileData = new Message()
                    {
                        Id = id,
                        FormatVersion = formatVersion,
                        From = from,
                        To = to,
                        Color = color,
                        Text = text,
                        ImagePath = imagePath,
                        ImageBytes = imageBytes
                    };
                    TCPServerService.Message = fileData; //обновляем сообщение на сервере
                    TCPServerService.MessageFilled?.Cancel();
                }
            }
            catch (Exception ex)
            {
                TCPServerService.ErrorMessage = ex.Message;
                _logger.LogWarning(ex.Message);

            }
        }


        public async Task ParseXmlFile(string filePath)
        {
            try
            {
                _tcpServerService.ErrorMessage = "";
                Message fileData = null;
                // Load the XML document from file
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                // Get the root element
                XmlElement root = doc.DocumentElement;
                if (root == null)
                {
                    _logger.LogWarning("No root element found in XML.");
                    return;
                }

                // Get the first Message element
                XmlNodeList messageList = root.GetElementsByTagName("Message");
                if (messageList.Count == 0)
                {
                    _logger.LogWarning("No Message elements found in XML.");
                    return;
                }

                XmlElement messageElement = (XmlElement)messageList[0]; // Take the first message if there are multiple

                // Extract attributes from Message element
                string formatVersion = GetAttributeOrDefault(messageElement, "FormatVersion");
                string from = GetAttributeOrDefault(messageElement, "from");
                string to = GetAttributeOrDefault(messageElement, "to");

                if (string.IsNullOrEmpty(formatVersion) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                {
                    _logger.LogWarning("Missing required attributes in Message element.");
                    return;
                }

                // Extract msg element details
                XmlNodeList msgList = messageElement.GetElementsByTagName("msg");
                if (msgList.Count == 0)
                {
                    _logger.LogWarning("No msg element found in Message.");
                    return;
                }

                XmlElement msgElement = (XmlElement)msgList[0]; // Assuming only one msg element

                // Extract id attribute from msg
                string idString = GetAttributeOrDefault(msgElement, "id");
                if (string.IsNullOrEmpty(idString) || !int.TryParse(idString, out int id))
                {
                    _logger.LogWarning("Invalid or missing 'id' attribute in msg element.");
                    return;
                }

                // Extract text element from msg
                XmlNodeList textList = msgElement.GetElementsByTagName("text");
                if (textList.Count == 0)
                {
                    _logger.LogWarning("No text element found in msg.");
                    return;
                }

                XmlElement textElement = (XmlElement)textList[0]; // Assuming only one text element
                string textValue = textElement.InnerText;
                string textColor = GetAttributeOrDefault(textElement, "color");

                if (string.IsNullOrEmpty(textColor))
                {
                    _logger.LogWarning("No 'color' attribute found in text element.");
                    return;
                }
                if (!textColor.StartsWith("#"))
                {
                    textColor = "#" + textColor; // Add '#' prefix if missing
                }

                if (string.IsNullOrEmpty(textValue))
                {
                    _logger.LogWarning("No text content found in text element.");
                    return;
                }

                // Extract image element from msg
                XmlNodeList imageList = msgElement.GetElementsByTagName("image");
                if (imageList.Count == 0)
                {
                    _logger.LogWarning("No image element found in msg.");
                    return;
                }

                XmlElement imageElement = (XmlElement)imageList[0]; // Assuming only one image element
                string imageData = imageElement.InnerText;

                if (string.IsNullOrEmpty(imageData))
                {
                    _logger.LogWarning("No image data found in image element.");
                    return;
                }

                // Save image data to disk and get its path
                byte[] imageBytes = Convert.FromBase64String(imageData);
                string imagePath = await SaveImageToFileAsync(imageBytes);

                if (string.IsNullOrEmpty(imagePath))
                {
                    _logger.LogWarning("Failed to save image to disk.");
                    return;
                }
                fileData = new Message()
                {
                    Id = id,
                    FormatVersion = formatVersion,
                    From = from,
                    To = to,
                    Color = textColor,
                    Text = textValue,
                    ImagePath = imagePath,
                    ImageBytes = imageBytes
                };
                TCPServerService.Message = fileData; //обновляем сообщение на сервере
                TCPServerService.MessageFilled?.Cancel();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing XML file: {ex.Message}");
            }
        }

        private string GetAttributeOrDefault(XmlElement element, string attributeName)
        {
            return element.HasAttribute(attributeName) ? element.GetAttribute(attributeName) : null;
        }

        private async Task<string> SaveImageToFileAsync(byte[] imageBytes)
        {
            try
            {
                // Generate unique file name
                string fileName = $"image_{DateTime.Now:yyyyMMddHHmmssfff}.jpg";

                // Set the directory path where you want to save the image
                string directoryPath = "C:/images"; // Replace with your directory path
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Combine directory path and file name
                string filePath = Path.Combine(directoryPath, fileName);

                // Write bytes to file
                await File.WriteAllBytesAsync(filePath, imageBytes);

                // Return the file path
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image to file: {ex.Message}");
                return null;
            }
        }
    }
}
