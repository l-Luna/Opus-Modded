using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modded_Opus{
	class CleanupVisitor : DepthFirstAstVisitor {

		public override void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression){
			// We end up with a lot of "(pm1924383734: true)" nonsense, which we really don't want, since that's not the parameter name
			// ...and also don't need, the parameters are in the right order. We'll just replace these with the underlying expression.
			namedArgumentExpression.ReplaceWith(namedArgumentExpression.GetChildByRole(Roles.Expression));
		}
	}
}