using System;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>
    /// <c>{m3:Text Cancel}</c>: one of the library's own texts (<see cref="LibraryText"/>), kept current when the app
    /// overrides it or switches the UI culture and calls <see cref="LibraryText.Refresh"/>.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class TextExtension : MarkupExtension {
        /// <summary>Creates the extension; set <see cref="Key"/>.</summary>
        public TextExtension() { }

        /// <summary>Creates the extension for <paramref name="key"/>.</summary>
        public TextExtension(TextKey key) => Key = key;

        /// <summary>Which text.</summary>
        [ConstructorArgument("key")]
        public TextKey Key { get; set; }

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider) =>
            new Binding($"[{Key}]") { Source = LiveText.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
    }

    internal sealed class LiveText : INotifyPropertyChanged {
        internal static readonly LiveText Instance = new LiveText();

        private LiveText() => LibraryText.Changed += (_, __) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));

        public string this[string key] => Enum.TryParse(key, out TextKey k) ? LibraryText.Get(k) : key;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
