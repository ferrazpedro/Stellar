﻿/* ----------------------------------------------------------------------
    Stellar ~ RetroArch Nightly Updater by wyzrd
    https://stellarupdater.github.io
    https://forums.libretro.com/users/wyzrd

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.If not, see <http://www.gnu.org/licenses/>. 

    Image Credit: ESO & NASA (CC)
   ---------------------------------------------------------------------- */

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
// Disable XML Comment warnings
#pragma warning disable 1591
#pragma warning disable 1587
#pragma warning disable 1570

namespace Stellar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Current Version
        public static Version currentVersion;
        // GitHub Latest Version
        public static Version latestVersion;
        // Alpha, Beta, Stable
        public static string currentBuildPhase = "beta";
        public static string latestBuildPhase;
        public static string[] splitVersionBuildPhase;

        // MainWindow Title
        public string TitleVersion
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static string stellarCurrentVersion;

        // Windows
        public static Configure configure;
        public static Checklist checklist;

        // Ready Check
        public static bool ready = true;

        // -----------------------------------------------
        // Window Defaults
        // -----------------------------------------------
        public MainWindow()
        {
            InitializeComponent();

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string assemblyVersion = fvi.FileVersion;
            currentVersion = new Version(assemblyVersion);

            // -------------------------
            // Title + Version
            // -------------------------
            TitleVersion = "Stellar ~ RetroArch Nightly Updater (" + Convert.ToString(currentVersion) + "-" + currentBuildPhase + ")";

            MinWidth = 500;
            MinHeight = 225;
            MaxWidth = 500;
            MaxHeight = 225;

            // -----------------------------------------------------------------
            /// <summary>
            ///     Control Binding
            /// </summary>
            // -----------------------------------------------------------------
            //DataContext = vm;


            // --------------------------------------------------
            // Load Saved Settings
            // --------------------------------------------------

            // -------------------------
            // Import Config INI
            // -------------------------
            // config.ini settings
            if (File.Exists(Paths.configFile) == true)
            {
                Configure.ImportConfig(this, Paths.configFile);
            }
            // Defaults
            else
            {
                Configure.LoadDefaults(this);
            }

            // -------------------------
            // Window Position
            // -------------------------
            if (this.Top == 0 && 
                this.Left == 0)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // -------------------------
            // Load MainWindow Defaults Override
            // -------------------------
            if (VM.MainView.Download_SelectedItem == "New Install" ||
                VM.MainView.Download_SelectedItem == "Upgrade" ||
                VM.MainView.Download_SelectedItem == "Redist" ||
                VM.MainView.Download_SelectedItem == "Stellar")
            {
                VM.MainView.Download_SelectedItem = "RetroArch";
            }

            // -------------------------
            // Load Configure Defaults
            // -------------------------
            Configure.sevenZipPath = VM.MainView.SevenZipPath_Text;
            Configure.winRARPath = VM.MainView.WinRARPath_Text;

            // -------------------------
            // Load Theme
            // -------------------------
            try
            {
                Configure.theme = VM.MainView.Theme_SelectedItem.Replace(" ", string.Empty);
                App.Current.Resources.MergedDictionaries.Clear();
                App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri("Theme" + Configure.theme + ".xaml", UriKind.RelativeOrAbsolute)
                });
            }
            catch
            {
                MessageBox.Show("Could not load theme.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }

        }

        // ----------------------------------------------------------------------------------------------
        // METHODS 
        // ----------------------------------------------------------------------------------------------
        /// <summary>
        ///    Window Loaded
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Check for Available Updates
            // -------------------------
            Task.Factory.StartNew(() =>
            {
                UpdateAvailableCheck();
            });
        }

        // -----------------------------------------------
        // Close All
        // -----------------------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // -------------------------
            // Export Config INI
            // -------------------------
            // Overwrite only if changes made
            if (File.Exists(Paths.configFile) == true)
            {
                Configure.INIFile inif = new Configure.INIFile(Paths.configFile);

                double? top = Convert.ToDouble(inif.Read("Main Window", "Position_Top"));
                double? left = Convert.ToDouble(inif.Read("Main Window", "Position_Left"));

                if (// Main Window
                    this.Top != top ||
                    this.Left != left ||
                    VM.MainView.RetroArchPath_Text != inif.Read("Main Window", "RetroArchPath_Text") ||
                    VM.MainView.Server_SelectedItem != inif.Read("Main Window", "Server_SelectedItem") ||
                    VM.MainView.Download_SelectedItem != inif.Read("Main Window", "Download_SelectedItem") ||
                    VM.MainView.Architecture_SelectedItem != inif.Read("Main Window", "Architecture_SelectedItem") ||
                    // Configure Window
                    VM.MainView.SevenZipPath_Text != inif.Read("Configure Window", "SevenZipPath_Text") ||
                    VM.MainView.WinRARPath_Text != inif.Read("Configure Window", "WinRARPath_Text") ||
                    VM.MainView.LogPath_IsChecked != Convert.ToBoolean(inif.Read("Configure Window", "LogPath_IsChecked").ToLower()) ||
                    VM.MainView.LogPath_Text != inif.Read("Configure Window", "LogPath_Text") ||
                    VM.MainView.Theme_SelectedItem != inif.Read("Configure Window", "Theme_SelectedItem") ||
                    VM.MainView.UpdateAutoCheck_IsChecked != Convert.ToBoolean(inif.Read("Configure Window", "UpdateAutoCheck_IsChecked").ToLower())
                    )
                {
                    Configure.ExportConfig(this, Paths.configFile);
                }
            }

            // Export Defaults & Currently Selected
            else if (File.Exists(Paths.configFile) == false)
            {
                //Configure.INIFile inif = new Configure.INIFile(Paths.configFile);

                Configure.ExportConfig(this, Paths.configFile);
            }

            // Exit
            e.Cancel = true;
            System.Windows.Forms.Application.ExitThread();
            Environment.Exit(0);
        }


        /// <summary>
        ///     Check For Internet Connection
        /// </summary>
        [System.Runtime.InteropServices.DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool CheckForInternetConnection()
        {
            int desc;
            return InternetGetConnectedState(out desc, 0);
        }

        /// <summary>
        ///    Update Available Check
        /// </summary>
        public void UpdateAvailableCheck()
        {
            if (CheckForInternetConnection() == true)
            {
                if (VM.MainView.UpdateAutoCheck_IsChecked == true)
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    WebClient wc = new WebClient();
                    // UserAgent Header
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Stellar Updater (https://github.com/StellarUpdater/Stellar)" + " v" + MainWindow.currentVersion + "-" + MainWindow.currentBuildPhase + " Self-Update");
                    //wc.Headers.Add("Accept-Encoding", "gzip,deflate"); //error

                    wc.Proxy = null;

                    // -------------------------
                    // Parse GitHub .version file
                    // -------------------------
                    string parseLatestVersion = string.Empty;

                    try
                    {
                        parseLatestVersion = wc.DownloadString("https://raw.githubusercontent.com/StellarUpdater/Stellar/master/.version");
                    }
                    catch
                    {
                        return;
                    }

                    // -------------------------
                    // Split Version & Build Phase by dash
                    // -------------------------
                    if (!string.IsNullOrEmpty(parseLatestVersion)) //null check
                    {
                        try
                        {
                            // Split Version and Build Phase
                            splitVersionBuildPhase = Convert.ToString(parseLatestVersion).Split('-');

                            // Set Version Number
                            latestVersion = new Version(splitVersionBuildPhase[0]); //number
                            latestBuildPhase = splitVersionBuildPhase[1]; //alpha
                        }
                        catch
                        {
                            return;
                        }

                        // Check if Stellar is the Latest Version
                        // Update Available
                        if (latestVersion > currentVersion)
                        {
                            //updateAvailable = " ~ Update Available: " + "(" + Convert.ToString(latestVersion) + "-" + latestBuildPhase + ")";

                            Dispatcher.Invoke(new Action(delegate
                            {
                                TitleVersion = "Stellar ~ RetroArch Nightly Updater (" + Convert.ToString(currentVersion) + "-" + currentBuildPhase + ")"
                                                + " UPDATE";
                            }));
                        }
                        // Update Not Available
                        else if (latestVersion <= currentVersion)
                        {
                            return;
                        }
                    }
                }
            }

            // Internet Connection Failed
            else
            {
                MessageBox.Show("Could not detect Internet Connection.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                return;
            }
        }



        // -----------------------------------------------
        // Clear RetroArch Variables
        // -----------------------------------------------
        public static void ClearRetroArchVars()
        {
            Parse.parseUrl = string.Empty;
            Parse.page = string.Empty;
            Parse.element = string.Empty;
            Parse.nightly7z = string.Empty;
            Parse.nightlyUrl = string.Empty;
            Download.extractArgs = string.Empty;

            Parse.stellar7z = string.Empty;
            Parse.stellarUrl = string.Empty;
            Parse.latestVersion = null;
        }

        // -----------------------------------------------
        // Clear Cores Variables
        // -----------------------------------------------
        public static void ClearCoresVars()
        {
            Parse.parseCoresUrl = string.Empty;
            Parse.indexextendedUrl = string.Empty;
            Download.extractArgs = string.Empty;
        }

        // -----------------------------------------------
        // Clear Lists
        // -----------------------------------------------

        public static void ClearLists()
        {
            // RetroArch
            if (Queue.NightliesList != null)
            {
                Queue.NightliesList.Clear();
                Queue.NightliesList.TrimExcess();
            }

            // Larget List Compre
            Queue.largestList = 0;

            // PC & Buildbot Core Sublists
            Queue.pcArr = null;
            Queue.bbArr = null;

            // PC Core Name
            if (Queue.List_PcCores_Name != null)
            {
                Queue.List_PcCores_Name.Clear();
                Queue.List_PcCores_Name.TrimExcess();
            }
            // PC Core Date
            if (Queue.List_PcCores_Date != null)
            {
                Queue.List_PcCores_Date.Clear();
                Queue.List_PcCores_Date.TrimExcess();
            }
            // PC Cores Name+Date
            if (Queue.List_PcCores_NameDate != null)
            {
                Queue.List_PcCores_NameDate.Clear();
                Queue.List_PcCores_NameDate.TrimExcess();
            }
            // PC Cores Name+Date Collection
            if (Queue.Collection_PcCores_NameDate != null)
            {
                Queue.Collection_PcCores_NameDate = null;
            }
            // PC Unknown Name+Date
            if (Queue.List_PcCores_UnknownName != null)
            {
                Queue.List_PcCores_UnknownName.Clear();
                Queue.List_PcCores_UnknownName.TrimExcess();
            }
            // PC Cores Unknown Name+Date
            if (Queue.List_PcCores_UnknownName != null)
            {
                Queue.List_PcCores_UnknownName.Clear();
            }
            // PC Cores Unknown Name+Date Collection
            if (Queue.CollectionPcCoresUnknownNameDate != null)
            {
                Queue.CollectionPcCoresUnknownNameDate = null;
            }


            // Buildbot Core Name
            if (Queue.List_BuildbotCores_Name != null)
            {
                Queue.List_BuildbotCores_Name.Clear();
                Queue.List_BuildbotCores_Name.TrimExcess();
            }
            // Buildbot Core Date
            if (Queue.List_BuildbotCores_Date != null)
            {
                Queue.List_BuildbotCores_Date.Clear();
                Queue.List_BuildbotCores_Date.TrimExcess();
            }
            // Buildbot Cores Name+Date
            if (Queue.List_BuildbotCores_NameDate != null)
            {
                Queue.List_BuildbotCores_NameDate.Clear();
                Queue.List_BuildbotCores_NameDate.TrimExcess();
            }
            // Buildbot Cores Name+Date Collection
            if (Queue.Collection_BuildbotCores_NameDate != null)
            {
                Queue.Collection_BuildbotCores_NameDate = null;
            }
            // Buildbot Core New Name
            if (Queue.List_BuildbotCores_NewName != null)
            {
                Queue.List_BuildbotCores_NewName.Clear();
                Queue.List_BuildbotCores_NewName.TrimExcess();
            }
            // Buildbot Core ID
            //if (Queue.ListBuildbotID != null)
            //{
            //    Queue.ListBuildbotID.Clear();
            //    Queue.ListBuildbotID.TrimExcess();
            //}


            // Excluded Core Name
            if (Queue.List_ExcludedCores_Name != null)
            {
                Queue.List_ExcludedCores_Name.Clear();
                Queue.List_ExcludedCores_Name.TrimExcess();
            }
            // Excluded Core Name ObservableCollection
            if (Queue.Collection_ExcludedCores_Name != null)
            {
                Queue.Collection_ExcludedCores_Name = null;
            }
            // Excluded Core Name+Date
            if (Queue.List_ExcludedCores_NameDate != null)
            {
                Queue.List_ExcludedCores_NameDate.Clear();
                Queue.List_ExcludedCores_NameDate.TrimExcess();
            }


            // Updated Cores Name
            if (Queue.List_CoresToUpdate_Name != null)
            {
                Queue.List_CoresToUpdate_Name.Clear();
                Queue.List_CoresToUpdate_Name.TrimExcess();
            }
            // Updated Cores Date
            if (Queue.List_UpdatedCores_Date != null)
            {
                Queue.List_UpdatedCores_Date.Clear();
                Queue.List_UpdatedCores_Date.TrimExcess();
            }
            // Updated Cores Name Collection
            if (Queue.Collection_CoresToUpdate_Name != null)
            {
                Queue.Collection_CoresToUpdate_Name = null;
            }


            // Do Not Clear
            //
            // List_RejectedCores_Name
        }


        // -----------------------------------------------
        // Folder Browser Popup 
        // -----------------------------------------------
        public void FolderBrowser() // Method
        {
            // Open Folder
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();

            // Popup Folder Browse Window
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Display Folder Path in Textbox
                VM.MainView.RetroArchPath_Text = folderBrowserDialog.SelectedPath.TrimEnd('\\') + @"\";

                // Set the Paths.retroarchPath string
                Paths.retroarchPath = VM.MainView.RetroArchPath_Text;
            }
        }




        

        // ----------------------------------------------------------------------------------------------
        // CONTROLS
        // ----------------------------------------------------------------------------------------------

        // -----------------------------------------------
        // Info Button
        // -----------------------------------------------
        private void buttonInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("RetroArch Nightly Updater (Unofficial)" 
                + "\nby wyzrd \n\nNew versions at https://stellarupdater.github.io." 
                + "\n\nPlease install 7-Zip for this program to properly extract files." 
                + "\nhttp://www.7-zip.org \n\nThis software is licensed under GNU GPLv3." 
                + "\nSource code is included in the archive with this executable." 
                + "\n\nImage Credit: \nESO/José Francisco (josefrancisco.org), Milky Way" 
                + "\nESO, NGC 1232, Galaxy \nNASA, IC 405 Flaming Star" 
                + "\nNASA, NGC 5189, Spiral Nebula"
                + "\nNASA, M100, Galaxy" 
                + "\nNASA, IC 405, Lagoon" 
                + "\nNASA, Solar Flare" 
                + "\nNASA, Rho Ophiuchi, Dark Nebula" 
                + "\nNASA, N159, Star Dust" 
                + "\nNASA, NGC 6357, Chaos"
                + "\n\nThis software comes with no warranty, express or implied, and the author makes no representation of warranties. " 
                + "The author claims no responsibility for damages resulting from any use or misuse of the software.");
        }

        // -----------------------------------------------
        // Configure Settings Window Button
        // -----------------------------------------------
        private void buttonConfigure_Click(object sender, RoutedEventArgs e)
        {
            // Prevent Monitor Resolution Window Crash
            //
            try
            {
                // Detect which screen we're on
                var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                var thisScreen = allScreens.SingleOrDefault(s => Left >= s.WorkingArea.Left && Left < s.WorkingArea.Right);

                // Open Configure Window
                configure = new Configure();

                // Keep Window on Top
                configure.Owner = Window.GetWindow(this);

                // Position Relative to MainWindow
                // Keep from going off screen
                configure.Left = Math.Max((Left + (Width - configure.Width) / 2), thisScreen.WorkingArea.Left);
                configure.Top = Math.Max(Top - configure.Height - 12, thisScreen.WorkingArea.Top);

                // Open Winndow
                configure.ShowDialog();
            }
            // Simplified
            catch
            {
                // Open Configure Window
                configure = new Configure();

                // Keep Window on Top
                configure.Owner = Window.GetWindow(this);

                // Position Relative to MainWindow
                // Keep from going off screen
                configure.Left = Math.Max((Left + (Width - configure.Width) / 2), Left);
                configure.Top = Math.Max(Top - configure.Height - 12, Top);

                // Open Winndow
                configure.ShowDialog();
            }
        }

        // -----------------------------------------------
        // Stellar Website Button
        // -----------------------------------------------
        private void buttonStellarWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://stellarupdater.github.io");
        }

        // -----------------------------------------------
        // RetroArch Website Button
        // -----------------------------------------------
        private void buttonRetroArchWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.retroarch.com");
        }

        // -----------------------------------------------
        // RetroArch Folder Open Button
        // -----------------------------------------------
        private void buttonRetroArchExplorer_Click(object sender, RoutedEventArgs e)
        {
            // Paths.retroarchPath string is set when user chooses location from textbox
            if (!string.IsNullOrEmpty(VM.MainView.RetroArchPath_Text))
            {
                Process.Start("explorer.exe", @VM.MainView.RetroArchPath_Text);
            }
            else
            {
                MessageBox.Show("Please choose RetroArch Folder location first.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }

        // -----------------------------------------------
        // BuildBot Web Server Open Button
        // -----------------------------------------------
        private void buttonBuildBotDir_Click(object sender, RoutedEventArgs e)
        {
            // Open the URL
            Process.Start(VM.MainView.DownloadURL_Text);
        }

        // -----------------------------------------------
        // Location RetroArch Label (On Click)
        // -----------------------------------------------
        private void labelRetroArchPath_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Call Folder Browser Popup
            FolderBrowser();
        }

        // -----------------------------------------------
        // Location RetroArch TextBox (On Click/Mouse Down)
        // -----------------------------------------------
        private void tbxRetroArchPath_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FolderBrowser();
        }

        // -----------------------------------------------
        // Dropdown Combobox Platform/Architecture 32-bit, 64-bit
        // -----------------------------------------------
        private void comboBoxArchitecture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set Architecture to show in URL Textbox
            Paths.SetArchitecture();
            // Show URL in Textbox
            Paths.SetUrls();

            // Clear for next pass
            ClearLists();
        }

        // -----------------------------------------------
        // Combobox Download (On Changed)
        // -----------------------------------------------
        private void comboBoxDownload_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Disable Server ComboBox
            if (VM.MainView.Download_SelectedItem == "Stellar")
            {
                VM.MainView.RetroArchPath_IsEnabled = false;
                VM.MainView.Server_IsEnabled = false;
            }
            // Enable Server ComboBox
            else
            {
                VM.MainView.RetroArchPath_IsEnabled = true;
                VM.MainView.Server_IsEnabled = true;
            }


            // Reset Update Button Text to "Update"
            VM.MainView.Update_Text = "Update";


            // Stellar Self-Update Selected, Disable Architecture ComboBox
            if (VM.MainView.Download_SelectedItem == "Stellar")
            {
                VM.MainView.Architecture_IsEnabled = false;
            }
            else
            {
                VM.MainView.Architecture_IsEnabled = true;
            }

            // Cross Thread
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                // New Install
                //
                if (VM.MainView.Download_SelectedItem== "New Install")
                {
                    // Change Update Button Text
                    VM.MainView.Update_Text = "Install";

                    // Warn user about New Install
                    MessageBox.Show("This will install a New Nightly RetroArch + Cores." 
                                    + "\n\nIt will overwrite any existing files/configs in the selected folder." 
                                    + "\n\nDo not use the New Install option to Update.",
                                    "Notice",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }

                // Upgrade
                //
                else if (VM.MainView.Download_SelectedItem == "Upgrade")
                {
                    // Change Update Button Text
                    VM.MainView.Update_Text = "Upgrade";

                    // Warn user about Upgrade
                    MessageBox.Show("Backup your configs and custom shaders! Large Download." 
                                    + "\n\nThis will fully upgrade RetroArch to the latest version."
                                    + "\nFor small updates use the \"RetroArch\" or \"RA+Cores\" menu option."
                                    + "\n\nUpdate Cores separately using \"Cores\" menu option.",
                                    "Notice",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }

                // New Cores
                //
                else if (VM.MainView.Download_SelectedItem == "New Cores")
                {
                    // Change Update Button Text to "Upgrade"
                    VM.MainView.Update_Text = "Download";
                }
            });


            // Call Set Architecture
            Paths.SetArchitecture();
            // Call Set Urls Method
            Paths.SetUrls();

            // Clear if checked/unchecked for next pass
            ClearLists();
        }


        // -----------------------------------------------
        // Textbox RetroArch Location (On Click Change)
        // -----------------------------------------------
        private void tbxRetroArchPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set the Paths.retroarchPath string
            Paths.retroarchPath = VM.MainView.RetroArchPath_Text; //end with backslash
        }


        // -----------------------------------------------
        // Server Switch
        // -----------------------------------------------
        private void cboServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set Architecture
            Paths.SetArchitecture();

            // Set URLs
            Paths.SetUrls();
        }


        // -----------------------------------------------
        // Check Button - Tests Download URL
        // -----------------------------------------------
        private void buttonCheck_Click(object sender, RoutedEventArgs e)
        {
            //Download.waiter.Reset();
            //Download.waiter = new ManualResetEvent(false);

            // Clear RetroArch Nightlies List before each run
            if (Queue.NightliesList != null)
            {
                Queue.NightliesList.Clear();
                Queue.NightliesList.TrimExcess();
            }

            //var message = string.Join(Environment.NewLine, Queue.NightliesList); //debug
            //MessageBox.Show(message); //debug

            // Progress Info
            VM.MainView.ProgressInfo_Text = "Checking...";

            // Call SetArchitecture Method
            Paths.SetArchitecture();


            // -------------------------
            // Stellar Self-Update
            // -------------------------
            if (VM.MainView.Download_SelectedItem == "Stellar")
            {
                // Parse GitHub Page HTML
                Parse.ParseGitHubReleases();

                // Check if Stellar is the Latest Version
                if (MainWindow.currentVersion != null && Parse.latestVersion != null)
                {
                    if (Parse.latestVersion > MainWindow.currentVersion)
                    {
                        MessageBox.Show("Update Available \n\n" + "v" + Parse.latestVersion + "-" + Parse.latestBuildPhase);
                    }
                    else if (Parse.latestVersion <= MainWindow.currentVersion)
                    {
                        MessageBox.Show("This version is up to date.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                    }
                    else // null
                    {
                        MessageBox.Show("Could not find download.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                    }
                }
            }


            // -------------------------
            // RetroArch Part
            // -------------------------
            if (VM.MainView.Download_SelectedItem == "New Install" ||
                VM.MainView.Download_SelectedItem == "Upgrade" ||
                VM.MainView.Download_SelectedItem == "RA+Cores" ||
                VM.MainView.Download_SelectedItem == "RetroArch" ||
                VM.MainView.Download_SelectedItem == "Redist")
            {
                // Parse Page (HTML) Method
                Parse.ParseBuildbotPage();

                // Display message if download available
                if (!string.IsNullOrEmpty(Parse.nightly7z)) // If Last Item in Nightlies List is available
                {
                    MessageBox.Show("Download Available \n\n" + Paths.buildbotArchitecture + "\n\n" + Parse.nightly7z);
                }
                else
                {
                    MessageBox.Show("Could not find download.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                }
            }

            // Progress Info
            VM.MainView.ProgressInfo_Text = "";


            // -------------------------
            // Cores Part
            // -------------------------
            // If Download Combobox Cores or RA+Cores selected
            if (VM.MainView.Download_SelectedItem == "RA+Cores" ||
                VM.MainView.Download_SelectedItem == "Cores" ||
                VM.MainView.Download_SelectedItem == "New Cores")
            {
                // Create Builtbot Cores List
                Parse.ParseBuildbotCoresIndex();

                // Create PC Cores Lists
                Parse.ScanPcCoresDir();

                // Create Cores to Update List
                Queue.CoresToUpdate();

                // Check if Cores Up To Date
                // If All Cores up to date, display message
                Queue.CoresUpToDateCheck(); //Note there are Clears() in this method

                // -------------------------
                // Window Checklist Popup
                // -------------------------
                // If Update List greater than 0, Popup Checklist
                if (Queue.List_CoresToUpdate_Name.Count != 0)
                {
                    // Prevent Monitor Resolution Window Crash
                    //
                    try
                    {
                        // Detect which screen we're on
                        var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                        var thisScreen = allScreens.SingleOrDefault(s => Left >= s.WorkingArea.Left && Left < s.WorkingArea.Right);

                        // Start Window
                        checklist = new Checklist();

                        // Keep Window on Top
                        checklist.Owner = Window.GetWindow(this);

                        // Position Relative to MainWindow
                        checklist.Left = Math.Max((Left + (Width - checklist.Width) / 2), thisScreen.WorkingArea.Left);
                        checklist.Top = Math.Max((Top + (Height - checklist.Height) / 2), thisScreen.WorkingArea.Top);

                        // Open Window
                        checklist.ShowDialog();
                    }
                    // Simplified
                    catch
                    {
                        // Start Window
                        checklist = new Checklist();

                        // Keep Window on Top
                        checklist.Owner = Window.GetWindow(this);

                        // Position Relative to MainWindow
                        checklist.Left = Math.Max((Left + (Width - checklist.Width) / 2), Left);
                        checklist.Top = Math.Max((Top + (Height - checklist.Height) / 2), Top);

                        // Open Window
                        checklist.ShowDialog();
                    }
                }
            }

            // Clear All again to prevent doubling up on Update button
            ClearRetroArchVars();
            ClearCoresVars();
            ClearLists();
        }


        // ----------------------------------------------------------------------------------------------
        // Update
        // ----------------------------------------------------------------------------------------------
        public void Update()
        {
            Download.waiter.Reset();
            Download.waiter = new ManualResetEvent(false);

            // Clear RetroArch Nightlies List before each run
            if (Queue.NightliesList != null)
            {
                Queue.NightliesList.Clear();
                Queue.NightliesList.TrimExcess();
            }

            //var message = string.Join(Environment.NewLine, Queue.NightliesList); //debug
            //MessageBox.Show(message); //debug

            // Add backslash to Location Textbox path if missing
            if (!string.IsNullOrEmpty(VM.MainView.RetroArchPath_Text) && !VM.MainView.RetroArchPath_Text.EndsWith("\\"))
            {
                VM.MainView.RetroArchPath_Text = VM.MainView.RetroArchPath_Text + "\\";
            }
            // Load the User's RetroArch Location from Text Box / Saved Settings
            Paths.retroarchPath = VM.MainView.RetroArchPath_Text; //end with backslash


            // If RetroArch Path is empty, halt progress
            if (string.IsNullOrEmpty(Paths.retroarchPath) &&
                VM.MainView.Download_SelectedItem != "Stellar") // ignore if Stellar Self Update
            {
                ready = false;
                MessageBox.Show("Please select your RetroArch main folder.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                return;
            }

            // MUST BE IN THIS ORDER: 1. SetArchitecture -> 2. parsePage -> 3. SetArchiver  ##################
            // If you checkArchiver before parsePage, it will not set the Parse.nightly7z string in the CLI Arguments first
            // Maybe solve this by putting CLI Arguments in another method?

            // 1. Call SetArchitecture Method
            Paths.SetArchitecture();

            // 2. Call parse Page (HTML) Method
            if (VM.MainView.Download_SelectedItem == "New Install" ||
                VM.MainView.Download_SelectedItem == "Upgrade" ||
                VM.MainView.Download_SelectedItem == "RA+Cores" ||
                VM.MainView.Download_SelectedItem == "RetroArch" ||
                VM.MainView.Download_SelectedItem == "Redist")
            {
                // Progress Info
                VM.MainView.ProgressInfo_Text = "Fetching RetroArch List...";

                Parse.ParseBuildbotPage();
            }

            // 3. Call checkArchiver Method
            // If Archiver exists, Set string
            Archiver.SetArchiver(this);


            // -------------------------
            // Stellar Self-Update
            // -------------------------
            if (VM.MainView.Download_SelectedItem == "Stellar")
            {
                // Parse GitHub Page HTML
                Parse.ParseGitHubReleases();

                if (Parse.latestVersion != null && MainWindow.currentVersion != null)
                {
                    // Check if Stellar is the Latest Version
                    if (Parse.latestVersion > MainWindow.currentVersion)
                    {
                        // Yes/No Dialog Confirmation
                        //
                        MessageBoxResult result = MessageBox.Show("v" + Parse.latestVersion + "-" + Parse.latestBuildPhase + "\n\nDownload Update?", "Update Available", MessageBoxButton.YesNo);
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                // Proceed
                                break;
                            case MessageBoxResult.No:
                                // Lock
                                MainWindow.ready = false;
                                break;
                        }
                    }
                    else if (Parse.latestVersion <= MainWindow.currentVersion)
                    {
                        // Lock
                        MainWindow.ready = false;
                        MessageBox.Show("This version is up to date.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                    }
                    else // null
                    {
                        // Lock
                        MainWindow.ready = false;
                        MessageBox.Show("Could not find download. Try updating manually.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                    }
                }
            }


            // -----------------------------------------------
            // If New Install (RetroArch + Cores)
            // -----------------------------------------------
            if (VM.MainView.Download_SelectedItem == "New Install")
            {
                // -------------------------
                // Create Cores Folder
                // -------------------------
                using (Process execMakeCoresDir = new Process())
                {
                    execMakeCoresDir.StartInfo.UseShellExecute = false;
                    execMakeCoresDir.StartInfo.Verb = "runas"; //use with ShellExecute for admin
                    execMakeCoresDir.StartInfo.CreateNoWindow = true;
                    execMakeCoresDir.StartInfo.RedirectStandardOutput = true; //set to false if using ShellExecute
                    execMakeCoresDir.StartInfo.FileName = "cmd.exe";
                    execMakeCoresDir.StartInfo.Arguments = "/c cd " + "\"" + Paths.retroarchPath + "\"" + " && mkdir cores";
                    execMakeCoresDir.Start();
                    execMakeCoresDir.WaitForExit();
                    execMakeCoresDir.Close();
                }

                // Set Cores Folder (Dont Scan PC)
                Paths.coresPath = Paths.retroarchPath + "RetroArch-Win64\\cores\\";
                // Call Parse Builtbot Page Method
                Parse.ParseBuildbotCoresIndex();
            }



            // -----------------------------------------------
            // RetroArch+Cores or Cores Only Update
            // -----------------------------------------------
            if (VM.MainView.Download_SelectedItem == "New Install" ||
                VM.MainView.Download_SelectedItem == "RA+Cores" ||
                VM.MainView.Download_SelectedItem == "Cores" ||
                VM.MainView.Download_SelectedItem == "New Cores")
            {
                // Progress Info
                VM.MainView.ProgressInfo_Text = "Fetching Cores List...";

                // Create Builtbot Cores List
                Parse.ParseBuildbotCoresIndex();

                // Create PC Cores List
                Parse.ScanPcCoresDir();

                // Create Cores to Update List
                Queue.CoresToUpdate();

                // Check if Cores Up To Date
                // If All Cores up to date, display message
                Queue.CoresUpToDateCheck(); //Note there are Clears() in this method
            }



            // -----------------------------------------------
            // Ready
            // -----------------------------------------------
            if (ready == true)
            {
                // Start Download
                if (CheckForInternetConnection() == true)
                {
                    Download.StartDownload();
                }
                // Internet Connection Failed
                else
                {
                    MessageBox.Show("Could not detect Internet Connection.",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);

                    return;
                }
            }
            else
            {
                // Restart & Reset ready value
                ready = true;
                // Call Garbage Collector
                GC.Collect();
            }
        }

        // -----------------------------------------------
        // Update Button
        // -----------------------------------------------
        // Launches Download and 7-Zip Extraction
        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            Update();
        }

    }


    /// <summary>
    ///    ListView Indexer Class
    /// </summary>
    // ListView Numbering
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            var item = (ListViewItem)value;
            var listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item) + 1;
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
