using System.Windows;
using System.Windows.Media;

namespace AppUI.Classes.Themes
{
    public class ThemeSettings : ITheme
    {
        public string Name { get; set; }

        public string PrimaryAppBackground { get; set; }
        public string SecondaryAppBackground { get; set; }
        public string PrimaryControlBackground { get; set; }
        public string PrimaryControlForeground { get; set; }
        public string PrimaryControlSecondary { get; set; }
        public string PrimaryControlPressed { get; set; }
        public string PrimaryControlMouseOver { get; set; }
        public string PrimaryControlDisabledBackground { get; set; }
        public string PrimaryControlDisabledForeground { get; set; }
        public string BackgroundImageName { get; set; }
        public string BackgroundImageBase64 { get; set; }
        public HorizontalAlignment BackgroundHorizontalAlignment { get; set; }
        public VerticalAlignment BackgroundVerticalAlignment { get; set; }
        public Stretch BackgroundStretch { get; set; }


        public static string ColorToHexString(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        public static ITheme GetThemeFromEnum(AppTheme appTheme)
        {
            switch (appTheme)
            {
                case AppTheme.Custom:
                    return new ThemeSettings();

                case AppTheme.Tsunamods:
                    return new TsunamodsTheme();

                case AppTheme.JunctionVIIITheme:
                    return new JunctionVIIITheme();

                default:
                    return null;
            }
        }
    }
}
