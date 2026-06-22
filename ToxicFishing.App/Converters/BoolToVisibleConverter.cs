using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ToxicFishing.App.Converters;

/// <summary>Maps <see langword="true"/> to <see cref="Visibility.Visible"/> and anything else to
/// <see cref="Visibility.Collapsed"/>.</summary>
public sealed class BoolToVisibleConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to a <see cref="Visibility"/>.
    /// </summary>
    /// <param name="value">The source value; treated as visible only when it is <see langword="true"/>.</param>
    /// <param name="targetType">The binding target type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    /// <returns><see cref="Visibility.Visible"/> when <paramref name="value"/> is <see langword="true"/>;
    /// otherwise <see cref="Visibility.Collapsed"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Converts a <see cref="Visibility"/> back to a boolean.
    /// </summary>
    /// <param name="value">The source value; treated as <see langword="true"/> only when it is
    /// <see cref="Visibility.Visible"/>.</param>
    /// <param name="targetType">The binding target type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is <see cref="Visibility.Visible"/>;
    /// otherwise <see langword="false"/>.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
