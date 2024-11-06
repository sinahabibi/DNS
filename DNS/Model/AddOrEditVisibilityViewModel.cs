using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

public class AddOrEditVisibilityViewModel : INotifyPropertyChanged
{
    private Visibility _btnMinimizeVisibility = Visibility.Hidden;
    private Visibility _btnMaximizeVisibility = Visibility.Hidden;
    private Visibility _btnCloseVisibility = Visibility.Hidden;

    public event PropertyChangedEventHandler PropertyChanged;

    public Visibility BtnMinimizeVisibility
    {
        get => _btnMinimizeVisibility;
        set
        {
            if (_btnMinimizeVisibility != value)
            {
                _btnMinimizeVisibility = value;
                OnPropertyChanged();
            }
        }
    }

    public Visibility BtnMaximizeVisibility
    {
        get => _btnMaximizeVisibility;
        set
        {
            if (_btnMaximizeVisibility != value)
            {
                _btnMaximizeVisibility = value;
                OnPropertyChanged();
            }
        }
    }

    public Visibility BtnCloseVisibility
    {
        get => _btnCloseVisibility;
        set
        {
            if (_btnCloseVisibility != value)
            {
                _btnCloseVisibility = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
