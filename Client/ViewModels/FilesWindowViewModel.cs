using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using System.IO;

namespace Client.ViewModels
{
    internal class FilesWindowViewModel : ViewModelBase
    {
        public FilesWindowViewModel()
        {
            foreach (var logicalDrive in Directory.GetLogicalDrives())
                DirectoriesAndFiles.Add(logicalDrive);
        }
        string _filePath = "ABCDEFG";
        public string FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);
            
        }
        ObservableCollection<string> _directoriesAndFiles = new ObservableCollection<string>();
        public ObservableCollection<string> DirectoriesAndFiles
        {
            get => _directoriesAndFiles;
            set => this.RaiseAndSetIfChanged(ref _directoriesAndFiles, value);
        }
    }
}
