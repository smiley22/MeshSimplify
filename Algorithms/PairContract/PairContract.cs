using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MeshSimplify.Algorithms {
	/// <summary>
	/// Implementiert den 'Pair-Contract' Algorithmus nach Garland's
	///		"Surface Simplification Using Quadric Error Metrics"
	/// Methode.
	/// </summary>
	[DisplayName("[Gar97]")]
	public class PairContract : Algorithm {
		/// <summary>
		/// Die Q-Matrizen der Vertices.
		/// </summary>
		IDictionary<int, Matrix4d> Q;

		/// <summary>
		/// Die Vertices der Mesh. 
		/// </summary>
		IDictionary<int, Vector3d> vertices = new SortedDictionary<int, Vector3d>();

		/// <summary>
		/// Die Facetten der Mesh.
		/// </summary>
		IList<Triangle> faces;

		/// <summary>
		/// Die Vertexpaare welche nach und nach kontraktiert werden.
		/// </summary>
		ISet<Pair> pairs = new SortedSet<Pair>();

		/// <summary>
		/// Der distanzbasierte Schwellwert zur Bestimmung von gültigen Vertexpaaren.
		/// </summary>
		readonly double distanceThreshold;

		/// <summary>
		/// true für ausführliche Ausgaben auf der Standardausgabe.
		/// </summary>
		bool verbose;

		/// <summary>
		/// Speichert für jeden Vertex Referenzen auf seine inzidenten Facetten.
		/// </summary>
		IDictionary<int, ISet<Triangle>> incidentFaces = new Dictionary<int, ISet<Triangle>>();

		/// <summary>
		/// Speichert für jeden Vertex Referenzen auf die Vertexpaare zu denen er gehört.
		/// </summary>
		IDictionary<int, ISet<Pair>> pairsPerVertex = new Dictionary<int, ISet<Pair>>();

		/// <summary>
		/// Stack der ursprünglichen Indices der Vertices, die kontraktiert wurden.
		/// </summary>
		Stack<int> contractionIndices = new Stack<int>();

		/// <summary>
		/// Speichert VertexSplit Einträge zur Erstellung einer Progressive Mesh.
		/// </summary>
		Stack<VertexSplit> splits = new Stack<VertexSplit>();

		/// <summary>
		/// Gibt an, ob <c>VertexSplit</c> Einträge erzeugt werden sollen.
		/// </summary>
		bool createSplitRecords;

		/// <summary>
		/// Initialisiert eine neue Instanz der PairContract-Klasse.
		/// </summary>
		/// <param name="opts">
		/// Die Argumente, die dem Algorithmus zur Verfügung gestellt werden.
		/// </param>
		public PairContract(Options opts)
			: base(opts) {
				distanceThreshold = opts.DistanceThreshold;
			Print("PairContract initialized with distanceThreshold = {0}",
				distanceThreshold);
		}

		/// <summary>
		/// Vereinfacht die angegebene Eingabemesh.
		/// </summary>
		/// <param name="input">
		/// Die zu vereinfachende Mesh.
		/// </param>
		/// <param name="targetFaceCount">
		/// Die Anzahl der Facetten, die die erzeugte Vereinfachung anstreben soll.
		/// </param>
		/// <param name="createSplitRecords">
		/// true, um <c>VertexSplit</c> Einträge für die Mesh zu erzeugen; andernfalls false.
		/// </param>
		/// <param name="verbose">
		/// true, um diagnostische Ausgaben während der Vereinfachung zu erzeugen.
		/// </param>
		/// <returns>
		/// Die erzeugte vereinfachte Mesh.
		/// </returns>
		public override Mesh Simplify(Mesh input, int targetFaceCount, bool createSplitRecords,
			bool verbose) {
			this.verbose = verbose;
			this.createSplitRecords = createSplitRecords;
			// Wir starten mit den Vertices und Faces der Ausgangsmesh.
			faces = new List<Triangle>(input.Faces);
			for (int i = 0; i < input.Vertices.Count; i++)
				vertices[i] = input.Vertices[i].Position;
			// 1. Die initialen Q Matrizen für jeden Vertex v berechnen.
			Q = ComputeInitialQForEachVertex(options.Strict);
			// 2. All gültigen Vertexpaare bestimmen.
			pairs = ComputeValidPairs();
			// 3. Iterativ das Paar mit den geringsten Kosten kontraktieren und entsprechend
			//    die Kosten aller davon betroffenen Paare aktualisieren.
			while (faces.Count > targetFaceCount) {
		//		var pair = FindLeastCostPair();
				var pair = pairs.First();
				Print("Contracting pair ({0}, {1}) with contraction cost = {2}",
					pair.Vertex1, pair.Vertex2, pair.Cost.ToString("e"));
				ContractPair(pair);
				Print("New face count: {0}", faces.Count);
			}
			// 4. Neue Mesh Instanz erzeugen und zurückliefern.
			return BuildMesh();
		}

		/// <summary>
		/// Berechnet die Kp-Matrix für die Ebenen aller Facetten.
		/// </summary>
		/// <param name="strict">
		/// true, um eine <c>InvalidOperationException</c> zu werfen, falls ein
		/// degeneriertes Face entdeckt wird.
		/// </param>
		/// <returns>
		/// Eine Liste von Kp-Matrizen.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Es wurde ein degeneriertes Face gefunden, dessen Vertices kollinear sind.
		/// </exception>
		IList<Matrix4d> ComputeKpForEachPlane(bool strict) {
			var kp = new List<Matrix4d>();
			var degenerate = new List<Triangle>();
			foreach (var f in faces) {
				var points = new[] {
					vertices[f.Indices[0]],
					vertices[f.Indices[1]],
					vertices[f.Indices[2]]
				};
				// Ebene aus den 3 Ortsvektoren konstruieren.
				var dir1 = points[1] - points[0];
				var dir2 = points[2] - points[0];
				var n = Vector3d.Cross(dir1, dir2);
				// Wenn das Kreuzprodukt der Nullvektor ist, sind die Richtungsvektoren
				// kollinear, d.h. die Vertices liegen auf einer Geraden und die Facette
				// ist degeneriert.
				if (n == Vector3d.Zero) {
					degenerate.Add(f);
					if (strict) {
						var msg = new StringBuilder()
							.AppendFormat("Encountered degenerate face ({0} {1} {2})",
								f.Indices[0], f.Indices[1], f.Indices[2])
							.AppendLine()
							.AppendFormat("Vertex 1: {0}\n", points[0])
							.AppendFormat("Vertex 2: {0}\n", points[1])
							.AppendFormat("Vertex 3: {0}\n", points[2])
							.ToString();
						throw new InvalidOperationException(msg);
					}
				} else {
					n.Normalize();
					var a = n.X;
					var	b = n.Y;
					var	c = n.Z;
					var	d = -Vector3d.Dot(n, points[0]);
					// Siehe [Gar97], Abschnitt 5 ("Deriving Error Quadrics").
					var m = new Matrix4d() {
						M11 = a * a, M12 = a * b, M13 = a * c, M14 = a * d,
						M21 = a * b, M22 = b * b, M23 = b * c, M24 = b * d,
						M31 = a * c, M32 = b * c, M33 = c * c, M34 = c * d,
						M41 = a * d, M42 = b * d, M43 = c * d, M44 = d * d
					};
					kp.Add(m);
				}
			}
			if (degenerate.Count > 0)
				Print("Warning: {0} degenerate faces found.", degenerate.Count);
			foreach (var d in degenerate)
				faces.Remove(d);
			return kp;
		}

		/// <summary>
		/// Berechnet die initialen Q-Matrizen für alle Vertices.
		/// </summary>
		/// <param name="strict">
		/// true, um eine <c>InvalidOperationException</c> zu werfen, falls ein
		/// degeneriertes Face entdeckt wird.
		/// </param>
		/// <returns>
		/// Eine Map der initialen Q-Matrizen.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Es wurde ein degeneriertes Face gefunden, dessen Vertices kollinear sind.
		/// </exception>
		IDictionary<int, Matrix4d> ComputeInitialQForEachVertex(bool strict) {
			var q = new Dictionary<int, Matrix4d>();
			// Kp Matrix für jede Ebene, d.h. jede Facette bestimmen.
			var kp = ComputeKpForEachPlane(strict);
			// Q Matrix für jeden Vertex mit 0 initialisieren.
			for (int i = 0; i < vertices.Count; i++)
				q[i] = new Matrix4d();
			// Q ist die Summe aller Kp Matrizen der inzidenten Facetten jedes Vertex.
			for (int c = 0; c < faces.Count; c++) {
				var f = faces[c];
				for (int i = 0; i < f.Indices.Length; i++) {
					q[f.Indices[i]] = q[f.Indices[i]] + kp[c];
				}
			}
			return q;
		}

		/// <summary>
		/// Berechnet alle gültigen Vertexpaare.
		/// </summary>
		/// <returns>
		/// Die Menge aller gültigen Vertexpaare.
		/// </returns>
		ISet<Pair> ComputeValidPairs() {
			// 1. Kriterium: 2 Vertices sind ein Kontraktionspaar, wenn sie durch eine Kante
			//               miteinander verbunden sind.
			for (int i = 0; i < faces.Count; i++) {
				var f = faces[i];
				// Vertices eines Dreiecks sind jeweils durch Kanten miteinander verbunden
				// und daher gültige Paare.
				var s = f.Indices.OrderBy(val => val).ToArray();
				for (int c = 0; c < s.Length; c++) {
					if (!pairsPerVertex.ContainsKey(s[c]))
						pairsPerVertex[s[c]] = new HashSet<Pair>();
					if (!incidentFaces.ContainsKey(s[c]))
						incidentFaces[s[c]] = new HashSet<Triangle>();
					incidentFaces[s[c]].Add(f);
				}

				var p = ComputeMinimumCostPair(s[0], s[1]);
				if (pairs.Add(p))
					Print("Added Vertexpair ({0}, {1})", s[0], s[1]);
				pairsPerVertex[s[0]].Add(p);
				pairsPerVertex[s[1]].Add(p);

				p = ComputeMinimumCostPair(s[0], s[2]);
				if (pairs.Add(p))
					Print("Added Vertexpair ({0}, {1})", s[0], s[2]);
				pairsPerVertex[s[0]].Add(p);
				pairsPerVertex[s[2]].Add(p);

				p = ComputeMinimumCostPair(s[1], s[2]);
				if (pairs.Add(p))
					Print("Added Vertexpair ({0}, {1})", s[1], s[2]);
				pairsPerVertex[s[1]].Add(p);
				pairsPerVertex[s[2]].Add(p);

			}
			// 2. Kriterium: 2 Vertices sind ein Kontraktionspaar, wenn die euklidische
			//               Distanz < Threshold-Parameter t.
			if (distanceThreshold > 0) {
				// Nur prüfen, wenn überhaupt ein Threshold angegeben wurde.
				for (int i = 0; i < vertices.Count; i++) {
					for (int c = i + 1; c < vertices.Count; c++) {
						if ((vertices[i] - vertices[c]).Length < distanceThreshold) {
							if (pairs.Add(ComputeMinimumCostPair(i, c)))
								Print("Added Vertexpair ({0}, {1})", i, c);
						}
					}
				}
			}
			return pairs;
		}

		/// <summary>
		/// Bestimmt die Kosten für die Kontraktion der angegebenen Vertices.
		/// </summary>
		/// <param name="s">
		/// Der erste Vertex des Paares.
		/// </param>
		/// <param name="t">
		/// Der zweite Vertex des Paares.
		/// </param>
		/// <returns>
		/// Eine Instanz der Pair-Klasse.
		/// </returns>
		Pair ComputeMinimumCostPair(int s, int t) {
			Vector3d target;
			double cost;
			var q = Q[s] + Q[t];
			// Siehe [Gar97], Abschnitt 4 ("Approximating Error With Quadrics").
			var m = new Matrix4d() {
				M11 = q.M11, M12 = q.M12, M13 = q.M13, M14 = q.M14,
				M21 = q.M12, M22 = q.M22, M23 = q.M23, M24 = q.M24,
				M31 = q.M13, M32 = q.M23, M33 = q.M33, M34 = q.M34,
				M41 = 0, M42 = 0, M43 = 0, M44 = 1
			};
			// Wenn m invertierbar ist, lässt sich die optimale Position bestimmen.
//			if (m.Determinant != 0) {
			try {
				// Determinante ist ungleich 0 für invertierbare Matrizen.
				var inv = Matrix4d.Invert(m);
				target = new Vector3d(inv.M14, inv.M24, inv.M34);
				cost = ComputeVertexError(target, q);
			} catch(InvalidOperationException) {
//			} else {
				// Ansonsten den besten Wert aus Position von Vertex 1, Vertex 2 und
				// Mittelpunkt wählen.
				var v1 = vertices[s];
				var v2 = vertices[t];
				var mp = new Vector3d() {
					X = (v1.X + v2.X) / 2,
					Y = (v1.Y + v2.Y) / 2,
					Z = (v1.Z + v2.Z) / 2
				};
				var candidates = new[] {
					new { cost = ComputeVertexError(v1, q), target = v1 },
					new { cost = ComputeVertexError(v2, q), target = v2 },
					new { cost = ComputeVertexError(mp, q), target = mp }
				};
				var best = (from p in candidates
							orderby p.cost
							select p).First();
				target = best.target;
				cost = best.cost;
			}
			return new Pair(s, t, target, cost);
		}

		/// <summary>
		/// Bestimmt den geometrischen Fehler des angegebenen Vertex in Bezug auf die
		/// Fehlerquadrik Q.
		/// </summary>
		/// <param name="v">
		/// Der Vertex, dessen geometrischer Fehler bestimmt werden soll.
		/// </param>
		/// <param name="q">
		/// Die zugrundeliegende Fehlerquadrik.
		/// </param>
		/// <returns>
		/// Der geometrische Fehler an der Stelle des angegebenen Vertex.
		/// </returns>
		double ComputeVertexError(Vector3d v, Matrix4d q) {
			var h = new Vector4d(v, 1);
			// Geometrischer Fehler Δ(v) = vᵀQv.
			return Vector4d.Dot(Vector4d.Transform(h, q), h);
		}

		/// <summary>
		/// Findet aus der angegebenen Menge das Paar mit den kleinsten Kosten.
		/// </summary>
		/// <param name="pairs">
		/// Die Menge der Vertexpaare, aus der das Paar mit den kleinsten Kosten
		/// gefunden werden soll.
		/// </param>
		/// <returns>
		/// Das Paar mit den kleinsten Kosten.
		/// </returns>
		Pair FindLeastCostPair(ISet<Pair> pairs) {
			double cost = double.MaxValue;
			Pair best = null;
			foreach (var p in pairs) {
				if (p.Cost < cost) {
					cost = p.Cost;
					best = p;
				}
			}
			return best;
		}

		/// <summary>
		/// Kontraktiert das angegebene Vertexpaar.
		/// </summary>
		/// <param name="p">
		/// Das Vertexpaar, das kontraktiert werden soll.
		/// </param>
		void ContractPair(Pair p) {
			if (createSplitRecords)
				AddSplitRecord(p);
			// 1. Koordinaten von Vertex 1 werden auf neue Koordinaten abgeändert.
			vertices[p.Vertex1] = p.Target;
			// 2. Matrix Q von Vertex 1 anpassen.
			Q[p.Vertex1] = Q[p.Vertex1] + Q[p.Vertex2];
			// 3. Alle Referenzen von Facetten auf Vertex 2 auf Vertex 1 umbiegen.
			var facesOfVertex2	 = incidentFaces[p.Vertex2];
			var facesOfVertex1	 = incidentFaces[p.Vertex1];
			var degeneratedFaces = new HashSet<Triangle>();

			// Jede Facette von Vertex 2 wird entweder Vertex 1 hinzugefügt, oder
			// degeneriert.
			foreach (var f in facesOfVertex2) {
				if (facesOfVertex1.Contains(f)) {
					degeneratedFaces.Add(f);
				} else {
					// Indices umbiegen und zu faces von Vertex 1 hinzufügen.
					for (int i = 0; i < f.Indices.Length; i++) {
						if (f.Indices[i] == p.Vertex2)
							f.Indices[i] = p.Vertex1;
					}
					facesOfVertex1.Add(f);
				}
			}
			// Nun degenerierte Facetten entfernen.
			foreach (var f in degeneratedFaces) {
				for (int i = 0; i < f.Indices.Length; i++)
					incidentFaces[f.Indices[i]].Remove(f);
				faces.Remove(f);
			}

			// Vertex 2 aus Map löschen.
			vertices.Remove(p.Vertex2);

			// Alle Vertexpaare zu denen Vertex 2 gehört dem Set von Vertex 1 hinzufügen.
			pairsPerVertex[p.Vertex1].UnionWith(pairsPerVertex[p.Vertex2]);
			// Anschließend alle Paare von Vertex 1 ggf. umbiegen und aktualisieren.
			var remove = new List<Pair>();
			foreach (var pair in pairsPerVertex[p.Vertex1]) {
				// Aus Collection temporär entfernen, da nach Neuberechnung der Kosten
				// neu einsortiert werden muss.
				pairs.Remove(pair);
				int s = pair.Vertex1, t = pair.Vertex2;
				if (s == p.Vertex2)
					s = p.Vertex1;
				if (t == p.Vertex2)
					t = p.Vertex1;
				if (s == t) {
					remove.Add(pair);
				} else {
					var np = ComputeMinimumCostPair(Math.Min(s, t), Math.Max(s, t));
					pair.Vertex1 = np.Vertex1;
					pair.Vertex2 = np.Vertex2;
					pair.Target = np.Target;
					pair.Cost = np.Cost;

					pairs.Add(pair);
				}
			}
			// "Degenerierte" Paare entfernen.
			foreach (var r in remove) {
				pairsPerVertex[p.Vertex1].Remove(r);
			}
		}

		/// <summary>
		/// Erzeugt und speichert einen VertexSplit Eintrag für das angegebene Paar.
		/// </summary>
		/// <param name="p">
		/// Das Paar für welches ein VertexSplit Eintrag erstellt werden soll.
		/// </param>
		void AddSplitRecord(Pair p) {
			contractionIndices.Push(p.Vertex2);
			var split = new VertexSplit() {
				S = p.Vertex1, SPosition = vertices[p.Vertex1],
				TPosition = vertices[p.Vertex2]
			};
			foreach (var f in incidentFaces[p.Vertex2]) {
				// -1 wird später durch eigentlichen Index ersetzt, wenn dieser bekannt
				// ist.
				split.Faces.Add(new Triangle(
					f.Indices[0] == p.Vertex2 ? -1 : f.Indices[0],
					f.Indices[1] == p.Vertex2 ? -1 : f.Indices[1],
					f.Indices[2] == p.Vertex2 ? -1 : f.Indices[2]));
			}
			splits.Push(split);
		}

		/// <summary>
		/// Erzeugt eine neue Mesh Instanz den aktuellen Vertex- und Facettendaten.
		/// </summary>
		/// <returns>
		/// Die erzeugte Mesh Instanz.
		/// </returns>
		Mesh BuildMesh() {
			// Mapping von alten auf neue Vertexindices für Facetten erstellen.
			var mapping = new Dictionary<int, int>();
			int index = 0;
			var verts = new List<Vertex>();
			var _faces = new List<Triangle>();
			foreach (var p in vertices) {
				mapping.Add(p.Key, index++);
				verts.Add(new Vertex() { Position = p.Value });
			}
			foreach (var f in faces) {
				_faces.Add(new Triangle(
					mapping[f.Indices[0]],
					mapping[f.Indices[1]],
					mapping[f.Indices[2]]
				));
			}
			// Progressive Mesh: Indices im Mapping für "zukünftige" Vertices anlegen.
			foreach (var c in contractionIndices)
				mapping.Add(c, index++);
			int n = verts.Count;
			foreach (var s in splits) {
				var t = n++;
				s.S = mapping[s.S];
				foreach (var f in s.Faces) {
					for (int i = 0; i < f.Indices.Length; i++) {
						f.Indices[i] = f.Indices[i] < 0 ? t : mapping[f.Indices[i]];
					}
				}
			}
			return new Mesh(verts, _faces, splits);
		}

		/// <summary>
		/// Gibt alle Ressourcen der Instanz frei.
		/// </summary>
		public override void Dispose() {
			// Nichts zu tun hier.
		}

		/// <summary>
		/// Schreibt den angegebenen String in die Standardausgabe, wenn ausführliche
		/// Ausgaben aktiviert sind.
		/// </summary>
		/// <param name="format">
		/// Der formatierte String.
		/// </param>
		/// <param name="arg">
		/// Die etwaigen Parameter für den formatierten String.
		/// </param>
		void Print(string format, params object[] arg) {
			if (verbose)
				Console.WriteLine(format, arg);
		}
	}
}
