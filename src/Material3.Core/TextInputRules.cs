using System.Text.RegularExpressions;

namespace Material3.Core {
    /// <summary>
    /// The rules a filtered numeric field checks an edit against. Each accepts the text as it would read after the
    /// edit, and passes a value still being typed (<c>-</c>, <c>1.</c>): only the caller knows whether it may stay so.
    /// </summary>
    public static class TextInputRules {
        private static readonly Regex PartialNumber = new Regex(@"^-?[0-9]*\.?[0-9]*\z");
        private static readonly Regex PartialInteger = new Regex(@"^-?[0-9]*\z");

        /// <summary>Whether <paramref name="text"/> is a signed decimal with a <c>.</c> separator, or the start of one.</summary>
        public static bool IsNumberPrefix(string text) => PartialNumber.IsMatch(text ?? string.Empty);

        /// <summary>Whether <paramref name="text"/> is a signed whole number, or the start of one.</summary>
        public static bool IsIntegerPrefix(string text) => PartialInteger.IsMatch(text ?? string.Empty);
    }
}
