using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface
{
    static class MessageManager
    {
        public static enumMessageLevel MessageLevel =
            enumMessageLevel.ProgressLog | enumMessageLevel.DetailProgressLog;
        private static int TabLevel = 0;
        private static bool NeedToAppendLine = true;
        
        public static void GoInnerTab()
        {
            TabLevel = Math.Max(TabLevel + 4,0);
        }

        public static void GoOuterTab()
        {
            TabLevel = Math.Max(TabLevel - 4,0);
        }

        private static void ShowText(string text)
        {
            if (NeedToAppendLine)
            {
                Console.Write("\r\n" + new string(' ',TabLevel));
                NeedToAppendLine = false;
            }

            text = text.Replace("\r\n","\r\n" + new string(' ',TabLevel));
            Console.Write(text);
        }

        public static bool TestLevel(enumMessageLevel level)
        {
            if ((MessageLevel & level) != level)
                return false;
            return true;
        }

        public static void Show(string text,enumMessageLevel level = enumMessageLevel.ProgressLog)
        {
            if ((MessageLevel & level) != level)
                return;

            if (text.EndsWith("\r\n"))
                ShowLine(text.Substring(0,text.Length - 2));
            else
                ShowText(text);
        }

        public static void ShowLine(string text,enumMessageLevel level = enumMessageLevel.ProgressLog)
        {
            if ((MessageLevel & level) != level)
                return;
            ShowText(text);
            NeedToAppendLine = true;
        }

        public static void ShowErrors(List<Assemble.AssembleError> errorList)
        {
            Console.WriteLine("Error occurs:");
            foreach (var e in errorList)
            {
                Console.WriteLine($"* { e.Title }: { e.Detail }");
                Console.WriteLine($"   At { e.Position.GenerateExplainText() }");
            }
        }

        public static void SetLevel(string mode)
        {
            enumMessageLevel level = enumMessageLevel.None;
            for (int i = 0; i < mode.Length; i++)
            {
                char c = mode[i];
                switch (c)
                {
                    case 'e':
                        level |= enumMessageLevel.ExecutionLog;
                        break;
                    case 'E':
                        level |= enumMessageLevel.ExecutionDetailLog;
                        break;
                    case 'i':
                        level |= enumMessageLevel.InfomationLog;
                        break;
                    case 'I':
                        level |= enumMessageLevel.InfomationDetailLog;
                        break;
                    case 'p':
                        level |= enumMessageLevel.ProgressLog;
                        break;
                    case 'P':
                        level |= enumMessageLevel.DetailProgressLog;
                        break;
                }
            }

            MessageLevel = level;
        }
    }

    [Flags]
    public enum enumMessageLevel
    {
        None = 0,
        ProgressLog = 0x1,
        DetailProgressLog = 0x2,
        ExecutionLog = 0x8,
        ExecutionDetailLog = 0x10,
        InfomationLog = 0x20,
        InfomationDetailLog = 0x40
    }
}
