using DiffPatch;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Modded_Opus {
	class Program {

		public static Dictionary<string, string> mappings = new Dictionary<string, string>();
		public static Dictionary<int, string> Strings = new Dictionary<int, string>();

		static void Main(string[] args) {
			// args[0] is the path to Opus Magnum EXE
			string exe = args[0];
			// args[1] is the path to mappings CSV
			string mappingsLoc = args[1];
			// if there's a third string, its the path out.csv
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

			Console.WriteLine("Writing nonsense -> intermediary...");
			using StreamWriter intermediaryFile = new StreamWriter("./intermediary.txt");
			foreach(KeyValuePair<string, string> kv in IdentifierCollectingVisitor.intermediary) {
				intermediaryFile.WriteLine(kv.Key + " -> " + kv.Value);
			}
			foreach(KeyValuePair<KeyValuePair<string, string>, string> kv in IdentifierCollectingVisitor.paramIntermediary) {
				intermediaryFile.WriteLine(kv.Key.Key + ", " + kv.Key.Value + " -> " + kv.Value);
			}

			string code = ast.ToString();

			// if there's a fourth string, its the path to patch.diff
			// apply patch and compile
			if(args.Length > 3) {
				Console.WriteLine("Applying compilation patch...");
				string patchFile = File.ReadAllText(args[3]);
				var diff = DiffParserHelper.Parse(patchFile);
				code = PatchHelper.Patch(code, diff.First().Chunks, "\n");

				Console.WriteLine("Recompiling...");
				SyntaxTree syntax = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions());
				var options = new CSharpCompilationOptions(
					OutputKind.WindowsApplication,
					optimizationLevel: OptimizationLevel.Release,
					allowUnsafe: true,
					warningLevel: 1,
					platform: Platform.X86
				);
				// File/Process/Directory doesn't exist
				var compilation = CSharpCompilation.Create("ModdedLightning", options: options)
					.AddReferences(new MetadataReference[]{
						// add libs
						MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
						MetadataReference.CreateFromFile(typeof(HashSet<object>).Assembly.Location),
						MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
						MetadataReference.CreateFromFile(Assembly.Load("mscorlib").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Extensions").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.IO").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.IO.FileSystem").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.IO.FileSystem.Watcher").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.Net.Requests").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.Diagnostics.Process").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.Private.Uri").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel.Primitives").Location),
						MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location),
						MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
						MetadataReference.CreateFromFile(typeof(Steamworks.CSteamID).Assembly.Location),
						MetadataReference.CreateFromFile(typeof(Ionic.Zip.ZipEntry).Assembly.Location)
					})
					.AddSyntaxTrees(syntax);
				using FileStream outputAssembly = new FileStream("./ModdedLightning.exe", FileMode.Create);
				var res = compilation.Emit(outputAssembly);
				if(res.Success) {
					Console.WriteLine("Successfully recompiled!");
					Console.WriteLine("(Press any key to continue.)");
					Console.ReadKey();
					Console.WriteLine("Writing runtime config & running batch...");

					string runtimeConfig = @"{ ""runtimeOptions"": { ""tfm"": ""netcoreapp3.1"", ""framework"": { ""name"": ""Microsoft.NETCore.App"", ""version"": ""3.1.0"" }}}";
					using StreamWriter configFile = new StreamWriter("./ModdedLightning.runtimeconfig.json");
					configFile.WriteLine(runtimeConfig);

					string runBatch = @"""C:\Program Files (x86)\dotnet\dotnet.exe"" ModdedLightning.exe";
					using StreamWriter batchFile = new StreamWriter("./runModded.bat");
					batchFile.WriteLine(runBatch);
				} else {
					Console.WriteLine("Recompilation failed with " + res.Diagnostics.Length + " errors.");
					foreach(var error in res.Diagnostics) {
						Console.WriteLine("Location: " + error.Location.GetLineSpan());
						Console.WriteLine("    " + error.GetMessage());
						Console.WriteLine("(Press any key to continue.)");
						Console.ReadKey();
					}
				}
			}

			Console.WriteLine("Writing code output...");
			using StreamWriter outputFile = new StreamWriter("./decomp.cs");
			outputFile.WriteLine(code);

			Console.WriteLine("Done!");
		}
	}
}