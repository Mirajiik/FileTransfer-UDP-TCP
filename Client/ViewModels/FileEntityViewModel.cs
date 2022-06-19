using ReactiveUI;

namespace Client.ViewModels
{
    public abstract class FileEntityViewModel : ViewModelBase
    {
        string _name;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        string _fullName;
        public string FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }
        protected FileEntityViewModel(string name)
        {
            Name = name;
        }
    }
}
