
namespace MeshSimplify {
	/// <summary>
	/// Repräsentiert eine Facette.
	/// </summary>
	public class Triangle {
		/// <summary>
		/// Die Indices der Vertices des Triangles.
		/// </summary>
		public readonly int[] Indices = new int[3];

		/// <summary>
		/// Initialisiert eine neue Instanz der Triangle Klasse.
		/// </summary>
		/// <param name="index1">
		/// Der erste Index.
		/// </param>
		/// <param name="index2">
		/// Der zweite Index.
		/// </param>
		/// <param name="index3">
		/// Der dritte Index.
		/// </param>
		public Triangle(int index1, int index2, int index3) {
			Indices[0] = index1;
			Indices[1] = index2;
			Indices[2] = index3;
		}

		/// <summary>
		/// Initialisiert eine neue Instanz der Triangle Klasse.
		/// </summary>
		/// <param name="indices">
		/// Die Indices der Vertices, die das Triangle ausmachen.
		/// </param>
		public Triangle(int[] indices)
			: this(indices[0], indices[1], indices[2]) {
		}
	}
}
