using System;
using NodeGraph.Editor;

namespace NodeGraph.Sandbox;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        EditorEntryPoint.Run(args);
    }
}
