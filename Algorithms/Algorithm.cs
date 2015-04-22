using System;

namespace MeshSimplify.Algorithms {
	/// <summary>
	/// Die abstrakte Basisklasse von der jede konkrete Implementierung abgeleitet werden muss.
	/// </summary>
	public abstract class Algorithm : IDisposable {
		/// <summary>
		/// Gewährt der Instanz Zugriff auf etwaige Kommandozeileneinstellungen.
		/// </summary>
		protected Options options;

		/// <summary>
		/// Initialisiert eine neue Instanz der Algorithm-Klasse.
		/// </summary>
		/// <param name="opts">
		/// Die Argumente, die dem Algorithmus zur Verfügung gestellt werden.
		/// </param>
		public Algorithm(Options opts) {
			options = opts;
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
		public abstract Mesh Simplify(Mesh input, int targetFaceCount, bool createSplitRecords,
			bool verbose);

		/// <summary>
		/// Gibt alle Ressourcen der Instanz frei.
		/// </summary>
		public abstract void Dispose();
	}
}
