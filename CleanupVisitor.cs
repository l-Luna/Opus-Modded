using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modded_Opus{
	class CleanupVisitor : DepthFirstAstVisitor {

		// steamworks got a refactor, so DispatchDelegate<T> is now Callback<T>.DispatchDelegate and APIDispatchDelegate<T> is now CallResult<T>.APIDispatchDelegate
	}
}