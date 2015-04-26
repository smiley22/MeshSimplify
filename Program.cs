using MeshSimplify.Algorithms;
using Plossum.CommandLine;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MeshSimplify {
	/// <summary>
	/// Implementiert ein Programm zur Vereinfachung von Polygonnetzen.
	/// </summary>
	class Program {
		/// <summary>
		/// Einstiegspunkt des Programms.
		/// </summary>
		static void Main() {
			// Kommandozeilenparameter auslesen und verarbeiten.
			var args = ProcessArguments();
			if(args == null)
				return;
			try {
				var mesh = ObjIO.Load(args.InputFile);
				// Das Erzeugen von Progressive Meshes aus bestehenden PMs wird nicht
				// unterstützt.
				if (args.ProgressiveMesh && mesh.Splits.Count > 0)
					Error("progressive-mesh may only be created from plain mesh.");
				using (var algo = InstantiateAlgorithm(args.Algorithm, args)) {
					Stopwatch sw = Stopwatch.StartNew();
					var alteredMesh = args.Restore ? mesh.Expand(args.TargetFaceCount, args.Verbose) :
						algo.Simplify(mesh, args.TargetFaceCount, args.ProgressiveMesh, args.Verbose);
					sw.Stop();
					Console.WriteLine("{0} took {1} ms", args.Restore ? "Expansion" : "Simplification",
						sw.Elapsed.TotalMilliseconds);
					var output = args.OutputFile ??
						Path.GetFileNameWithoutExtension(args.InputFile) + "_out" +
						Path.GetExtension(args.InputFile);
					Console.WriteLine("Writing {0} mesh to {1}...", args.Restore ? "expanded" : "simplified",
						output);
					ObjIO.Save(alteredMesh, output);
					Console.WriteLine("Done");
				}
			} catch (FileNotFoundException) {
				Console.WriteLine("Couldn't find file `{0}'.",  args.InputFile);
			}
		}

		/// <summary>
		/// Verarbeitet die Kommandozeilenparameter.
		/// </summary>
		/// <returns>
		/// Eine Instanz der Options-Klasse, die die an das Programm übergebenen Parameter
		/// enthält, oder null, wenn das Programm beendet werden soll.
		/// </returns>
		static Options ProcessArguments() {
			int width = 78;
			var opts = new Options();
			using (var parser = new CommandLineParser(opts)) {
				parser.Parse();
				if (opts.Help) {
					Console.WriteLine(parser.UsageInfo.ToString(width, false));
				} else if(opts.Version) {
					var a = FileVersionInfo.GetVersionInfo(
						Assembly.GetExecutingAssembly().Location);
					Console.WriteLine("{0} ({1})\n- {2} -\n{3}\n{4}", a.ProductName, a.ProductVersion,
						a.Comments, a.CompanyName, a.LegalCopyright);
				} else if (parser.HasErrors) {
					Console.WriteLine(parser.UsageInfo.ToString(width, true));
				} else if (parser.RemainingArguments.Count == 0) {
					Console.WriteLine("error: no input file");
					Console.WriteLine("usage: MeshSimplify [options] file");
					Console.WriteLine("Try `MeshSimplify --help' for more information.");
				} else {
					opts.InputFile = parser.RemainingArguments[0];
					return opts;
				}
				return null;
			}
		}

		/// <summary>
		/// Erstellt eine Instanz des Algorithmus mit dem angegebenen Namen.
		/// </summary>
		/// <param name="name">
		/// Der Name des Algorithmus.
		/// </param>
		/// <param name="opts">
		/// Die Argumente, die der Instanz des Algorithmus zur Verfügung gestellt werden sollen.
		/// </param>
		/// <returns>
		/// Eine Instanz des Algorithmus mit dem angegebenen Namen.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Es konnte keine Klasse mit dem Namen des angegebenen Algorithmus gefunden
		/// werden.
		/// </exception>
		static Algorithm InstantiateAlgorithm(string name, Options opts) {
			foreach (var t in Assembly.GetCallingAssembly().GetTypes()) {
				if(!t.IsSubclassOf(typeof(Algorithm)))
					continue;
				var a = t.GetCustomAttribute<DisplayNameAttribute>();
				if ((a != null && a.DisplayName == name) || (t.Name == name)) {
					return (Algorithm) Activator.CreateInstance(t, new object[] { opts });
				}
			}
			throw new InvalidOperationException(string.Format(
				"Could find algorithm '{0}'.", name));
		}

		/// <summary>
		/// Gibt die angegebene Nachricht auf der Standardausgabe aus und beendet anschließend
		/// das Programm.
		/// </summary>
		/// <param name="format">
		/// Ein formatierter String.
		/// </param>
		/// <param name="arg">
		/// Etwaige Parameter des formatierten Strings.
		/// </param>
		static void Error(string format, params object[] arg) {
			Console.WriteLine("error: " + format, arg);
			Environment.Exit(1);
		}
	}
}
