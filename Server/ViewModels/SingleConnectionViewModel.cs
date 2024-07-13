﻿using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Server.Commands;
using Server.Models;
using Server.Services;
using Server.Services.Server;
using System.IO;
using System.Windows.Input;
using System.Xml;

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
                    Task.Run(() => ParseFile(openFileDialog.FileName)); //парсим файл
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
    }
}
