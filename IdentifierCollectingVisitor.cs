using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using System;
using System.Collections.Generic;

namespace Modded_Opus {
	class IdentifierCollectingVisitor : DepthFirstAstVisitor {

		// on every class and member declaration,
		// add a dictionary entry from it to some intermediary name

		public static Dictionary<string, string> intermediary = new Dictionary<string, string>();
		public static Dictionary<KeyValuePair<string, string>, string> paramIntermediary = new Dictionary<KeyValuePair<string, string>, string>();
		static int classIndex = 0, methodIndex = 0, fieldIndex = 0, parameterIndex = 0;

		public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration){
			string className = "class_" + classIndex;
			try{
				intermediary.Add(typeDeclaration.Name, className);
				classIndex++;
			}catch(ArgumentException){
				// There's a lot of "<>c"s, presumably for Serialization. These can have duplicate names.
			}
			foreach(EntityDeclaration member in typeDeclaration.Members){
				// visit-field and visit-method didn't work because I forgot to call base.VisitTypeDeclaration(...) and I'm too lazy to split them back
				if(member is FieldDeclaration field){
					// Names are stored in the Variables
					foreach(VariableInitializer variable in field.Variables){
						try{
							intermediary.Add(variable.Name, "field_" + fieldIndex);
							fieldIndex++;
						}catch(ArgumentException){
							// I assume(tm) that duplicate field names mean duplicate original names.
						}
					}
				}
				if(member is MethodDeclaration){
					// Name works fine here though - don't know what's up with that.
					try{
						intermediary.Add(member.Name, "method_" + methodIndex);
						methodIndex++;
					}catch(ArgumentException){
						// Not a problem if we have duplicate method names:
						//  - happens for non-obfuscated methods, where we don't want to remap them anyways
						//  - happens for overriden methods, where they need to be remapped to the same name
					}
				}
			}
			base.VisitTypeDeclaration(typeDeclaration);
		}

		public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration) {
			// Parameters do actually have distinct duplicates
			// Parent will be an entity (method or ctor) decleration, get the name from there
			if(parameterDeclaration.Parent is EntityDeclaration declaration) {
				try{
					// Use intermediary method name, if possible
					// Operators don't get intermediary...????
					paramIntermediary.Add(KeyValuePair.Create(intermediary.ContainsKey(declaration.Name) ? intermediary[declaration.Name] : declaration.Name, parameterDeclaration.Name), "parameter_" + parameterIndex);
					parameterIndex++;
				}catch(ArgumentException){
					// This means that methods with the same name have a parameter of the same name
					// This only happens for overrides
					// For now, I'll map those to the same name
					// If need be, the class can be added as context later
				}
			}
			// Or an anonymous method
			else {
				try{
					intermediary.Add(parameterDeclaration.Name, "parameter_" + parameterIndex);
					parameterIndex++;
				}catch(ArgumentException){
					// Non-unique names, but duplicate names are probably duplicate in the original source so idc
				}
			}
		}
	}
}