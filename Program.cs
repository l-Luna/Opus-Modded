using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using System;
using System.Collections.Generic;
using System.IO;

namespace Modded_Opus {
	class Program {

		public static Dictionary<string, string> mappings = new Dictionary<string, string>();

		static void Main(string[] args) {
			// args[0] is path to Opus Magnum EXE
			string exe = args[0];
			// args[1] is path to mappings CSV
			string mappingsLoc = args[1];

			Console.WriteLine("Reading mappings...");
			// for every line that doesn't start with a "#", split by "," and add to mappings
			string[] mappingsFile = File.ReadAllLines(mappingsLoc);
			foreach(var line in mappingsFile){
				if(!line.StartsWith("#") && !line.Trim().Equals("")){
					string[] split = line.Split(",", 2);
					mappings.Add(split[0], split[1]);
				}
			}

			var module = new PEFile(exe);
			var decompiler = new CSharpDecompiler(exe, new UniversalAssemblyResolver(exe, false, module.DetectTargetFrameworkId()), new DecompilerSettings() {
				NamedArguments = false
			});

			// decompile
			Console.WriteLine("Decompiling...");
			var ast = decompiler.DecompileWholeModuleAsSingleFile();

			// we now have a syntax tree
			// we just need to walk it, modify class and member name references, and then output to files
			Console.WriteLine("Collecting intermediary names...");
			ast.AcceptVisitor(new IdentifierCollectingVisitor());

			Console.WriteLine("Remapping...");
			ast.AcceptVisitor(new RemappingVisitor());

			Console.WriteLine("Cleaning up invalid code...");
			ast.AcceptVisitor(new CleanupVisitor());

			// some params are in the wrong place...

			//Console.WriteLine("Adding modded entry point...");
			//ast.AcceptVisitor(new EntrypointAddingVisitor());

			Console.WriteLine("Writing output...");
			using StreamWriter intermediaryFile = new StreamWriter("./intermediary.txt");
			foreach(KeyValuePair<string, string> kv in IdentifierCollectingVisitor.intermediary) {
				intermediaryFile.WriteLine(kv.Key + " -> " + kv.Value);
			}
			foreach(KeyValuePair<KeyValuePair<string, string>, string> kv in IdentifierCollectingVisitor.paramIntermediary) {
				intermediaryFile.WriteLine(kv.Key.Key + ", " + kv.Key.Value + " -> " + kv.Value);
			}

			string code = ast.ToString();
			using StreamWriter outputFile = new StreamWriter("./decomp.cs");
			outputFile.WriteLine(code);

			Console.WriteLine("Applying compilation patch...");
			Console.WriteLine("Done!");
		}
	}
}