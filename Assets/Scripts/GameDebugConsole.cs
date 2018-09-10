using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public static class GameDebugConsole
{
    private class ConsoleEntry
    {
        public float duration;
        public float age;
        public string text;
        public string id;
        public int priority;
    }

    private static List<ConsoleEntry> consoleEntries;

    static GameDebugConsole()
    {
        consoleEntries = new List<ConsoleEntry>();
    }

    public static void Log(string text, float duration = -1.0f, string id = null)
    {
        void CreateEntry(int priority)
        {
            var entry = new ConsoleEntry();
            entry.id = id;
            entry.text = text;
            entry.duration = duration;
            entry.priority = priority;
            entry.age = 0.0f;
            consoleEntries.Add(entry);
        }

        if (id == null)
        {
            CreateEntry(1);
        }
        else
        {
            var existing = consoleEntries.Find(entry => (entry.id == null) ? false : entry.id == id);
            if (existing != null)
                existing.text = text;
            else
                CreateEntry(0);
        }
    }

    public static void Draw(float deltaTime)
    {
        int line = 0;
        int lineWidth = 20;

        consoleEntries.OrderByDescending(entry => entry.priority);

        var expiredEntries = new List<ConsoleEntry>();
        foreach (var entry in consoleEntries)
        {
            void DrawEntry()
            {
                GUI.Box(new Rect(10, line * lineWidth, lineWidth * 8, lineWidth), "");
                GUI.Label(new Rect(14, line * lineWidth, 100, 20), entry.text);
            }

            if (entry.duration > 0.0f)
            {
                entry.age += deltaTime;
                if (entry.age >= entry.duration)
                    expiredEntries.Add(entry);
                else
                    DrawEntry();
            }
            else
            {
                DrawEntry();
            }

            line += 1;
        }

        foreach (var expired in expiredEntries)
        {
            consoleEntries.Remove(expired);
        }
    }

}

