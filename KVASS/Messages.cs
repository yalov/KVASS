using KSP.Localization;
using Smooth.Algebraics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KVASSNS
{
    static class Messages
    {
        class Triplet
        {
            public Triplet(string message, string note, bool? paragraph = null)
            {
                Message = message;
                Note = note;
                Paragraph = paragraph;
            }

            public String Message { get; }
            public String Note { get; }
            public bool? Paragraph { get; }
        }

        public const String MessageColor = "#ffa500ff"; // Orange
        public const String NoteColor    = "#ffa500af"; // OrangeAlpha
        

       
        /// <summary>
        /// Add fail message and optional note to the quere.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="note"></param>
        public static void Add(string message, string note = "", bool? paragraph= null)
        {
            if (String.IsNullOrEmpty(message)) message = "";
            if (String.IsNullOrEmpty(note)) note = "";

            message = message.Trim().RemoveUnpairedHtmlTag("b");
            note = note.Trim().RemoveUnpairedHtmlTag("b");

            _failMessages.Add(
                new Triplet(message, note, paragraph)
            );

            foreach (var str in new List<string> { message, note })
                if (!String.IsNullOrEmpty(str))
                    _countOfLines += str.Split('\n').Count();
        }
        
        /// <summary>
        /// Show all messages from the quere, and clear the quere afterwards.
        /// Looks like max amount of line at screen at ones is 10.
        /// </summary>
        /// <param name="seconds"></param>
        public static void ShowAndClear(int seconds = 5)
        {
            bool several = Count() > 1;

            if (several)
                Add(Localizer.Format("#KVASS_message_total", Count()), paragraph: false);
                
            bool emptyLine = Count() + _countOfLines <= 11;
            bool second = _countOfLines <= 10;

            for (int i = 0; i < Count(); i++)
            {
                var fm = _failMessages[i];
                string message = fm.Message;
                string note = fm.Note;
                bool paragraph = fm.Paragraph.HasValue ? fm.Paragraph.Value : several;
                bool both = !String.IsNullOrEmpty(message) && !String.IsNullOrEmpty(note);
                bool last = i == Count() - 1;

                PostScreenMessage(
                    Paragraph(Colorize(message, MessageColor), paragraph)
                    + (both && second ? "\n" : "")
                    + (second ? Colorize(note, NoteColor) : "")
                    + (emptyLine && !last ? "\n\n" : ""),
                    seconds * (i + 1));

            }
                
            Clear();
        }
        
        /// <summary>
        /// Quick message post without a quere. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public static void QuickPost(string message, float duration = 5.0f, String color = null)
        {
            if (color == null) PostScreenMessage(message, duration);

            UnityEngine.Color c;
            if (UnityEngine.ColorUtility.TryParseHtmlString(color, out c))
                PostScreenMessage(message, duration, color: c);
            else
                PostScreenMessage(message, duration);
        }
        


        /// <summary>
        /// Amount of messages in the quere.
        /// </summary>
        /// <returns></returns>
        public static int Count()
        {
            return _failMessages.Count;
        }

        private static void Clear()
        {
            _failMessages.Clear();
            _countOfLines = 0;
        }

        private static string Paragraph(string message, bool enclose)
        {
            if (enclose)
                return Red("> ") + message;
            else
                return message;

        }
        private static string Red(string message) => Colorize(message, "red");
        private static string Colorize(string message, string color) => String.Format("<color={1}>{0}</color>", message, color);

        private static void PostScreenMessage(string message, float duration = 5.0f,
            ScreenMessageStyle style = ScreenMessageStyle.UPPER_CENTER, UnityEngine.Color? color = null)
        {
            if (color == null)
                ScreenMessages.PostScreenMessage(message, duration, style);
            else
                ScreenMessages.PostScreenMessage(message, duration, style, color.Value);
        }

        private static string RemoveUnpairedHtmlTag(this string str, string tag)
        {
            int open_index = str.IndexOf("<" + tag + ">");
            int close_index = str.IndexOf("</" + tag + ">");

            if (open_index != -1 && close_index == -1)
                return str.Remove(open_index, tag.Length + 2);

            if (open_index == -1 && close_index != -1)
                return str.Remove(close_index, tag.Length + 3);

            return str;
        }


        private static List<Triplet> _failMessages = new List<Triplet>();
        private static int _countOfLines = 0;
    }
}
