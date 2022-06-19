using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;

namespace Client.ViewModels
{
    internal class FilesWindowViewModel : ViewModelBase
    {
        public FilesWindowViewModel()
        {
            foreach (var logicalDrive in Directory.GetLogicalDrives())
                DirectoriesAndFiles.Add(new DirectoryViewModel(logicalDrive));
        }
        string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);

        }

        FileEntityViewModel _selectedFileEntity;
        public FileEntityViewModel SelectedFileEntity
        {
            get => _selectedFileEntity;
            set => this.RaiseAndSetIfChanged(ref _selectedFileEntity, value);
        }

        ObservableCollection<FileEntityViewModel> _directoriesAndFiles = new ObservableCollection<FileEntityViewModel>();
        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles
        {
            get => _directoriesAndFiles;
            set => this.RaiseAndSetIfChanged(ref _directoriesAndFiles, value);
        }

        private void Open(object parameter)
        {
            if (parameter is DirectoryViewModel directoryViewModel)
            {
                FilePath = directoryViewModel.FullName;
                DirectoriesAndFiles.Clear();
                var directoryInfo = new DirectoryInfo(FilePath);
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    DirectoriesAndFiles.Add(new DirectoryViewModel(directory));
                }

                foreach (var fileInfo in directoryInfo.GetFiles())
                {
                    DirectoriesAndFiles.Add(new FileViewModel(fileInfo));
                }
            }
            else
            {
                FilePath = (parameter as FileViewModel).FullName;
                if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Windows.ToList().ForEach(x =>
                    {
                        if (x is Views.FilesWindow)
                            x.Close();
                    });
                }
            }
        }

        public void Back()
        {
            DirectoriesAndFiles.Clear();
            var directoryInfo = new DirectoryInfo(FilePath).Parent;
            if (directoryInfo != null)
            {
                FilePath = directoryInfo.FullName;
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    DirectoriesAndFiles.Add(new DirectoryViewModel(directory));
                }

                foreach (var fileInfo in directoryInfo.GetFiles())
                {
                    DirectoriesAndFiles.Add(new FileViewModel(fileInfo));
                }
            }
            else
            {
                FilePath = "";
                DirectoriesAndFiles.Clear();
                foreach (var logicalDrive in Directory.GetLogicalDrives())
                    DirectoriesAndFiles.Add(new DirectoryViewModel(logicalDrive));

            }
        }
    }
}