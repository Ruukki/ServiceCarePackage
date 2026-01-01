using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ServiceCarePackage.Translator
{
    public interface ITranslator
    {
        string Translate(string input);
    }
    public sealed class Translator : ITranslator
    {
        private readonly Dictionary<string, string> _map;
        private readonly Func<string> nameProvider;

        public Translator(
            Dictionary<string, string> map,
            Func<string> nameProvider)
        {
            _map = map;
            this.nameProvider = nameProvider;
        }

        public string Translate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string prefix = string.Empty;
            if (text.StartsWith("/"))
            {
                if (text.StartsWith("/w "))
                {
                    text = text.Replace("/w ", "/tell ");
                }
                if (text.StartsWith("/t "))
                {
                    text = text.Replace("/t ", "/tell ");
                }

                prefix = Regex.Match(text, @"(?<=^|\s)/tell\s{1}\S+\s{1}\S+@\S+(?=\s|$)").Value;
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (prefix.Length == text.Length)
                    {
                        return text;
                    }
                    text = text.Substring(prefix.Length + 1);
                }
            }
            foreach (var (key, value) in _map)
            {
                var len = text.Length;
                var resolvedValue = value.Replace("_", nameProvider());
                //should ignore <text>, sidenote still ignores <text for optimisation
                text = Regex.Replace(text, $@"(?i)(?<=^|\s|\W)(?<=[^<\[]){key}(?=[^>\]])(?=\s|\W|$)", resolvedValue);
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                return prefix + " " + text;
            }
            return text;
        }
    }

}
