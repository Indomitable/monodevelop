// 
// RefactoringOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Ide.Gui;
 
using System.Text;
using MonoDevelop.Projects.Text;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Core;
using MonoDevelop.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Refactoring
{
	public class RefactoringOptions
	{
		public ITypeResolveContext Dom {
			get;
			set;
		}
		
		public Document Document {
			get;
			set;
		}
		
		public object SelectedItem {
			get;
			set;
		}
		
		public ResolveResult ResolveResult {
			get;
			set;
		}
		
		// file provider for unit test purposes.
		public ITextFileProvider TestFileProvider {
			get;
			set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (Document.FileName);
			}
		}
		
		public AstLocation Location {
			get {
				return new AstLocation (Document.Editor.Caret.Line, Document.Editor.Caret.Column);
			}
		}
		
		public Mono.TextEditor.TextEditorData GetTextEditorData ()
		{
			return Document.Editor;
		}
		
		public static string GetWhitespaces (Document document, int insertionOffset)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = insertionOffset; i < document.Editor.Length; i++) {
				char ch = document.Editor.GetCharAt (i);
				if (ch == ' ' || ch == '\t') {
					result.Append (ch);
				} else {
					break;
				}
			}
			return result.ToString ();
		}

		public string OutputNode (AstNode node)
		{
			using (var stringWriter = new System.IO.StringWriter ()) {
				var formatter = new TextWriterOutputFormatter (stringWriter);
//				formatter.Indentation = indentLevel;
				stringWriter.NewLine = Document.Editor.EolMarker;
				
				var visitor = new OutputVisitor (formatter, new CSharpFormattingOptions ());
				node.AcceptVisitor (visitor, null);
				return stringWriter.ToString ();
			}
		}
		
		public CodeGenerator CreateCodeGenerator ()
		{
			var result = CodeGenerator.CreateGenerator (Document.Editor);
			if (result == null)
				LoggingService.LogError ("Generator can't be generated for : " + Document.Editor.MimeType);
			return result;
		}
		
		public static string GetIndent (Document document, IEntity member)
		{
			return GetWhitespaces (document, document.Editor.Document.LocationToOffset (member.Region.BeginLine, 1));
		}
		
		public string GetWhitespaces (int insertionOffset)
		{
			return GetWhitespaces (Document, insertionOffset);
		}
		
		public string GetIndent (IEntity member)
		{
			return GetIndent (Document, member);
		}
//		
//		public IReturnType ShortenTypeName (IReturnType fullyQualifiedTypeName)
//		{
//			return Document.ParsedDocument.CompilationUnit.ShortenTypeName (fullyQualifiedTypeName, Document.Editor.Caret.Line, Document.Editor.Caret.Column);
//		}
//		
//		public ParsedDocument ParseDocument ()
//		{
//			return ProjectDomService.Parse (Dom.Project, Document.FileName, Document.Editor.Text);
//		}
		
		public List<string> GetUsedNamespaces ()
		{
			var result = new List<string> ();
			var pf = Document.ParsedDocument.Annotation<ParsedFile> ();
			if (pf == null)
				return result;
			var scope = pf.GetUsingScope (Location);
			if (scope == null)
				return result;
			var ctx = Document.TypeResolveContext;
			
			for (var n = scope; n != null; n = n.Parent) {
				result.Add (n.NamespaceName);
				result.AddRange (n.Usings.Select (u => u.ResolveNamespace (ctx))
					.Where (nr => nr != null)
					.Select (nr => nr.NamespaceName));
			}
			return result;
		}
		
		
//		public List<string> GetResolveableNamespaces (RefactoringOptions options, out bool resolveDirect)
//		{
//			IReturnType returnType = null; 
//			INRefactoryASTProvider astProvider = RefactoringService.GetASTProvider (DesktopService.GetMimeTypeForUri (options.Document.FileName));
//			
//			if (options.ResolveResult != null && options.ResolveResult.ResolvedExpression != null) {
//				if (astProvider != null) 
//					returnType = astProvider.ParseTypeReference (options.ResolveResult.ResolvedExpression.Expression).ConvertToReturnType ();
//				if (returnType == null)
//					returnType = DomReturnType.GetSharedReturnType (options.ResolveResult.ResolvedExpression.Expression);
//			}
//			
//			List<string> namespaces;
//			if (options.ResolveResult is UnresolvedMemberResolveResult) {
//				namespaces = new List<string> ();
//				UnresolvedMemberResolveResult unresolvedMemberResolveResult = options.ResolveResult as UnresolvedMemberResolveResult;
//				IType type = unresolvedMemberResolveResult.TargetResolveResult != null ? options.Dom.GetType (unresolvedMemberResolveResult.TargetResolveResult.ResolvedType) : null;
//				if (type != null) {
//					List<IType> allExtTypes = DomType.GetAccessibleExtensionTypes (options.Dom, null);
//					foreach (ExtensionMethod method in type.GetExtensionMethods (allExtTypes, unresolvedMemberResolveResult.MemberName)) {
//						string ns = method.OriginalMethod.DeclaringType.Namespace;
//						if (!namespaces.Contains (ns) && !options.Document.CompilationUnit.Usings.Any (u => u.Namespaces.Contains (ns)))
//							namespaces.Add (ns);
//					}
//				}
//				resolveDirect = false;
//			} else {
//				namespaces = new List<string> (options.Dom.ResolvePossibleNamespaces (returnType));
//				resolveDirect = true;
//			}
//			for (int i = 0; i < namespaces.Count; i++) {
//				for (int j = i + 1; j < namespaces.Count; j++) {
//					if (namespaces[j] == namespaces[i]) {
//						namespaces.RemoveAt (j);
//						j--;
//					}
//				}
//			}
//			return namespaces;
//		}
	}
}
