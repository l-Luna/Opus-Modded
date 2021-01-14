using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using System;
using System.Collections.Generic;
using System.IO;

namespace Modded_Opus {
	class Program {
		static void Main(string[] args) {
			// args[0] is path to Opus Magnum EXE
			string exe = args[0];

			var module = new PEFile(exe);
			var decompiler = new CSharpDecompiler(exe, new UniversalAssemblyResolver(exe, false, module.DetectTargetFrameworkId()), new DecompilerSettings());
			// now to remap and output to files
			var ast = decompiler.DecompileWholeModuleAsSingleFile();
			// we now have a syntax tree
			// we just need to walk it, modify class and member name references, and then output to files
			ast.AcceptVisitor(new ClassCollectingVisitor());
			ast.AcceptVisitor(new ClassRemappingVisitor());
			//ast.AcceptVisitor(new EntrypointAddingVisitor());

			using StreamWriter mappingsFile = new StreamWriter("./generated.txt");
			foreach(KeyValuePair<string, string> kv in ClassCollectingVisitor.mappings) {
				mappingsFile.WriteLine(kv.Key + " -> " + kv.Value);
			}
			foreach(KeyValuePair<KeyValuePair<string, string>, string> kv in ClassCollectingVisitor.paramMappings) {
				mappingsFile.WriteLine(kv.Key.Key + ", " + kv.Key.Value + " -> " + kv.Value);
			}

			string code = ast.ToString();
			using StreamWriter outputFile = new StreamWriter("./whatever.txt");
			outputFile.WriteLine(code);

			Console.WriteLine("Done! See generated.txt for generated mappings, whatever.txt for source (as a single file).");
		}
	}
}