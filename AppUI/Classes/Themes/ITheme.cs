using System.Windows;
using System.Windows.Media;

namespace AppUI.Classes.Themes
{
    public enum AppTheme
    {
        Custom,
        Tsunamods,
        JunctionVIIITheme
    }

    public interface ITheme
    {
        string Name { get; }
        string PrimaryAppBackground { get; }
        string SecondaryAppBackground { get; }
        string PrimaryControlBackground { get; }
        string PrimaryControlForeground { get; }
        string PrimaryControlSecondary { get; }
        string PrimaryControlPressed { get; }
        string PrimaryControlMouseOver { get; }
        string PrimaryControlDisabledBackground { get; }
        string PrimaryControlDisabledForeground { get; }

        string BackgroundImageName { get; }
        string BackgroundImageBase64 { get; }
        HorizontalAlignment BackgroundHorizontalAlignment { get; }
        VerticalAlignment BackgroundVerticalAlignment { get; }
        Stretch BackgroundStretch { get; }

    }
}
