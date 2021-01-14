using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modded_Opus {
	class ClassRemappingVisitor : DepthFirstAstVisitor {

		// on every class or member reference, use ClassCollectingVisitor's table to either add the correct mapping, or use intermediary

		public override void VisitIdentifier(Identifier identifier) {
			if(ClassCollectingVisitor.mappings.ContainsKey(identifier.Name)){
				identifier.ReplaceWith(Identifier.Create(ClassCollectingVisitor.mappings[identifier.Name]));
				return;
			}
			// If I'm in any methods, replace references to parameters
			foreach(EntityDeclaration method in identifier.Ancestors.OfType<MethodDeclaration>().Union<EntityDeclaration>(identifier.Ancestors.OfType<ConstructorDeclaration>())){
				// Get the corresponding parameter name
				KeyValuePair<string, string> param = KeyValuePair.Create<string, string>(method.Name, identifier.Name);
				if(ClassCollectingVisitor.paramMappings.ContainsKey(param)) {
					identifier.ReplaceWith(Identifier.Create(ClassCollectingVisitor.paramMappings[param]));
					return;
				}
			}
		}
	}
}