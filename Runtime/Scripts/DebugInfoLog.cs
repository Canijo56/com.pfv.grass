using System;
using System.Text;

namespace PFV.Grass
{
    [System.Serializable]
    public class DebugInfoLog
    {
        private StringBuilder _builder;
        private int _indentLevel;

        public static implicit operator string(DebugInfoLog log) => log._builder.ToString();
        private static string _HEADER_SEPARATOR = "----------------";
        private static string _HEADER_FORMAT = $"{_HEADER_SEPARATOR}\n{{0}}\n{_HEADER_SEPARATOR}\n\n";
        private static string _NAME_VALUE_FORMAT = $"\t<b>{{0}}</b>: {{1}}.\n";
        private static string _PARAGRAPH_FORMAT = $"\t{{0}}.\n";
        private static string _LIST_START_FORMAT = $"<b>{{0}}</b>:\n";
        private static string _LIST_ITEM_FORMAT = $"\t- {{0}}.\n";

        public DebugInfoLog()
        {
            _builder = new StringBuilder();
        }

        ~DebugInfoLog()
        {
        }

        public void Header(string headerLabel)
        {
            ApplyIndent();
            _builder.AppendFormat(_HEADER_FORMAT, headerLabel);
        }

        public void Indent() => _indentLevel++;
        public void Unindent() => _indentLevel--;
        public void ClearIndent() => _indentLevel = 0;

        private void ApplyIndent()
        {
            if (_indentLevel <= 0)
                return;
            for (int i = 0; i < _indentLevel; i++)
                _builder.Append("\t");
        }

        public void Value<T>(string valueName, T value)
        {
            ApplyIndent();
            _builder.AppendFormat(_NAME_VALUE_FORMAT, valueName, value.ToString());
        }
        public void Paragraph(string msg)
        {
            ApplyIndent();
            _builder.AppendFormat(_PARAGRAPH_FORMAT, msg);
        }
        public void ListStart(string listName)
        {
            ApplyIndent();
            _builder.AppendFormat(_LIST_START_FORMAT, listName);
        }
        public void ListItem<T>(T item)
        {
            ApplyIndent();
            _builder.AppendFormat(_LIST_ITEM_FORMAT, item.ToString());
        }
        public void NextLine()
        {
            ApplyIndent();
            _builder.Append("\n");
        }
        public void Clear()
        {
            ApplyIndent();
            _builder.Clear();
        }
    }
}