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

			// for every line that doesn't start with a "#", split by "," and add to mappings
			string[] mappingsFile = File.ReadAllLines(mappingsLoc);
			foreach(var line in mappingsFile){
				if(!line.StartsWith("#")){
					string[] split = line.Split(",", 2);
					mappings.Add(split[0], split[1]);
				}
			}

			var module = new PEFile(exe);
			var decompiler = new CSharpDecompiler(exe, new UniversalAssemblyResolver(exe, false, module.DetectTargetFrameworkId()), new DecompilerSettings());
			// now to remap and output to files
			var ast = decompiler.DecompileWholeModuleAsSingleFile();
			// we now have a syntax tree
			// we just need to walk it, modify class and member name references, and then output to files
			ast.AcceptVisitor(new IdentifierCollectingVisitor());
			ast.AcceptVisitor(new RemappingVisitor());
			//ast.AcceptVisitor(new EntrypointAddingVisitor());

			using StreamWriter intermediaryFile = new StreamWriter("./intermediary.txt");
			foreach(KeyValuePair<string, string> kv in IdentifierCollectingVisitor.intermediary) {
				intermediaryFile.WriteLine(kv.Key + " -> " + kv.Value);
			}
			foreach(KeyValuePair<KeyValuePair<string, string>, string> kv in IdentifierCollectingVisitor.paramIntermediary) {
				intermediaryFile.WriteLine(kv.Key.Key + ", " + kv.Key.Value + " -> " + kv.Value);
			}

			string code = ast.ToString();
			using StreamWriter outputFile = new StreamWriter("./decomp.txt");
			outputFile.WriteLine(code);

			Console.WriteLine("Done! See intermediary.txt for generated mappings, decomp.txt for source.");
		}
	}
}