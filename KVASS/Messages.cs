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
        /// <summary>
        /// Add fail message and optional note to the quere.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="note"></param>
        public static void Add(string message, string note = null)
        {
            fail_messages.Add(
                new Tuple<string, string>(message, note)
            );
        }

        /// <summary>
        /// Show all messages from the quere, and clear the quere afterwards.
        /// </summary>
        /// <param name="seconds"></param>
        public static void ShowAndClear(int seconds = 5)
        {
            for (int i = 0; i < fail_messages.Count; i++)
            {
                string message = fail_messages[i].Item1;
                string note = fail_messages[i].Item2;
                bool both = !String.IsNullOrEmpty(message) && !String.IsNullOrEmpty(note);

                PostScreenMessage(
                    Asterix(Orange(message)) + (both?"\n":"") + note,
                    seconds * (i + 1)
                );
            }
                
            Clear();
        }
        /// <summary>
        /// Amount of messages in the quere.
        /// </summary>
        /// <returns></returns>
        public static int Count()
        {
            return fail_messages.Count;
        }

        private static void Clear()
        {
            fail_messages.Clear();
        }

        /// <summary>
        /// Quick message post without a quere. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public static void QuickPost(string message, float duration = 5.0f) 
            => PostScreenMessage(message, duration);



        private static List<Tuple<string, string>> fail_messages = new List<Tuple<string, string>>();

        private static string Orange(string message) => "<color=orange>" + message + "</color>"; // #ffa500ff
        private static string OrangeAlpha(string message) => "<color=#ffa500af>" + message + "</color>";
        private static string Red(string message) => "<color=red>" + message + "</color>";

        private static string Asterix(string message, bool? enclose = null)
        {
            bool Enclose = enclose.HasValue ? enclose.Value : fail_messages.Count > 1;

            if (Enclose)
                return Red("* ") + message + Red(" *");
            else
                return message;

        }

        private static void PostScreenMessage(string message, float duration = 5.0f,
            ScreenMessageStyle style = ScreenMessageStyle.UPPER_CENTER)
        {
            ScreenMessages.PostScreenMessage(message, duration, style);
        }

       
    }
}
