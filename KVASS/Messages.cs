using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using static KVASSNS.Logging;


namespace KVASSNS
{
    static class Messages
    {
        public const String Orange = "#ffa500ff";
        public const String OrangeAlpha = "#ffa500af";


        public enum DurationType { 
            CONST,              // duration
            CLEVERCONSTPERLINE, // duration per line
            INCREMENT           // every next message stay additional duration
        };


        /// <summary>
        /// Quick Fail post without a quere. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public static void QuickPostFail(string message, string note = "", float duration = 5.0f)
        {
            message = message.Trim().RemoveUnpairedHtmlTag("b");
            note = note.Trim().RemoveUnpairedHtmlTag("b");
            
            bool both = !String.IsNullOrEmpty(message) && !String.IsNullOrEmpty(note);

            PostScreenMessage(Colorize(message, Orange) + (both ? "\n" : "") + Colorize(note, OrangeAlpha), duration);
        }

        /// <summary>
        /// Quick message post without a quere. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public static void QuickPost(string message, float duration = 5.0f, String color = null)
        {
            if (color == null) PostScreenMessage(message, duration);

            if (UnityEngine.ColorUtility.TryParseHtmlString(color, out UnityEngine.Color c))
                PostScreenMessage(message, duration, color: c);
            else
                PostScreenMessage(message, duration);
        }

        
        public static void Add(string message, int key=0, string color = null)
        {
            try
            {
                messages.Add(key, new Duplet(message, color));
            }
            catch (Exception ex) when (
            ex is ArgumentNullException 
            || ex is ArgumentException 
            || ex is NotSupportedException) 
            {
                Logging.Log("Can't add message, index: " + key);
            }
        }

        public static void Append(string message, string color = null)
        {
            if (messages.Count == 0)
                messages.Add(0, new Duplet(message, color));
            else
                messages.Add(messages.Keys.Last() +1,new Duplet(message, color));
        }

        public static void Prepend(string message, string color = null)
        {
            if (messages.Count == 0)
                messages.Add(0, new Duplet(message, color));
            else
                messages.Add(messages.Keys.First() - 1, new Duplet(message, color));
        }

        public static void ShowAndClear(float duration = 5.0f, DurationType type = DurationType.CONST, String color = null, bool log = true)
        {
            if (log)
            {
                string message = String.Join(", ", from m in messages select m.Value.Message.Replace("\n", ", "));
                Log(message);
            }

            switch (type)
            {
                case DurationType.CONST:
                    {
                        string message = String.Join("\n", from m in messages select m.Value.Message);

                        QuickPost(message, duration, color);
                        break;
                    }
                case DurationType.CLEVERCONSTPERLINE:
                    {
                        string message = String.Join("\n", from m in messages select m.Value.Message);
                        var countOfLines = (from m in messages select m.Value.Message.Split('\n').Length).Sum();

                        QuickPost(message, countOfLines * duration, color);
                        break;
                    }
                case DurationType.INCREMENT:
                    {
                        int i = 1;
                        foreach (var pair in messages) QuickPost(pair.Value.Message, i++ * duration, color ?? pair.Value.Color);
                        break;
                    }
            }
            messages.Clear();
        }

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
        
        private static SortedDictionary<int, Duplet> messages = new SortedDictionary<int, Duplet>();

        class Duplet
        {
            public Duplet(string message, String color)
            {
                Message = message;
                Color = color;
            }

            public String Message { get; }
            public String Color { get; }
            
        }


        //private static void ShowDia
        //(string message = "You can't afford to launch this vessel.",
        //    string close_title = "Unable to Launch", float height = 100f)
        //{
        //    PopupDialog.SpawnPopupDialog(
        //        new MultiOptionDialog(
        //            "NotEnoughFunds", message, "Not Enough Funds!",
        //            HighLogic.UISkin,
        //            new Rect(0.5f, 0.5f, 300f, height),
        //            new DialogGUIButton(close_title, () => { }, true)
        //        ), false, HighLogic.UISkin, true);
        //}
    }
}
