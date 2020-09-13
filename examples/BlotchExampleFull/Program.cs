﻿using System;

namespace BlotchExample
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var win = new GameExample())
                win.Run();
        }
    }
}
