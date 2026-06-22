using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ToxicFishing.App.Converters;

/// <summary>Maps <see langword="true"/> to <see cref="Visibility.Collapsed"/> and anything else to
/// <see cref="Visibility.Visible"/> — the inverse of <see cref="BoolToVisibleConverter"/>.</summary>
public sealed class BoolToCollapsedConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to a <see cref="Visibility"/>, collapsing on <see langword="true"/>.
    /// </summary>
    /// <param name="value">The source value; collapses the element only when it is <see langword="true"/>.</param>
    /// <param name="targetType">The binding target type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    /// <returns><see cref="Visibility.Collapsed"/> when <paramref name="value"/> is <see langword="true"/>;
    /// otherwise <see cref="Visibility.Visible"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Converts a <see cref="Visibility"/> back to a boolean.
    /// </summary>
    /// <param name="value">The source value; returns <see langword="true"/> unless it is
    /// <see cref="Visibility.Visible"/>.</param>
    /// <param name="targetType">The binding target type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is not
    /// <see cref="Visibility.Visible"/>; otherwise <see langword="false"/>.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not Visibility.Visible;
}
