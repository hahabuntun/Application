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
        private readonly ITCPServerService _tcpServerService;
        private CancellationTokenSource _tokenSource;
        private readonly IWindowManagerService _windowManagerService;
        private readonly ViewModelLocatorService _viewModelLocatorService;
        private AllMessagesViewModel _allMessagesViewModel;
        public ITCPServerService TCPServerService { get => _tcpServerService; }
        public ICommand CloseSelfCommand { get; }
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenAllMessagesCommand { get; }
        public SingleConnectionViewModel(ITCPServerService tcpServerService, IWindowManagerService windowManagerService, ViewModelLocatorService viewModelLocatorService)
        {
            _windowManagerService = windowManagerService;
            _viewModelLocatorService = viewModelLocatorService;
            _tcpServerService = tcpServerService;
            CloseSelfCommand = new RelayCommand(param => CloseSelf());
            StopServerCommand = new RelayCommand(param => StopServer(), (param) => !_tcpServerService.IsServerStopped);
            StartServerCommand = new RelayCommand(param =>  StartServer(), (param) => _tcpServerService.IsServerStopped);
            OpenFileCommand = new RelayCommand(param => OpenFile(), (param) => ((_tcpServerService.IsClientConnected) && (_tcpServerService.Message == null)));
            OpenAllMessagesCommand = new RelayCommand(param => OpenAllMessages(), (param) => true);
        }
        public void StartServer()
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _tokenSource.Token;
            Task.Run(() => _tcpServerService.StartServerAsync(cancellationToken), cancellationToken);
        }
        public void StopServer()
        {
            _tokenSource?.Cancel();
        }

        public void OpenAllMessages()
        {
            AllMessagesViewModel allMessagesViewModel = _viewModelLocatorService.AllMessagesViewModel;
            _allMessagesViewModel = allMessagesViewModel;
            _allMessagesViewModel.TCPServerService = _tcpServerService;
            _windowManagerService.ShowWindow(allMessagesViewModel);
        }

        public override void ClearResources()
        {
            _allMessagesViewModel?.CloseAction?.Invoke();
            StopServer();
        }
        public void OpenFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "XML files (*.xml)|*.xml";

                if (openFileDialog.ShowDialog() == true)
                {
                    Task.Run(() => ParseFile(openFileDialog.FileName));
                }
            }
            catch (Exception ex)
            {
            }
        }
        public async Task ParseFile(string filePath)
        {
            try
            {
                _tcpServerService.ErrorMessage = "";
                Message fileData = null;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNode messageNode = xmlDoc.SelectSingleNode("Message");

                if (messageNode != null)
                {
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
                        return;
                    }


                    if (!int.TryParse(messageNode.SelectSingleNode("Id")?.InnerText, out int id))
                    {
                        TCPServerService.ErrorMessage = "Не удалось преобразовать поле 'Id' в числовое значение.";
                        return; // Возвращаем, если не удалось преобразовать Id
                    }


                    if (!File.Exists(imagePath))
                    {
                        TCPServerService.ErrorMessage = "Неверный путь к картинке";
                        return;
                        
                    }
                    byte[] imageBytes = await File.ReadAllBytesAsync(imagePath, _tokenSource.Token);
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
                    
                    TCPServerService.Message = fileData;
                }
            }
            catch (Exception ex)
            {
                TCPServerService.ErrorMessage = ex.Message;
            }

        }

    }
}
