using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modded_Opus {
	class RemappingVisitor : DepthFirstAstVisitor {

		// on every class or member reference, use IdentifierCollectingVisitor's table to either add the correct mapping, or use intermediary

		public override void VisitIdentifier(Identifier identifier){
			if(IdentifierCollectingVisitor.intermediary.ContainsKey(identifier.Name)){
				identifier.ReplaceWith(Identifier.Create(getMappedOrIntermediary(identifier.Name)));
				return;
			}
			// If I'm in any methods, replace references to parameters
			foreach(EntityDeclaration method in identifier.Ancestors.OfType<MethodDeclaration>().Union<EntityDeclaration>(identifier.Ancestors.OfType<ConstructorDeclaration>())){
				// Get the corresponding parameter name
				KeyValuePair<string, string> param = KeyValuePair.Create<string, string>(intermediaryWhenMapped(method.Name), identifier.Name);
				if(IdentifierCollectingVisitor.paramIntermediary.ContainsKey(param)){
					identifier.ReplaceWith(Identifier.Create(getMappedOrIntermediary(IdentifierCollectingVisitor.paramIntermediary[param])));
					return;
				}
			}
		}

		private string getMappedOrIntermediary(string nonsense){
			if(IdentifierCollectingVisitor.intermediary.ContainsKey(nonsense))
				nonsense = IdentifierCollectingVisitor.intermediary[nonsense];
			if(Program.mappings.ContainsKey(nonsense))
				return Program.mappings[nonsense];
			return nonsense;
		}

		private string intermediaryWhenMapped(string name){
			var result = Program.mappings.FirstOrDefault(x => x.Value == name);
			if(!result.Equals(new KeyValuePair<string, string>()))
				return result.Key;
			else return name;
		}
	}
}