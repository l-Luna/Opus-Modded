using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modded_Opus {
	class RemappingVisitor : DepthFirstAstVisitor {

		// on every class or member reference, use IdentifierCollectingVisitor's table to either add the correct mapping, or use intermediary

		public override void VisitIdentifier(Identifier identifier){
			// If I'm in any methods, replace references to parameters
			foreach(EntityDeclaration method in identifier.Ancestors.OfType<MethodDeclaration>().Union<EntityDeclaration>(identifier.Ancestors.OfType<ConstructorDeclaration>())){
				// Get the corresponding parameter name
				KeyValuePair<string, string> param = KeyValuePair.Create<string, string>(IntermediaryWhenMapped(method.Name), identifier.Name);
				if(IdentifierCollectingVisitor.paramIntermediary.ContainsKey(param)){
					identifier.ReplaceWith(Identifier.Create(GetMappedOrIntermediary(IdentifierCollectingVisitor.paramIntermediary[param])));
					return;
				}
			}
			identifier.ReplaceWith(Identifier.Create(GetMappedOrIntermediary(identifier.Name)));
		}

		private string GetMappedOrIntermediary(string nonsense){
			if(IdentifierCollectingVisitor.intermediary.ContainsKey(nonsense))
				nonsense = IdentifierCollectingVisitor.intermediary[nonsense];
			if(Program.mappings.ContainsKey(nonsense))
				return Program.mappings[nonsense];
			// if we've got something invalid, i.e. nonsense still or "CS$<>___8" whatever, we'll remap that to "p" + the hash of the nonsense
			// this should effectively only touch locals
			// and is just for getting this to compile
			// in the future a better solution can be made
			if(nonsense.StartsWith("#=") || nonsense.Contains("$") || nonsense.Contains("<"))
				return ("p" + nonsense.GetHashCode()).Replace("-", "m");
			return nonsense;
		}

		private string IntermediaryWhenMapped(string name){
			var result = Program.mappings.FirstOrDefault(x => x.Value == name);
			if(!result.Equals(new KeyValuePair<string, string>()))
				return result.Key;
			else return name;
		}
	}
}