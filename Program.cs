using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Modded_Opus {
	class Program {

		public static Dictionary<string, string> mappings = new Dictionary<string, string>();
		public static Dictionary<int, string> Strings = new Dictionary<int, string>();

		static void Main(string[] args) {
			// args[0] is path to Opus Magnum EXE
			string exe = args[0];
			// args[1] is path to mappings CSV
			string mappingsLoc = args[1];
			// if there's a third string, its out.csv
			if(args.Length > 2) {
				Console.WriteLine("Reading strings...");
				string[] lines = File.ReadAllLines(args[2]);
				bool hadSplit = true; // multi-line strings
				int lastIndex = 0;
				foreach(string line in lines) {
					string[] split = line.Split("~,~");
					if(split.Length > 1) {
						// if we *can* split on this line, then we're definitely at the first line of a string
						hadSplit = true;
						try {
							lastIndex = int.Parse(split[0]);
							Strings.Add(lastIndex, split[1]);
						} catch(ArgumentException) { }
					} else if(!hadSplit) {
						// if this line isn't blank (or even if it is), then we're continuing a previous multi-line string, so append
						Strings[lastIndex] = Strings[lastIndex] + "\n" + line;
					}
				}
				// these are ridden with special characters
				// we can't just trim normally, see "fmt " breaking WAV loading
				// so we manually regex replace: [^a-zA-Z0-9_.:\n;'*()+<>\\{}# ,~/$\[\]\-©!"?&’\t=—@%●●●●…—……] is removed
				// this kills other languages, a better solution is needed in the future
				foreach(int key in Strings.Keys.ToList())
					Strings[key] = Regex.Replace(Strings[key], "[^a-zA-Z0-9_.:\n;'*()+<>\\\\{}# ,~/$\\[\\]\\-©!\" ? &’\t =—@%●●●●…—……]", "");
			}

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

			//Console.WriteLine("Applying compilation patch...");
			Console.WriteLine("Done!");
		}
	}
}