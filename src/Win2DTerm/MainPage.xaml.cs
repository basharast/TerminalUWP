using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Win2DTerm
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static EventHandler DisconnectRequest;
        public static EventHandler ProfileRequest;
        public static EventHandler HideSettingsHandler;
        public static StorageFile profileFile = null;
        Visibility ScrollModeVisibile = Visibility.Visible;
        Visibility ScrollModeVisibileExt = Visibility.Collapsed;

        bool autoSave = true;
        bool AutoSave
        {
            get
            {
                return autoSave;
            }
            set
            {
                autoSave = value;
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("autosave", value);
            }
        }
        Crypto crypto = new Crypto();
        bool scrollModeState = false;
        bool ScrollModeState
        {
            get
            {
                return scrollModeState;
            }
            set
            {
                if (value)
                {
                    ScrollModeVisibile = Visibility.Collapsed;
                    ScrollModeVisibileExt = Visibility.Visible;
                }
                else
                {
                    ScrollModeVisibileExt = Visibility.Collapsed;
                    ScrollModeVisibile = Visibility.Visible;
                }
                scrollModeState = value;
                try
                {
                    this.Bindings.Update();
                }
                catch (Exception ex)
                {

                }
            }
        }

        bool appIsReady = false;
        List<string> linksHistory = new List<string>();
        List<string> commandsHistory = new List<string>();
        public MainPage()
        {
            InitializeComponent();
            try
            {
                AutoSave = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("autosave", true);
                if (AutoSave)
                {
                    var address = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("address", "192.168.0.1");
                    if (address != null)
                    {
                        Hostname.Text = address.ToString();
                    }

                    var port = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("port", "22");
                    if (port != null)
                    {
                        Port.Text = port.ToString();
                    }

                    var user = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("user", "root");
                    if (user != null)
                    {
                        Username.Text = user.ToString();
                    }

                    var pass = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("pass", "root");
                    if (pass != null)
                    {
                        Password.Password = pass.ToString();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                var linksHistoryData = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("History", "");
                if (linksHistoryData.Length > 0)
                {
                    linksHistory = JsonConvert.DeserializeObject<List<string>>(linksHistoryData);
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                var commandsHistoryData = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("CHistory", "");
                if (commandsHistoryData.Length > 0)
                {
                    commandsHistory = JsonConvert.DeserializeObject<List<string>>(commandsHistoryData);
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                commandInput = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("inputState", true);
                commandCheckBox.IsChecked = commandInput;
                if (!commandInput)
                {
                    CommandCode.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CommandCode.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                DisconnectRequest += DisconnectRequestCall;
                ProfileRequest += checkProfile;
                HideSettingsHandler += HideSettings;
            }
            catch (Exception ex)
            {

            }
            printWelcome();
            appIsReady = true;
            if (profileFile != null)
            {
                checkProfile(null, EventArgs.Empty);
            }
        }

        private async void checkProfile(object sender, EventArgs e)
        {
            try
            {
                if (profileFile != null)
                {
                    while (!appIsReady)
                    {
                        await Task.Delay(500);
                    }
                    byte[] resultInBytes;
                    using (var targetStream = await profileFile.OpenAsync(FileAccessMode.Read))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await targetStream.AsStreamForRead().CopyToAsync(memoryStream);
                            resultInBytes = memoryStream.ToArray();
                        }
                        try
                        {
                            targetStream.Dispose();
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    resultInBytes = crypto.Decrypt(resultInBytes);
                    var textRead = Encoding.UTF8.GetString(resultInBytes, 0, resultInBytes.Length);
                    ProfileData profileData = JsonConvert.DeserializeObject<ProfileData>(textRead);
                    await Disconnect();
                    Hostname.Text = profileData.host;
                    Port.Text = profileData.port;
                    Username.Text = profileData.user;
                    Password.Password = profileData.password;
                    if (profileData.isKey)
                    {
                        try
                        {
                            KeySwitch.IsChecked = true;
                            if (profileData.KeyToken.Length > 0)
                            {
                                KeyFile = await GetFileForToken(profileData.KeyToken);
                                if (KeyFile != null)
                                {
                                    ImportNewKey.Content = "Reset Key";
                                    KeyName.Text = KeyFile.Name;
                                }
                            }
                        }catch(Exception ex)
                        {
                            ShowDialog(ex);
                        }
                    }else
                    {
                        PasswordSwitch.IsChecked = true;
                    }
                    ShowDialog("Profile loaded");
                    profileFile = null;
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }
        private async void SaveProfile()
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
                folderPicker.FileTypeFilter.Add("*");

                StorageFolder PickedFolder = await folderPicker.PickSingleFolderAsync();
                if (PickedFolder != null)
                {
                    var fileToken = "";
                    if (KeySwitch.IsChecked.Value && KeyFile != null)
                    {
                        fileToken = RememberFile(KeyFile);
                    }
                    ProfileData profileData = new ProfileData(Hostname.Text.Trim(), Port.Text.Trim(), Username.Text.Trim(), Password.Password, KeySwitch.IsChecked.Value, fileToken);
                    var fileName = Hostname.Text.Replace(".", "_");
                    var htmlFile = await PickedFolder.CreateFileAsync($"{fileName}.tprf", CreationCollisionOption.GenerateUniqueName);
                    if (htmlFile != null)
                    {
                        var text = JsonConvert.SerializeObject(profileData);
                        var resultInBytes = Encoding.UTF8.GetBytes(text);
                        var resultInBytesEncrypted = crypto.Encrypt(resultInBytes);
                        await FileIO.WriteBytesAsync(htmlFile, resultInBytesEncrypted);
                        ShowDialog($"Profile saved:\n\r{htmlFile.Path}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }
        public string RememberFile(StorageFile file)
        {
            string token = Guid.NewGuid().ToString();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, file);
            return token;
        }
        public async Task<StorageFile> GetFileForToken(string token)
        {
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(token)) return null;
            return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
        }
        private void DisconnectRequestCall(object sender, EventArgs e)
        {
            try
            {
                if (terminal.Connected)
                {
                    terminal.Disconnect();
                }
            }
            catch (Exception ex)
            {

            }
        }
        private void DisableInputs(bool state)
        {
            Hostname.IsEnabled = !state;
            Port.IsEnabled = !state;
            Username.IsEnabled = !state;
            Password.IsEnabled = !state;
            PasswordIcon.IsEnabled = !state;
            PasswordSwitch.IsEnabled = !state;
            KeySwitch.IsEnabled = !state;
            ImportNewKey.IsEnabled = !state;
        }
        private async void printWelcome()
        {
            try
            {
                await Task.Delay(1500);
                terminal.PushNewData($"Terminal UWP {GetAppVersion()}\n\r{DateTime.Now}\n\r");
            }
            catch (Exception ex)
            {

            }
        }
        private async Task SaveSessionOutput()
        {
            try
            {
                var output = terminal.getAllOutput();
                if (output != null && output.Trim().Length > 0)
                {
                    var folderPicker = new FolderPicker();
                    folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
                    folderPicker.FileTypeFilter.Add("*");

                    StorageFolder PickedFolder = await folderPicker.PickSingleFolderAsync();
                    if (PickedFolder != null)
                    {
                        var fileName = Hostname.Text.Replace(".", "_");
                        var htmlFile = await PickedFolder.CreateFileAsync($"{fileName}_output.txt", CreationCollisionOption.GenerateUniqueName);
                        if (htmlFile != null)
                        {
                            var resultInBytes = Encoding.UTF8.GetBytes(output);
                            await FileIO.WriteBytesAsync(htmlFile, resultInBytes);
                            ShowDialog($"Output saved:\n\r{htmlFile.Path}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }
        public static string GetAppVersion()
        {
            try
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;

                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
            catch (Exception ex)
            {

            }
            return "1.0.4.0";
        }
        private void AboutNotesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDialog("\n\rCreated by Darren R. Starr\n\rEnhanced by Bashar Astifan\n\rApp Logo by Martin Anderson\n\rGitHub: https://github.com/darrenstarr/TerminalEmulatorUWP \n\r\n\rTHE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND");
            terminal.PasteText("\r");
            terminal.SetFocus();
        }

        public static bool _settingsPaneVisible = true;
        private void HideSettings(object sender, EventArgs e)
        {
            try
            {
                _settingsPaneVisible = false;
                ColumnSettings.Width = new GridLength(0);
                terminal.SetFocus();
            }
            catch (Exception ex)
            {
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settingsPaneVisible = !_settingsPaneVisible;
                if (_settingsPaneVisible)
                {
                    ColumnSettings.Width = new GridLength(220);
                }
                else
                {
                    ColumnSettings.Width = new GridLength(0);
                }
                terminal.SetFocus();
            }
            catch (Exception ex)
            {
            }
        }
        private void showKeyBoard()
        {
            try
            {
                InputPane pane = InputPane.GetForCurrentView();
                if (pane.Visible)
                {
                    var state = pane.TryHide();
                    if (!state)
                    {
                        ShowDialog("Touch keyboard not supported!");
                    }
                }
                else
                {
                    var state = pane.TryShow();
                    if (!state)
                    {
                        ShowDialog("Touch keyboard not supported!");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }
        private void AddressBar_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            ConnectionProgress.Visibility = Visibility.Visible;
            try
            {
                if (e.Key == VirtualKey.Enter)
                {
                    if (CommandCode.Text.Trim().Length != 0)
                    {
                        terminal.PasteText($"{CommandCode.Text}\r");
                        //var result = terminal.RunCommand(CommandCode.Text);
                        terminal.SetFocus();
                        try
                        {
                            if (!commandsHistory.Contains(CommandCode.Text.Trim()))
                            {
                                commandsHistory.Add(CommandCode.Text.Trim());
                                try
                                {
                                    var data = JsonConvert.SerializeObject(commandsHistory);
                                    Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("CHistory", data);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        CommandCode.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
            ConnectionProgress.Visibility = Visibility.Collapsed;
        }

        private void ClearCommandsHistory()
        {
            try
            {
                commandsHistory = new List<string>();
                var data = JsonConvert.SerializeObject(commandsHistory);
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("CHistory", data);
                ShowDialog("Commands history cleaned");
            }
            catch (Exception ex)
            {

            }
        }
        public void getConnectionInfo()
        {
            try
            {
                var connectionInfo = $"\n\rAuthMethod: {(terminal._client.ConnectionInfo.AuthenticationMethods.Count > 0 ? terminal._client.ConnectionInfo.AuthenticationMethods[0].Name : "0")}\n\r" +
                    $"ClientEncryption: {terminal._client.ConnectionInfo.CurrentClientEncryption}\n\r" +
                    $"ServerEncryption: {terminal._client.ConnectionInfo.CurrentServerEncryption}\n\r" +
                    $"IsAuthenticated: {terminal._client.ConnectionInfo.IsAuthenticated}\n\r" +
                    $"MaxSessions: {terminal._client.ConnectionInfo.MaxSessions}\n\r" +
                    $"RetryAttempts: {terminal._client.ConnectionInfo.RetryAttempts}\n\r" +
                    $"ServerVersion: {terminal._client.ConnectionInfo.ServerVersion}\n\r";
                ShowDialog(connectionInfo);
                terminal.PasteText("\r");
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                try
                {
                    ConnectButton.IsEnabled = false;
                    DiscconectButton.IsEnabled = false;
                    CommandCode.IsEnabled = false;
                    ConnectionProgress.Visibility = Visibility.Visible;

                    DiscconectButton.Visibility = Visibility.Collapsed;
                    ConnectionInfoButton.Visibility = Visibility.Collapsed;
                    try
                    {
                        terminal.PushNewData("Connecting...\n\r");
                        DisableInputs(true);
                        await Task.Delay(1000);
                        if (KeyFile != null)
                        {
                            await terminal.ConnectToSsh(Hostname.Text, Convert.ToInt32(Port.Text), Username.Text, KeyFile);
                        }
                        else
                        {
                            if (Port.Text.Length > 0)
                            {
                                terminal.ConnectToSsh(Hostname.Text, Convert.ToInt32(Port.Text), Username.Text, Password.Password);
                            }
                            else
                            {
                                terminal.ConnectToSsh(Hostname.Text, Username.Text, Password.Password);
                            }
                        }
                        //ShowDialog("Connected!");
                        ConnectButton.IsEnabled = false;
                        DiscconectButton.IsEnabled = true;
                        CommandCode.IsEnabled = true;
                        DiscconectButton.Visibility = Visibility.Visible;
                        ConnectionInfoButton.Visibility = Visibility.Visible;
                        ConnectButton.Visibility = Visibility.Collapsed;
                        if (AutoSave)
                        {
                            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("address", Hostname.Text.Trim());
                            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("port", Port.Text.Trim());
                            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("user", Username.Text.Trim());
                            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("pass", Password.Password);
                        }
                        else
                        {
                            Plugin.Settings.CrossSettings.Current.Remove("address");
                            Plugin.Settings.CrossSettings.Current.Remove("port");
                            Plugin.Settings.CrossSettings.Current.Remove("user");
                            Plugin.Settings.CrossSettings.Current.Remove("pass");
                        }
                        if (!linksHistory.Contains(Hostname.Text.Trim()))
                        {
                            linksHistory.Add(Hostname.Text.Trim());
                            try
                            {
                                var data = JsonConvert.SerializeObject(linksHistory);
                                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("History", data);
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        DisableInputs(true);
                        terminal.SetFocus();
                    }
                    catch (Exception ex)
                    {
                        ShowDialog(ex);
                        DisableInputs(false);
                        CommandCode.IsEnabled = false;
                        ConnectButton.IsEnabled = true;
                        ConnectButton.Visibility = Visibility.Visible;
                    }
                    ConnectionProgress.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {

                }

            });
        }

        private void TextBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            try
            {
                if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    //Set the ItemsSource to be your filtered dataset
                    //sender.ItemsSource = dataset;
                    if (linksHistory.Count > 0)
                    {
                        ((AutoSuggestBox)sender).ItemsSource = linksHistory;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void CTextBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            try
            {
                if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    //Set the ItemsSource to be your filtered dataset
                    //sender.ItemsSource = dataset;
                    if (commandsHistory.Count > 0)
                    {
                        ((AutoSuggestBox)sender).ItemsSource = commandsHistory;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async void ShowDialog(Exception ex)
        {
            terminal.PushNewData($"\n\r{ex.Message}\n\r");
            /*var messageDialog = new MessageDialog(ex.Message);
            messageDialog.Commands.Add(new UICommand(
                "Close"));
            await messageDialog.ShowAsync();*/
        }

        private async void ShowDialog(string message)
        {
            terminal.PushNewData($"\n\r{message}\n\r");
            /*var messageDialog = new MessageDialog(message);
            messageDialog.Commands.Add(new UICommand(
                "Close"));
            await messageDialog.ShowAsync();*/
        }

        private async void DiscconectButton_Click(object sender, RoutedEventArgs e)
        {
            await Disconnect();
        }
        private async Task Disconnect()
        {
            try
            {
                if (!terminal.Connected)
                {
                    return;
                }
                ConnectButton.IsEnabled = false;
                DiscconectButton.IsEnabled = false;
                CommandCode.IsEnabled = false;
                ConnectionProgress.Visibility = Visibility.Visible;

                ConnectButton.Visibility = Visibility.Collapsed;
                ConnectionInfoButton.Visibility = Visibility.Collapsed;

                try
                {
                    terminal.PushNewData("\n\rDisconnecting..\n\r");
                    terminal.Disconnect();
                    await Task.Delay(1000);
                    ShowDialog("Disconnected!");
                    DisableInputs(false);
                    ConnectButton.IsEnabled = true;
                    ConnectButton.Visibility = Visibility.Visible;
                    DiscconectButton.Visibility = Visibility.Collapsed;
                    DiscconectButton.IsEnabled = false;
                    CommandCode.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    ShowDialog(ex);
                    DiscconectButton.IsEnabled = true;
                    ConnectionInfoButton.Visibility = Visibility.Visible;
                    DiscconectButton.Visibility = Visibility.Visible;
                    CommandCode.IsEnabled = true;
                }
                ConnectionProgress.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }
        private void VideoPaneGrid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                if (_settingsPaneVisible)
                {
                    if (e.OriginalSource == MJPEGStreamerGrid || e.OriginalSource == terminal)
                    {
                        SettingsButton_Click(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }

        private void ConnectionInfoButton_Click(object sender, RoutedEventArgs e)
        {
            getConnectionInfo();
            terminal.SetFocus();
        }

        private void KeyBoardButton_Click(object sender, RoutedEventArgs e)
        {
            showKeyBoard();
            terminal.SetFocus();
        }

        private void terminal_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            showKeyBoard();
            terminal.SetFocus();
        }

        private void TouchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDialog("Important:\n\rAfter showing the touch keyboard,\n\rpress again on the terminal to be able to write.");
            terminal.PasteText("\r");
            terminal.SetFocus();
        }


        private void CopySelectedButton_Click(object sender, RoutedEventArgs e)
        {
            terminal.copySelectedText();
            terminal.SetFocus();
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            terminal.PasteClipboard();
            terminal.SetFocus();
        }


        bool commandInput = false;
        private void CommandInputButton_Click(object sender, RoutedEventArgs e)
        {

            if (!commandCheckBox.IsChecked.Value)
            {
                CommandCode.Visibility = Visibility.Collapsed;
                commandInput = false;
            }
            else
            {
                CommandCode.Visibility = Visibility.Visible;
                commandInput = true;
            }
            terminal.SetFocus();
            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("inputState", commandInput);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            OnClearRequest();
        }

        private async void OnClearRequest()
        {
            try
            {
                var messageDialog = new MessageDialog("Do you want clean all commands history?");
                messageDialog.Commands.Add(new UICommand("Clean", new UICommandInvokedHandler(this.CommandInvokedHandler)));
                messageDialog.Commands.Add(new UICommand("Dismiss"));
                await messageDialog.ShowAsync();
            }
            catch (Exception ex)
            {

            }
        }
        private void CommandInvokedHandler(IUICommand command)
        {
            ClearCommandsHistory();
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            SaveProfile();
        }
        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new FileOpenPicker();
                filePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                filePicker.FileTypeFilter.Add(".tprf");
                var TargetFile = await filePicker.PickSingleFileAsync();
                if (TargetFile != null)
                {
                    profileFile = TargetFile;
                    checkProfile(null, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }

        private void AppBarButton_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                terminal.TerminalWheelChanged(120);
            }
            catch (Exception ex)
            {

            }
        }

        private void AppBarButton_Click_4(object sender, RoutedEventArgs e)
        {
            try
            {
                terminal.TerminalWheelChanged(-120);
            }
            catch (Exception ex)
            {

            }
        }

        private async void AppBarButton_Click_5(object sender, RoutedEventArgs e)
        {
            await SaveSessionOutput();
        }

        private void AppBarButton_Click_6(object sender, RoutedEventArgs e)
        {
            try
            {
                terminal.Clean();
            }
            catch (Exception ex)
            {
                ShowDialog(ex);
            }
        }

        StorageFile KeyFile;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (KeyFile != null)
            {
                KeyFile = null;
                ImportNewKey.Content = "Import Key";
                KeyName.Text = "Click to import key";
            }
            else
            {
                try
                {
                    var filePicker = new FileOpenPicker();
                    filePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                    filePicker.FileTypeFilter.Add("*");
                    KeyFile = await filePicker.PickSingleFileAsync();
                    if (KeyFile != null)
                    {
                        ImportNewKey.Content = "Reset Key";
                        KeyName.Text = KeyFile.Name;
                    }
                }
                catch (Exception ex)
                {
                    ShowDialog(ex);
                }
            }
        }

        private void PasswordSwitch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KeyFile = null;
                ImportNewKey.Content = "Import Key";
                KeyName.Text = "Click to import key";
            }
            catch (Exception ex)
            {

            }
        }

        private void KeySwitch_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    class ProfileData
    {
        public string host;
        public string port;
        public string user;
        public string password;
        public bool isKey = false;
        public string KeyToken = "";
        public ProfileData(string host, string port, string user, string password, bool isKey = false, string KeyToken = "")
        {
            this.host = host;
            this.port = port;
            this.user = user;
            this.password = password;
            this.isKey = isKey;
            this.KeyToken = KeyToken;
        }
    }
}
