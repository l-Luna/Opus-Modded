using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Linq;

namespace Modded_Opus {
	class CleanupVisitor : DepthFirstAstVisitor {

		// steamworks got a refactor, so DispatchDelegate<T> is now Callback<T>.DispatchDelegate
		// APIDispatchDelegate<T> is now CallResult<T>.APIDispatchDelegate
		public override void VisitSimpleType(SimpleType simpleType) {
			base.VisitSimpleType(simpleType);
			// the types have to be mapped to DispatchDelegate and APIDispatchDelegate or it won't compile anyways
			// just don't map anything to those
			if(simpleType.TypeArguments.Count() == 1) {
				// the types have to be mapped to DispatchDelegate and APIDispatchDelegate or it won't compile anyways
				// just don't map anything to those
				if(simpleType.Identifier.Equals("DispatchDelegate")) {
					simpleType.ReplaceWith(new MemberType(new SimpleType("Callback", simpleType.TypeArguments.First().Clone()), "DispatchDelegate"));
				} else if(simpleType.Identifier.Equals("APIDispatchDelegate")) {
					simpleType.ReplaceWith(new MemberType(new SimpleType("CallResult", simpleType.TypeArguments.First().Clone()), "APIDispatchDelegate"));
				}
			}
		}

		// remove annotations at start of file

		// classes 207-213 should be public
		public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration) {
			base.VisitTypeDeclaration(typeDeclaration);
			// give these unique names too
			string id = RemappingVisitor.IntermediaryWhenMapped(typeDeclaration.Name);
			if(id == "class_207" || id == "class_208" || id == "class_209" || id == "class_210" || id == "class_211" || id == "class_212" || id == "class_213") {
				typeDeclaration.Modifiers = Modifiers.Public;
			}
		}

		// Current has to be an accessor
		public override void VisitMethodDeclaration(MethodDeclaration method) {
			base.VisitMethodDeclaration(method);
			// the method has to be mapped to Current or it won't compile anyways
			// just don't map anything to Current that looks like Enumerator's current
			if(method.Name.Equals("Current") && method.Body.Count() == 1 && method.Body.First() is ReturnStatement){
				var prop = new PropertyDeclaration();
				prop.ReturnType = method.ReturnType.Clone();
				prop.Name = (string)method.Name.Clone();
				prop.PrivateImplementationType = method.PrivateImplementationType.Clone();
				var acc = new Accessor();
				acc.AddChild(new CSharpTokenNode(TextLocation.Empty, PropertyDeclaration.GetKeywordRole), PropertyDeclaration.GetKeywordRole);
				acc.Body = (BlockStatement)method.Body.Clone();
				prop.Getter = acc;
				method.ReplaceWith(prop);
			}
		}

		// replace method_505 with the actual string
		public override void VisitInvocationExpression(InvocationExpression invocation) {
			base.VisitInvocationExpression(invocation);
			if(Program.Strings.Count() > 0) {
				// maybe it'd be better to reference the original name, or ensure this gets a unique name... or just don't map it? assuming it never comes up
				if(invocation.Target.LastChild is Identifier id) {
					// maybe it'd be better to reference the original name, or ensure this gets a unique name... or just don't map it? assuming it never comes up
					if(RemappingVisitor.IntermediaryWhenMapped(id.Name).Equals("method_505")) {
						if(invocation.Arguments.Count == 1 && invocation.Arguments.First() is PrimitiveExpression p && p.Value is int val) {
							if(Program.Strings.ContainsKey(val)) {
								invocation.ReplaceWith(new PrimitiveExpression(Program.Strings[val]));
							} else {
								Console.WriteLine("Missing string for " + val + "!");
							}
						}
					}
				}
			}
		}

		// Count sometimes misses its brackets
		// implicit Maybe doesn't work for NormalInputMode, must be manually changed to Maybe.Of
	}
}