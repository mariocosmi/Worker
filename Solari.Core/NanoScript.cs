/// <summary>
/// NanoScript.
/// 
/// A lightweight scripting engine for dot net
/// (C) Zimmermann Stephan 2009
/// 
/// </summary>

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using System.Reflection;
using System.Runtime.Remoting;
using System.Reflection.Emit;

namespace NanoScript {

	public class ScriptEngine {
		public delegate object MethodDelegate(params object[] paramters);

		public class ScriptingContextException : Exception {
			public class CompileError {
				public String text = String.Empty;
				public String line = String.Empty;
				public String column = String.Empty;
				public String errNumber = String.Empty;
				public String codeSnippet = String.Empty;
			}

			public List<CompileError> errors = null;
			public ScriptingContextException(String message)
				: base(message) {

			}
			public ScriptingContextException(String message, List<CompileError> errors)
				: base(message) {
				this.errors = errors;
			}
		}

		#region members

		CompilerParameters parameters = null;
		Hashtable code_table = new Hashtable();
		Hashtable vars = new Hashtable();
		Hashtable vars_defaults = new Hashtable();
		Hashtable methods = new Hashtable();
		Hashtable get_properties = new Hashtable();
		Hashtable set_properties = new Hashtable();
		Hashtable fields = new Hashtable();
		Hashtable external_functions = new Hashtable();
		Assembly compiledAssembly = null;
		Hashtable usingStatements = new Hashtable();
		Hashtable referencedAssemblies = new Hashtable();
		Object hinstance = null;
		bool isCompiled = false;

		#endregion

		public ScriptEngine() {
			parameters = new CompilerParameters();
			parameters.GenerateInMemory = true;
			parameters.GenerateExecutable = false;
			parameters.TreatWarningsAsErrors = true;
			parameters.CompilerOptions = "/nowarn:1633";

			usingStatements["System"] = "System";
			//IIS			referencedAssemblies["System.dll"] = "System.dll";
			//IIS			referencedAssemblies["mscorlib.dll"] = "mscorlib.dll";
			//IIS			referencedAssemblies["System.Data.dll"] = "System.Data.dll";                      
		}

		public bool IsCompiled {
			get { return isCompiled; }
		}

		// add a reference to a used dll to the script context
		public void References(string assembly) {
			return;
			//IIS
			if (!referencedAssemblies.ContainsKey(assembly)) {
				referencedAssemblies[assembly] = assembly;
			}
		}

		// add using statements to the scripting context, 
		// to make namespaces visible
		public void Using(string Namespace) {
			if (!usingStatements.ContainsKey(Namespace)) {
				usingStatements[Namespace] = Namespace;
			}

		}

		// export a static method of a certain assembly to the 
		// scripting context
		public void SetStaticFunction(Type typeOfAssembly, String MethodName) {
			MethodInfo inf = typeOfAssembly.GetMethod(MethodName);
			if (inf != null) {
				ScriptEngine.MethodDelegate func = delegate(object[] paramters) { return typeOfAssembly.GetMethod(MethodName).Invoke(null, paramters); };
				external_functions[MethodName] = func;
			} else {
				throw new ScriptingContextException("method " + MethodName + " is not declared");
			}
		}

		// export a member function of a given object instance to the scripting context 
		public void SetMemberFunction(object instance, String MethodName) {
			Type tp = instance.GetType();
			MethodInfo inf = tp.GetMethod(MethodName);
			if (inf != null) {
				ScriptEngine.MethodDelegate func = delegate(object[] paramters) { return tp.GetMethod(MethodName).Invoke(instance, paramters); };
				external_functions[MethodName] = func;
			} else {
				throw new ScriptingContextException("method " + MethodName + " is not declared in type " + tp.FullName);
			}
		}

		// add code to the scripting context
		public void SetCode(String func) {
			code_table[func.GetHashCode()] = func;
		}

		public void ClearCode() {
			code_table.Clear();
		}

		// declare a global variable visible in the scriptingcontext
		// and acessible from outside of it
		public void DeclareGlobal(string name, Type t) {
			vars[name] = t;
		}

		// declare a global variable and assign a default value to it
		public void DeclareGlobal(string name, Type t, object defaultvalue) {
			vars[name] = t;
			vars_defaults[name] = defaultvalue;
		}

		// assign a value to a global variable
		public void GlobalSet(string name, object value) {
			String methodName = "set_PropertyFunc" + name;

			if (hinstance == null) {
				throw new ScriptingContextException("Script is not compiled");
			}

			IDictionaryEnumerator it = set_properties.GetEnumerator();
			while (it.MoveNext()) {
				MethodInfo inv = (MethodInfo)it.Key;

				if (inv.Name == methodName) {
					inv.Invoke(hinstance, new object[] { value });
					return;
				}
			}

			throw new ScriptingContextException("variable " + name + " is not declared");
		}

		// get the value of a global variable
		public object GlobalGet(string name) {
			String methodName = "get_PropertyFunc" + name;

			if (hinstance == null) {
				throw new ScriptingContextException("Script is not compiled");
			}

			IDictionaryEnumerator it = get_properties.GetEnumerator();
			while (it.MoveNext()) {
				MethodInfo inv = (MethodInfo)it.Key;

				if (inv.Name == methodName) {
					return inv.Invoke(hinstance, new object[] { });
				}
			}

			throw new ScriptingContextException("variable " + name + " is not declared");
		}

		public MethodDelegate GetFunction(string methodName) {
			if (hinstance == null) {
				throw new ScriptingContextException("Script is not compiled");
			}

			IDictionaryEnumerator it = methods.GetEnumerator();
			while (it.MoveNext()) {
				MethodInfo inv = (MethodInfo)it.Key;
				ParameterInfo[] pars = (ParameterInfo[])it.Value;

				if (inv.Name == methodName) {
					if (pars.Length == 0) {
						return delegate(object[] paramters) { return inv.Invoke(hinstance, null); };
					} else {
						return delegate(object[] paramters) { return inv.Invoke(hinstance, paramters); };
					}
				}

			}

			throw new ScriptingContextException("method " + methodName + " is not implemented");

		}

		public object Execute(string methodName, params object[] parameters) {
			if (hinstance == null) {
				throw new ScriptingContextException("Script is not compiled");
			}

			IDictionaryEnumerator it = methods.GetEnumerator();
			while (it.MoveNext()) {
				MethodInfo inv = (MethodInfo)it.Key;
				ParameterInfo[] pars = (ParameterInfo[])it.Value;

				if (inv.Name == methodName) {
					return inv.Invoke(hinstance, parameters);
					//try
					//{
					//    return inv.Invoke(hinstance, parameters);
					//}
					//catch (Exception ex)
					//{
					//    Console.WriteLine("Invoked function caused exception: " + ex.Message);
					//}
				}
			}

			throw new ScriptingContextException("method " + methodName + " is not implemented");
		}



		public void Compile() {
			isCompiled = false;
			if (hinstance != null) {
				throw new ScriptingContextException("Script is already compiled");
			}

			StringBuilder code = new StringBuilder(4096);
			code.AppendLine("//Auto-generated file");

			//IIS            parameters.ReferencedAssemblies.Add("Solari.Core.dll");
			parameters.TreatWarningsAsErrors = false;

			IDictionaryEnumerator it = referencedAssemblies.GetEnumerator();
			while (it.MoveNext()) {
				String s = (String)it.Value;
				parameters.ReferencedAssemblies.Add(s);
			}
			// IIS
			parameters.ReferencedAssemblies.AddRange(GetGlobalReferences());

			it = usingStatements.GetEnumerator();
			while (it.MoveNext()) {
				String s = (String)it.Value;
				code.AppendLine("using " + s + ";");
			}

			code.AppendLine("using NanoScript;");
			code.AppendLine("namespace ScriptingContext");
			code.AppendLine("{");
			code.AppendLine("public class DynamicClass {");
			code.AppendLine("public bool IsDynamicScript{ get {return true;}} ");

			// external functions
			it = external_functions.GetEnumerator();
			while (it.MoveNext()) {
				String fname = (String)it.Key;
				ScriptEngine.MethodDelegate del = (ScriptEngine.MethodDelegate)it.Value;
				code.AppendLine("public ScriptEngine.MethodDelegate " + fname + "=null;");
			}

			//  scripted functions
			it = code_table.GetEnumerator();
			while (it.MoveNext()) {
				String func = (String)it.Value;
				code.AppendLine(func);
			}

			it = vars.GetEnumerator();
			while (it.MoveNext()) {
				String varname = (string)it.Key;
				Type t = (Type)it.Value;

				code.AppendLine("" + t + " " + varname + ";");
				code.AppendLine("public " + t.ToString() + "  PropertyFunc" + varname);
				code.AppendLine("{");
				code.AppendLine("    set { " + varname + "=value; }");
				code.AppendLine("    get { return " + varname + "; }");
				code.AppendLine("}");

			}

			code.AppendLine("}");
			code.AppendLine("}");

			Dictionary<string, string> providerOptions = new Dictionary<string, string>();

			providerOptions.Add("CompilerVersion", "v2.0");
			CSharpCodeProvider provider = new CSharpCodeProvider(providerOptions);
			String full_code = code.ToString();
			CompilerResults results = provider.CompileAssemblyFromSource(parameters, new string[] { full_code });

			if (results.Errors.Count > 0) {
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Compiling errors");
				String[] Lines = full_code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
				List<ScriptingContextException.CompileError> errors = new List<ScriptingContextException.CompileError>();
				foreach (CompilerError err in results.Errors) {
					ScriptingContextException.CompileError error = new ScriptingContextException.CompileError();
					error.line = err.Line.ToString();
					error.column = err.Column.ToString();
					error.text = err.ErrorText;
					error.errNumber = err.ErrorNumber;

					try {
						error.codeSnippet = Lines[err.Line - 1].TrimStart().TrimEnd();
					} catch { }

					sb.AppendFormat("at {0}:{1} - {2}", err.Line, err.Column, err.ErrorText);
					errors.Add(error);
				}

				throw new ScriptingContextException(sb.ToString(), errors);
			}

			compiledAssembly = results.CompiledAssembly;

			foreach (Type t in compiledAssembly.GetTypes()) {
				if (t.Name.Contains("DynamicClass")) {
					hinstance = Activator.CreateInstance(t);

					// we have found our classo
					foreach (MethodInfo method in t.GetMethods()) {
						ParameterInfo[] iParams = method.GetParameters();
						methods[method] = iParams;
					}

					foreach (PropertyInfo property in t.GetProperties()) {
						MethodInfo get = property.GetGetMethod();
						MethodInfo set = property.GetSetMethod();

						if (get != null) {
							get_properties[get] = null;
						}
						if (set != null) {
							set_properties[set] = null;
						}
					}

					foreach (FieldInfo field in t.GetFields()) {
						fields[field.Name] = field;
					}

					break;
				}
			}

			it = vars_defaults.GetEnumerator();
			while (it.MoveNext()) {
				String func = (String)it.Key;
				object o = (object)it.Value;

				GlobalSet(func, o);
			}

			it = external_functions.GetEnumerator();
			while (it.MoveNext()) {
				String fname = (String)it.Key;
				ScriptEngine.MethodDelegate del = (ScriptEngine.MethodDelegate)it.Value;

				if (fields.ContainsKey(fname)) {
					FieldInfo field = (FieldInfo)fields[fname];
					field.SetValue(hinstance, del);
				}
			}

			isCompiled = true;

		}

		public static string[] GetGlobalReferences() {
			string[] _globalReferences = new string[] {
				"System.dll", "mscorlib.dll", "System.Data.dll",
				"+log4net.dll", "+Nii.JSON.dll", 
				"+Solari.Core.dll"
			};

			Assembly a = Assembly.GetCallingAssembly();
			string s1 = a.CodeBase, basePath = "";
			if (s1 != null)
				basePath = Path.GetDirectoryName(new Uri(s1).LocalPath);
			string[] ret = new string[_globalReferences.Length];
			for (int i = 0; i < ret.Length; i++)
				if (_globalReferences[i].StartsWith("+"))
					ret[i] = Path.Combine(basePath, _globalReferences[i].Substring(1));
				else
					ret[i] = _globalReferences[i];
			return ret;
		}

	}
}

