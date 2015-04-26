using OpenTK;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MeshSimplify {
	/// <summary>
	/// Eine Klasse zum Laden und Speichern von Wavefront .obj Dateien.
	/// </summary>
	/// <remarks>
	/// Es werden nur Dreiecksnetze unterstützt.
	/// </remarks>
	public static class ObjIO {
		/// <summary>
		/// Parsed die angegebene .obj Datei.
		/// </summary>
		/// <param name="path">
		/// Der Name der Datei, welche geparsed werden soll.
		/// </param>
		/// <returns>
		/// Eine Mesh Instanz, die aus den Daten der .obj Datei erzeugt wurde.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Die angegebene .obj Datei ist ungültig oder enthält ein nicht-unterstütztes
		/// Meshformat.
		/// </exception>
		/// <exception cref="IOException">
		/// Die angegebene Datei konnte nicht gelesen werden.
		/// </exception>
		public static Mesh Load(string path) {
			var mesh = new Mesh();
			using (var sr = File.OpenText(path)) {
				string l = string.Empty;
				while ((l = sr.ReadLine()) != null) {
					if (l.StartsWith("v "))
						mesh.Vertices.Add(ParseVertex(l));
					else if (l.StartsWith("f "))
						mesh.Faces.Add(ParseFace(l));
					else if (l.StartsWith("#vsplit "))
						mesh.Splits.Enqueue(ParseVertexSplit(l));
				}
			}
			return mesh;
		}

		/// <summary>
		/// Schreibt die angegebene Mesh in die angegebene Datei.
		/// </summary>
		/// <param name="mesh">
		/// Die zu schreibende Mesh.
		/// </param>
		/// <param name="path">
		/// Der Name der Datei, in die die Mesh geschrieben werden soll.
		/// </param>
		public static void Save(Mesh mesh, string path) {
			using (var fs = File.Open(path, FileMode.Create)) {
				using (var sw = new StreamWriter(fs)) {
					sw.WriteLine("# {0}", DateTime.Now);
					sw.WriteLine("# {0} Vertices", mesh.Vertices.Count);
					foreach (var v in mesh.Vertices) {
						sw.WriteLine("v {0} {1} {2}",
							v.Position.X.ToString(CultureInfo.InvariantCulture),
							v.Position.Y.ToString(CultureInfo.InvariantCulture),
							v.Position.Z.ToString(CultureInfo.InvariantCulture));
					}
					sw.WriteLine();
					sw.WriteLine("# {0} Faces", mesh.Faces.Count);
					foreach (var f in mesh.Faces) {
						sw.WriteLine("f {0} {1} {2}", f.Indices[0] + 1, f.Indices[1] + 1,
							f.Indices[2] + 1);
					}
					if (mesh.Splits.Count > 0) {
						sw.WriteLine();
						sw.WriteLine("# {0} Split Records", mesh.Splits.Count);
						foreach (var s in mesh.Splits) {
							// vsplit als Kommentar schreiben, so daß die Datei auch weiterhin
							// eine gültige .obj Datei bleibt.
							sw.Write("#vsplit {0} {{{1} {2} {3}}} {{{4} {5} {6}}} {{ ", s.S + 1,
								s.SPosition.X.ToString(CultureInfo.InvariantCulture),
								s.SPosition.Y.ToString(CultureInfo.InvariantCulture),
								s.SPosition.Z.ToString(CultureInfo.InvariantCulture),
								s.TPosition.X.ToString(CultureInfo.InvariantCulture),
								s.TPosition.Y.ToString(CultureInfo.InvariantCulture),
								s.TPosition.Z.ToString(CultureInfo.InvariantCulture));
							foreach (var f in s.Faces) {
								sw.Write("({0} {1} {2}) ", f.Indices[0] + 1, f.Indices[1] + 1,
									f.Indices[2] + 1);
							}
							sw.Write("}");
							sw.WriteLine();
						}
					}
				}
			}
		}

		/// <summary>
		/// Parsed eine Vertex Deklaration.
		/// </summary>
		/// <param name="l">
		/// Die Zeile, die die Vertex Deklaration enthält.
		/// </param>
		/// <returns>
		/// Der Vertex.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Die Vertexdelaration ist ungültig.
		/// </exception>
		static Vertex ParseVertex(string l) {
			var p = l.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (p.Length != 4)
				throw new InvalidOperationException("Invalid vertex format: " + l);
			return new Vertex() {
				Position = new Vector3d(
					double.Parse(p[1], CultureInfo.InvariantCulture),
					double.Parse(p[2], CultureInfo.InvariantCulture),
					double.Parse(p[3], CultureInfo.InvariantCulture))
			};
		}

		/// <summary>
		/// Parsed eine Facetten Deklaration.
		/// </summary>
		/// <param name="l">
		/// Die Zeile, die die Facetten Deklaration enthält.
		/// </param>
		/// <returns>
		/// Die Facette.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Die Facettendeklaration ist ungültig.
		/// </exception>
		static Triangle ParseFace(string l) {
			var p = l.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (p.Length != 4)
				throw new InvalidOperationException("Invalid face: " + l);
			var indices = new[] {
				int.Parse(p[1]) - 1,
				int.Parse(p[2]) - 1,
				int.Parse(p[3]) - 1
			};
			return new Triangle(indices);
		}

		/// <summary>
		/// Parsed einen dreidimensionalen Vektor.
		/// </summary>
		/// <param name="l">
		/// Die Zeile, die den Vektor enthält.
		/// </param>
		/// <returns>
		/// Der Vektor.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Die Vektordeklaration ist ungültig.
		/// </exception>
		static Vector3d ParseVector(string l) {
			var p = l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (p.Length != 3)
				throw new InvalidOperationException("Invalid vector: " + l);
			return new Vector3d(
				double.Parse(p[0], CultureInfo.InvariantCulture),
				double.Parse(p[1], CultureInfo.InvariantCulture),
				double.Parse(p[2], CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Parsed eine VertexSplit Deklaration.
		/// </summary>
		/// <param name="l">
		/// Die Zeile, die die VertexSplit Deklaration enthält.
		/// </param>
		/// <returns>
		/// Der Vertex-Split Eintrag.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Die VertexSplit Deklaration ist ungültig.
		/// </exception>
		static VertexSplit ParseVertexSplit(string l) {
			var m = Regex.Match(l, @"^#vsplit\s+(\d+)\s+{(.*)}\s+{(.*)}\s+{(.*)}$");
			if (!m.Success)
				throw new InvalidOperationException("Invalid vsplit: " + l);
			var s = new VertexSplit() {
				S = int.Parse(m.Groups[1].Value) - 1,
				SPosition = ParseVector(m.Groups[2].Value),
				TPosition = ParseVector(m.Groups[3].Value)
			};
			var faceIndices = m.Groups[4].Value;
			var matches = Regex.Matches(faceIndices, @"\((-?\d+)\s+(-?\d+)\s+(-?\d+)\)");
			for (int i = 0; i < matches.Count; i++) {
				var _m = matches[i];
				if (!_m.Success)
					throw new InvalidOperationException("Invalid face index entry in vsplit: " + l);
				var indices = new[] {
					int.Parse(_m.Groups[1].Value) - 1,
					int.Parse(_m.Groups[2].Value) - 1,
					int.Parse(_m.Groups[3].Value) - 1
				};
				s.Faces.Add(new Triangle(indices));
			}
			return s;
		}
	}
}
