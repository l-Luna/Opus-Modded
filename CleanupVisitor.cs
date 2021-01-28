using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Modded_Opus{
	class CleanupVisitor : DepthFirstAstVisitor {

		// steamworks got a refactor, so DispatchDelegate<T> is now Callback<T>.DispatchDelegate
		// APIDispatchDelegate<T> is now CallResult<T>.APIDispatchDelegate

		// remove annotations at start of file

		// Current has to be an accessor
		// so Current(){ return ...; } has to be replaced with Current => ...;

		// "ref" should be replaced with "out"

		// replace method_505 with the actual string
		public override void VisitInvocationExpression(InvocationExpression invocationExpression) {
			base.VisitInvocationExpression(invocationExpression);
			if(Program.Strings.Count() > 0) {
				// maybe it'd be better to reference the original name, or ensure this gets a unique name... or just don't map it? assuming it never comes up
				if(invocationExpression.Target.LastChild is Identifier id) {
					// maybe it'd be better to reference the original name, or ensure this gets a unique name... or just don't map it? assuming it never comes up
					if(RemappingVisitor.IntermediaryWhenMapped(id.Name).Equals("method_505")) {
						if(invocationExpression.Arguments.Count == 1 && invocationExpression.Arguments.First() is PrimitiveExpression p && p.Value is int val) {
							if(Program.Strings.ContainsKey(val)) {
								invocationExpression.ReplaceWith(new PrimitiveExpression(Program.Strings[val]));
							} else {
								Console.WriteLine("Missing string for " + val + "!");
							}
						}
					}
				}
			}
		}
	}
}