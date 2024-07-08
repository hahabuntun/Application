using Microsoft.Win32;
using Server.Commands;
using Server.Models;
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
        public ITCPServerService TCPServerService { get => _tcpServerService; }
        public ICommand CloseSelfCommand { get; }
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand OpenFileCommand { get; }
        public SingleConnectionViewModel(ITCPServerService tcpServerService)
        {
            _tcpServerService = tcpServerService;
            CloseSelfCommand = new RelayCommand(param => CloseSelf());
            StopServerCommand = new RelayCommand(param => StopServer(), (param) => !_tcpServerService.IsServerStopped);
            StartServerCommand = new RelayCommand(param =>  StartServer(), (param) => _tcpServerService.IsServerStopped);
            OpenFileCommand = new RelayCommand(param => OpenFile(), (param) => ((_tcpServerService.IsClientConnected) && (_tcpServerService.Message == null)));
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
        public override void ClearResources()
        {
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
                Message fileData = null;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNode messageNode = xmlDoc.SelectSingleNode("Message");

                if (messageNode != null)
                {
                    fileData = new Message();
                    fileData.FormatVersion = messageNode.SelectSingleNode("FormatVersion")?.InnerText;
                    fileData.From = messageNode.SelectSingleNode("From")?.InnerText;
                    fileData.To = messageNode.SelectSingleNode("To")?.InnerText;
                    fileData.Id = int.Parse(messageNode.SelectSingleNode("Id")?.InnerText ?? "0");
                    fileData.Color = messageNode.SelectSingleNode("Color")?.InnerText;
                    fileData.Text = messageNode.SelectSingleNode("Text")?.InnerText;


                    string imagePath = messageNode.SelectSingleNode("ImagePath")?.InnerText;
                    fileData.ImagePath = imagePath;
                    if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                    {
                        fileData.ImageBytes = await File.ReadAllBytesAsync(imagePath, _tokenSource.Token);
                    }
                    TCPServerService.Message = fileData;
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

    }
}
